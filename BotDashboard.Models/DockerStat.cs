namespace BotDashboard.Models;

public class DockerStat
{
    public string ContainerId { get; set; }
    public string Name { get; set; }
    public string Cpu { get; set; }
    public string MemoryUsageLimit { get; set; }
    public string MemoryPercentage { get; set; }
    public string NetIo { get; set; }
    public string BlockIo { get; set; }
    public string Pids { get; set; }
}