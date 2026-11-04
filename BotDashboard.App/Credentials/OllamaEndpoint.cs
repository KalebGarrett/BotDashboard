namespace BotDashboard.App.Credentials;

/// <summary>
/// Where the Ollama server lives, as seen from Bot Harbor.
/// </summary>
/// <remarks>
/// Ollama stays on the DigitalOcean droplet. It binds to the droplet's loopback and has no
/// authentication of its own, so the only route to it is the tailnet: Bot Harbor shares the
/// Tailscale sidecar's network namespace, which is what lets it reach the droplet's tailnet
/// address directly rather than having to SSH in and curl from there.
///
/// This is the same path Jam Junction would use if it ran on Cortex, which is why checking it
/// from here is a meaningful test and not just a formality.
/// </remarks>
public class OllamaEndpoint
{
    public readonly string Host;
    public readonly int Port;

    public OllamaEndpoint(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && Port > 0;

    public string BaseUrl => $"http://{Host}:{Port}";
}
