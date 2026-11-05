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

    public string ListDiskUsage(string path)
    {
        return "df -B1 --output=used " + path + " | tail -n 1 | awk '{printf \"%.2f\\n\", $1/1024/1024/1024}'\n";
    }

    /// <summary>
    /// Total size of the root filesystem, in GiB.
    /// </summary>
    /// <summary>
    /// Every real mounted filesystem and its used bytes.
    /// </summary>
    /// <remarks>
    /// Deliberately returns raw df output for the caller to parse, rather than shaping it
    /// with awk. The quoting needed to embed an awk program in a C# string that then
    /// travels over SSH is a reliable source of silent breakage.
    ///
    /// tmpfs, devtmpfs and squashfs are excluded: they are kernel bookkeeping and loopback
    /// images, not storage anyone can fill.
    /// </remarks>
    public string ListDiskBreakdown()
    {
        return "df -B1 --output=target,used -x tmpfs -x devtmpfs -x squashfs -x efivarfs | tail -n +2";
    }

    /// <remarks>
    /// The disk gauge used to divide by a hardcoded 25, the droplet's size. Cortex's root is
    /// about 107 GiB, so that arithmetic would have reported several hundred percent used.
    /// </remarks>
    public string ListDiskTotal(string path)
    {
        return "df -B1 --output=size " + path + " | tail -n 1 | awk '{printf \"%.2f\\n\", $1/1024/1024/1024}'\n";
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