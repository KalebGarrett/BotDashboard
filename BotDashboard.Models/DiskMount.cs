namespace BotDashboard.Models;

/// <summary>
/// One mounted filesystem and how much of it is in use.
/// </summary>
public class DiskMount
{
    /// <summary>The backing device, used to collapse a filesystem mounted twice.</summary>
    public string Source { get; set; }

    public string Mount { get; set; }
    public double UsedGb { get; set; }
}
