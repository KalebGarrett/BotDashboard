using BotDashboard.App.Credentials;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace BotDashboard.App.Services;

/// <summary>
/// One long-lived SSH connection to the home server PC, shared by everything that talks to it.
/// </summary>
/// <remarks>
/// This started life as the droplet's session, where it was load-bearing: the droplet's UFW
/// rate-limited inbound SSH, and a connection per button locked the dashboard out of it. The
/// server PC is on the LAN and has no such rule, but the shape is still the right one -- a
/// Windows SSH login is slow (it builds a full user profile), and a burst of clicks on the
/// Ollama panel is far quicker as many channels on one connection than as many logins.
///
/// It reconnects on demand, so a dropped link (the PC rebooted for updates, sshd restarted)
/// costs one retry rather than a dead panel.
///
/// Registered as a singleton: the point is that every circuit shares the one connection.
/// </remarks>
public class ServerPcSshSession : IDisposable
{
    /// <summary>
    /// Pulling a model downloads gigabytes, so the default 30s command timeout is not enough.
    /// </summary>
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(10);

    private readonly ServerPcCredentials _serverPc;
    private readonly ILogger<ServerPcSshSession> _logger;

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

    public ServerPcSshSession(ServerPcCredentials serverPc, ILogger<ServerPcSshSession> logger)
    {
        _serverPc = serverPc;
        _logger = logger;
    }

    public bool IsConfigured => _serverPc.IsConfigured;

    /// <summary>
    /// Runs a command on the server PC, reusing the open session where there is one.
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
                // A session can die quietly between uses -- Windows Update reboots the PC,
                // sshd restarts, a NAT mapping expires. One silent reconnect is the whole
                // benefit of keeping it open; a second failure is a real fault and belongs to
                // the caller.
                _logger.LogInformation(exception,
                    "Server PC SSH session failed; reconnecting and retrying once.");

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
            _client = new SshClient(ConnectionInfo())
            {
                // Without traffic an idle link can be torn down underneath us, and the next
                // command pays for a full Windows login. A keepalive is far cheaper.
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
        var error = ReadableError(sshCommand.Error);

        // PowerShell and ollama both report real failures on stderr with an empty stdout, so a
        // blank panel would otherwise be the only sign that anything went wrong.
        return string.IsNullOrWhiteSpace(error) ? response : $"{response}\n{error}".Trim();
    }

    /// <summary>
    /// Offers the password both as plain <c>password</c> auth and as <c>keyboard-interactive</c>.
    /// </summary>
    /// <remarks>
    /// The OpenSSH client falls back to keyboard-interactive on its own, so a server that only
    /// allows that method looks perfectly normal from a terminal. SSH.NET's
    /// <c>SshClient(host, user, password)</c> offers plain password auth and nothing else, and
    /// such a server answers it with "Permission denied (password)" -- indistinguishable from
    /// a wrong password. Offering both makes the dashboard log in wherever a terminal can.
    /// </remarks>
    private Renci.SshNet.ConnectionInfo ConnectionInfo()
    {
        var keyboardInteractive = new KeyboardInteractiveAuthenticationMethod(_serverPc.Username);
        keyboardInteractive.AuthenticationPrompt += (_, prompt) =>
        {
            // Windows sshd asks a single "Password:" question. Answer anything else blank
            // rather than leak the password into a prompt it was not asked for.
            foreach (var question in prompt.Prompts)
            {
                question.Response = question.Request.Contains("password", StringComparison.OrdinalIgnoreCase)
                    ? _serverPc.Password
                    : string.Empty;
            }
        };

        return new Renci.SshNet.ConnectionInfo(_serverPc.Host, _serverPc.Username,
            new PasswordAuthenticationMethod(_serverPc.Username, _serverPc.Password),
            keyboardInteractive);
    }

    /// <summary>
    /// Unwraps PowerShell's CLIXML stderr into the error text it carries.
    /// </summary>
    /// <remarks>
    /// powershell.exe with redirected streams serialises its error stream as
    /// <c>#&lt; CLIXML</c> followed by an XML document, with newlines encoded as
    /// <c>_x000D__x000A_</c>. Shown raw, a one-line "service not found" becomes a screen of
    /// XML; the useful part is the text of the <c>S="Error"</c> elements.
    /// </remarks>
    private static string ReadableError(string error)
    {
        if (string.IsNullOrWhiteSpace(error) || !error.TrimStart().StartsWith("#< CLIXML", StringComparison.Ordinal))
        {
            return error;
        }

        var lines = System.Text.RegularExpressions.Regex
            .Matches(error, "<S S=\"Error\">(.*?)</S>", System.Text.RegularExpressions.RegexOptions.Singleline)
            .Select(match => System.Net.WebUtility.HtmlDecode(match.Groups[1].Value)
                .Replace("_x000D_", string.Empty)
                .Replace("_x000A_", "\n"));

        return string.Concat(lines).Trim();
    }

    private void Drop()
    {
        try
        {
            _client?.Dispose();
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Ignoring failure while discarding a dead server PC SSH session.");
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
