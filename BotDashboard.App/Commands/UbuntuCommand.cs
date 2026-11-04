namespace BotDashboard.App.Commands;

public class UbuntuCommand
{
    public string ListMemoryUsage()
    {
        return "free | awk '/Mem:/ {printf(\"Memory Usage: %.2f%%\\n\", $3/$2 * 100.0)}'\n";
    }

    public string ListCpuUsage()
    {
        return "top -bn1 | grep \"Cpu(s)\" | awk '{print \"CPU Usage: \" 100 - $8 \"%\"}'\n";
    }

    public string ListDiskUsage()
    {
        return "df -B1 --output=used / | tail -n 1 | awk '{printf \"%.2f\\n\", $1/1024/1024/1024}'\n";
    }

    /// <summary>
    /// Total size of the root filesystem, in GiB.
    /// </summary>
    /// <remarks>
    /// The disk gauge used to divide by a hardcoded 25, the droplet's size. Cortex's root is
    /// about 107 GiB, so that arithmetic would have reported several hundred percent used.
    /// </remarks>
    public string ListDiskTotal()
    {
        return "df -B1 --output=size / | tail -n 1 | awk '{printf \"%.2f\\n\", $1/1024/1024/1024}'\n";
    }

    public string ListUptime()
    {
        return "uptime -p";
    }

    public string ListBootTime()
    {
        return "who -b";
    }

    public string ListSshLogins(string weekStart, string weekEnd)
    {
        return
            $"journalctl --no-pager -u ssh -u sshd \\\n" +
            $"  --since \"{weekStart}\" \\\n" +
            $"  --until \"{weekEnd}\" |\n" +
            $"grep \"Accepted\" |\n" +
            $"awk '{{print $1, $2}}' |\n" +
            $"sort |\n" +
            $"uniq -c";
    }
}