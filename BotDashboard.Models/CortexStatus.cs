namespace BotDashboard.Models;

/// <summary>
/// What Cortex is and what it is running, as one SSH round trip returns it.
/// </summary>
/// <remarks>
/// Host, Username and DiskPath come from configuration rather than from the NAS: they say
/// what the dashboard was told to connect to, which is the useful thing to show when it
/// cannot connect at all.
/// </remarks>
public class CortexStatus
{
    public string Host { get; set; }
    public string Username { get; set; }
    public string DiskPath { get; set; }
    public string Hostname { get; set; }
    public string Kernel { get; set; }
    public string Uptime { get; set; }
    public string DockerVersion { get; set; }
    public int RunningContainers { get; set; }
    public int TotalContainers { get; set; }
}
