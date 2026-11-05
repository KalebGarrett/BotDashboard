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
        return "df -B1 --output=source,target,used -x tmpfs -x devtmpfs -x squashfs -x efivarfs | tail -n +2";
    }

    /// <remarks>
    /// The disk gauge used to divide by a hardcoded 25, the droplet's size. Cortex's root is
    /// about 107 GiB, so that arithmetic would have reported several hundred percent used.
    /// </remarks>
    public string ListDiskTotal(string path)
    {
        return "df -B1 --output=size " + path + " | tail -n 1 | awk '{printf \"%.2f\\n\", $1/1024/1024/1024}'\n";
    }

    /// <summary>
    /// A fixed six-line report on the machine, for the Manage Cortex page.
    /// </summary>
    /// <remarks>
    /// One round trip rather than six: every SSH connection to Cortex costs a handshake, and
    /// opening a page should not pay for six of them. Each line falls back to "-" so a command
    /// that is missing or not permitted shifts nothing below it -- the reader parses by
    /// position, and a silently absent line would relabel everything after it.
    /// </remarks>
    public string ListStatus()
    {
        return "hostname || echo -\n"
               + "uname -r || echo -\n"
               + "uptime -p || echo -\n"
               + "docker version --format '{{.Server.Version}}' 2>/dev/null || echo -\n"
               + "docker ps -q 2>/dev/null | wc -l || echo -\n"
               + "docker ps -aq 2>/dev/null | wc -l || echo -\n";
    }

    public string ListUptime()
    {
        return "uptime -p";
    }

    /// <summary>When the machine last booted.</summary>
    /// <remarks>
    /// Not <c>who -b</c>: that reads the utmp boot record, which UGOS never writes, so on
    /// Cortex it reports "system boot 1969-12-31 23:59" -- the Unix epoch, which the page then
    /// rendered as a 1969 calendar. <c>uptime -s</c> derives the same answer from /proc.
    /// </remarks>
    public string ListBootTime()
    {
        return "uptime -s";
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