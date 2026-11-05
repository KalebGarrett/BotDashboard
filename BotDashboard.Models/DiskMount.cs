namespace BotDashboard.Models;

/// <summary>
/// One mounted filesystem and how much of it is in use.
/// </summary>
public class DiskMount
{
    public string Mount { get; set; }
    public double UsedGb { get; set; }
}
