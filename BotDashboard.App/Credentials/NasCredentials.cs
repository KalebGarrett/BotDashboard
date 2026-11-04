namespace BotDashboard.App.Credentials;

/// <summary>
/// SSH credentials for the Cortex NAS, which is where the bots and their containers run.
/// </summary>
/// <remarks>
/// Bot Harbor itself runs in a container on Cortex, so this is a loopback of sorts: the app
/// SSHes back to its own host. That is deliberate. The alternative, mounting
/// <c>/var/run/docker.sock</c> into the container, is faster but hands anyone who reaches the
/// dashboard root-equivalent control of the NAS, and it cannot serve the Stats page at all --
/// <c>free</c>, <c>top</c> and <c>who -b</c> read the host, not a container.
///
/// The account needs to be in the <c>docker</c> group so <c>docker</c> works without sudo, and
/// in <c>systemd-journal</c> so <c>journalctl</c> can read SSH logins. Both are set up in
/// deploy/README.md; without them the commands fail asking for a password that SSH.NET has no
/// way to supply.
/// </remarks>
public class NasCredentials
{
    public readonly string Host;
    public readonly string Username;
    public readonly string Password;

    public NasCredentials(string host, string username, string password)
    {
        Host = host;
        Username = username;
        Password = password;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password);
}
