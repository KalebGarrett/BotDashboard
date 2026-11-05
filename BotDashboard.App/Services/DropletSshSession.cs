using BotDashboard.App.Credentials;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace BotDashboard.App.Services;

/// <summary>
/// One long-lived SSH connection to the droplet, shared by everything that talks to it.
/// </summary>
/// <remarks>
/// The droplet's UFW has <c>22/tcp LIMIT IN</c> rather than ALLOW. UFW implements LIMIT with
/// iptables' <c>recent</c> module: more than six connections from one source address in thirty
/// seconds sends that source to the <c>ufw-user-limit</c> chain, which REJECTs with
/// icmp-port-unreachable. A reject is a TCP RST, so it arrives here as "Connection refused" --
/// indistinguishable from nothing listening on the port, which is exactly how it was
/// misdiagnosed as a dead sshd.
///
/// Opening a client per command made the dashboard trip that rule against itself: the Ollama
/// panel has nine buttons, and six clicks inside half a minute locked it out for the next
/// thirty seconds while the panel reported the service state as unknown. Worse, the limit is
/// per source address, so the NAS and any workstation behind the same NAT share one budget.
///
/// Holding the session open turns a burst of clicks into one connection and many channels,
/// which is what SSH is for. It reconnects on demand, so a dropped link costs one retry rather
/// than a dead panel.
///
/// Registered as a singleton: the point is that every circuit shares the one connection.
/// </remarks>
public class DropletSshSession : IDisposable
{
    /// <summary>
    /// Pulling a model downloads gigabytes, so the default 30s command timeout is not enough.
    /// </summary>
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(10);

    private readonly DigitalOceanDropletCredentials _droplet;
    private readonly ILogger<DropletSshSession> _logger;

    /// <summary>
    /// Serialises access to the shared client.
    /// </summary>
    /// <remarks>
    /// SSH.NET can multiplex channels, but this class hands out one command at a time: several
    /// circuits can be open at once, and interleaving reconnects on a shared client is a
    /// source of races that would be hard to reproduce and harder to explain.
    /// </remarks>
    private readonly SemaphoreSlim _gate = new(1, 1);

    private SshClient _client;

    public DropletSshSession(DigitalOceanDropletCredentials droplet, ILogger<DropletSshSession> logger)
    {
        _droplet = droplet;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_droplet.Host)
        && !string.IsNullOrWhiteSpace(_droplet.Username)
        && !string.IsNullOrWhiteSpace(_droplet.Password);

    /// <summary>
    /// Runs a command on the droplet, reusing the open session where there is one.
    /// </summary>
    /// <returns>stdout, with stderr appended when the command wrote any.</returns>
    public string Run(string command)
    {
        _gate.Wait();

        try
        {
            try
            {
                return Execute(command);
            }
            catch (Exception exception) when (exception is SshException or ObjectDisposedException
                                                  or System.Net.Sockets.SocketException)
            {
                // A session can die quietly between uses -- the droplet reboots, sshd restarts,
                // a NAT mapping expires. One silent reconnect is the whole benefit of keeping
                // it open; a second failure is a real fault and belongs to the caller.
                _logger.LogInformation(exception,
                    "Droplet SSH session failed; reconnecting and retrying once.");

                Drop();
                return Execute(command);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private string Execute(string command)
    {
        if (_client is null)
        {
            _client = new SshClient(_droplet.Host, _droplet.Username, _droplet.Password)
            {
                // Without traffic the link is torn down by the droplet or by NAT, and the next
                // command pays for a reconnect. A keepalive is far cheaper than the rate limit
                // that reconnecting too often would earn.
                KeepAliveInterval = TimeSpan.FromSeconds(30)
            };
        }

        if (!_client.IsConnected)
        {
            _client.Connect();
        }

        using var sshCommand = _client.CreateCommand(command);
        sshCommand.CommandTimeout = CommandTimeout;

        var response = sshCommand.Execute();
        var error = sshCommand.Error;

        // systemctl and ollama both report real failures on stderr with an empty stdout, so a
        // blank panel would otherwise be the only sign that anything went wrong.
        return string.IsNullOrWhiteSpace(error) ? response : $"{response}\n{error}".Trim();
    }

    private void Drop()
    {
        try
        {
            _client?.Dispose();
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Ignoring failure while discarding a dead droplet SSH session.");
        }

        _client = null;
    }

    public void Dispose()
    {
        _gate.Wait();

        try
        {
            Drop();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }
}
