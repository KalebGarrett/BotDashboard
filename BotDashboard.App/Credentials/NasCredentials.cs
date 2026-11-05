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

    /// <summary>
    /// The filesystem the Stats page reports on.
    /// </summary>
    /// <remarks>
    /// Not <c>/</c>. On Cortex that is a 107 GB overlay holding the OS, while the storage that
    /// matters -- and that the bots and their volumes actually live on -- is the 11 TB pool at
    /// /volume1. Reporting the root would show a nearly empty disk and hide the real one.
    /// </remarks>
    public readonly string DiskPath;

    public NasCredentials(string host, string username, string password, string diskPath)
    {
        Host = host;
        Username = username;
        Password = password;
        DiskPath = string.IsNullOrWhiteSpace(diskPath) ? "/volume1" : diskPath;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password);
}
