namespace BotDashboard.App.Commands;

public class UbuntuCommand
{
    /// <summary>Total and available memory, in kB, straight from the kernel.</summary>
    /// <remarks>
    /// Two lines for the caller to do arithmetic on, rather than an awk program that formats a
    /// sentence. MemAvailable is the kernel's own estimate of what a new workload could claim
    /// without swapping, so total minus available is the figure UGOS shows too -- page cache is
    /// not counted as used, because it is not.
    /// </remarks>
    public string ListMemory()
    {
        return "grep -E '^(MemTotal|MemAvailable):' /proc/meminfo";
    }

    /// <summary>Two snapshots of the aggregate CPU counters, a second apart.</summary>
    /// <remarks>
    /// This replaces <c>top -bn1 | grep "Cpu(s)" | awk '{print 100 - $8}'</c>, which was wrong
    /// twice over. First, a single <c>top</c> iteration reports averages since boot rather than
    /// current load. Second, and worse, top pads its percentages to a fixed width: at exactly
    /// 100% idle the line reads "0.0 ni,100.0 id" with no space after the comma, awk's $8 lands
    /// on "id," instead of the number, and `100 - 0` reports the machine as 100% busy while it
    /// sits idle. Counting jiffies has no field-position guesswork in it at all.
    /// </remarks>
    public string ListCpuSamples()
    {
        return "head -1 /proc/stat; sleep 1; head -1 /proc/stat";
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