namespace BotDashboard.App.Credentials;

/// <summary>
/// Where the Ollama server lives, as seen from Bot Harbor.
/// </summary>
/// <remarks>
/// Ollama stays on the DigitalOcean droplet. It binds to the droplet's loopback and has no
/// authentication of its own, so the only route to it is the tailnet.
///
/// Bot Harbor itself is not on the tailnet and does not need to be: it reaches the public
/// internet through Cloudflare, and its own Tailscale sidecar was retired with that move. The
/// node that still matters is <c>bots-tailscale</c> on Cortex, whose network namespace Jam
/// Junction runs inside -- so the probe is issued from that container rather than from here.
/// Testing it anywhere else would answer a question nobody asked.
/// </remarks>
public class OllamaEndpoint
{
    public readonly string Host;
    public readonly int Port;

    /// <summary>
    /// The container on Cortex whose network namespace Jam Junction shares.
    /// </summary>
    /// <remarks>
    /// Configurable because it is a fact about the bots stack rather than about this app: the
    /// name comes from <c>container_name</c> in deploy/bots/docker-compose.yml, and a rename
    /// there would otherwise silently break the panel.
    /// </remarks>
    public readonly string ProbeContainer;

    public OllamaEndpoint(string host, int port, string probeContainer)
    {
        Host = host;
        Port = port;
        ProbeContainer = string.IsNullOrWhiteSpace(probeContainer) ? "bots-tailscale" : probeContainer;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && Port > 0;

    public string BaseUrl => $"http://{Host}:{Port}";
}
