namespace BotDashboard.Models;

/// <summary>
/// Two independent answers about Ollama, because they fail separately.
/// </summary>
/// <remarks>
/// The Windows service can be <c>Running</c> while the HTTP port answers nothing, and the
/// tailnet path can be down while the service itself is healthy. Jam Junction only cares about
/// the second: it speaks HTTP to the port and never looks at the service. Reporting a single state
/// would hide whichever half is actually broken.
/// </remarks>
public class OllamaHealth
{
    /// <summary>What <c>Get-Service Ollama</c> says, over SSH to the server PC.</summary>
    public string ServiceState { get; set; } = "unknown";

    /// <summary>True when the bots sidecar on Cortex reached the HTTP API across the tailnet.</summary>
    public bool Reachable { get; set; }

    /// <summary>Round trip for that HTTP call.</summary>
    public long LatencyMs { get; set; }

    /// <summary>The address Bot Harbor tried, so a wrong one is visible rather than guessed at.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Models the server reports, which proves the reply really came from Ollama.</summary>
    public List<string> Models { get; set; } = new();

    /// <summary>Why the HTTP call failed, when it did.</summary>
    public string Error { get; set; }

    public bool ServiceActive =>
        string.Equals(ServiceState, "Running", StringComparison.OrdinalIgnoreCase);

    /// <summary>Healthy only when the service is up and Cortex can actually talk to it.</summary>
    public bool Healthy => ServiceActive && Reachable;
}
