namespace BotDashboard.App.Credentials;

/// <summary>
/// SSH credentials for the home server PC, the Windows machine that runs Lavalink and Ollama.
/// </summary>
/// <remarks>
/// The box runs Windows' built-in OpenSSH server, so whatever arrives here is run by
/// <c>cmd.exe</c> unless the operator has changed the default shell. Every command Bot Harbor
/// sends it is therefore a self-contained <c>powershell -EncodedCommand</c>, which runs the
/// same under either shell -- see <see cref="Commands.OllamaCommand"/>.
///
/// Use the LAN address (192.168.1.218) or the tailnet one; both reach the same sshd. The
/// account must be a local administrator, or starting and stopping the Ollama service fails
/// with "Access is denied".
/// </remarks>
public class ServerPcCredentials
{
    public readonly string Host;
    public readonly string Username;
    public readonly string Password;

    public ServerPcCredentials(string host, string username, string password)
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
