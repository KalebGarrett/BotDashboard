using BotDashboard.App.Commands;
using BotDashboard.App.Credentials;
using BotDashboard.Models;
using Renci.SshNet;

namespace BotDashboard.App.Services;

public class UbuntuService
{
    private readonly NasCredentials _nas;

    public UbuntuService(NasCredentials nas)
    {
        _nas = nas;
    }
    
    private UbuntuCommand UbuntuCommand { get; } = new();

    /// <summary>Memory in use as a percentage of the total.</summary>
    public double MemoryUsagePercentage()
    {
        var response = RunCommandWithResponse(UbuntuCommand.ListMemory());

        double total = 0;
        double available = 0;

        foreach (var line in response.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(':', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            // "MemTotal:        7896808 kB" -- take the number, drop the unit.
            var figure = parts[1].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (figure.Length == 0 || !double.TryParse(figure[0], out var kilobytes))
            {
                continue;
            }

            if (parts[0].Trim() == "MemTotal")
            {
                total = kilobytes;
            }
            else if (parts[0].Trim() == "MemAvailable")
            {
                available = kilobytes;
            }
        }

        return total <= 0 ? 0 : Math.Round((total - available) / total * 100, 2);
    }

    /// <summary>CPU busy as a percentage, measured across a one second window.</summary>
    public double CpuUsagePercentage()
    {
        var samples = RunCommandWithResponse(UbuntuCommand.ListCpuSamples())
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith("cpu ", StringComparison.Ordinal))
            .Select(ParseCpuSample)
            .ToArray();

        if (samples.Length < 2)
        {
            return 0;
        }

        var busyAndIdle = samples[1].Total - samples[0].Total;
        var idle = samples[1].Idle - samples[0].Idle;

        // A counter that did not move means the window caught nothing, not a busy machine.
        return busyAndIdle <= 0 ? 0 : Math.Round((busyAndIdle - idle) / busyAndIdle * 100, 2);
    }

    /// <summary>
    /// Reads one <c>/proc/stat</c> aggregate line into total and idle jiffies.
    /// </summary>
    /// <remarks>
    /// The fields are user, nice, system, idle, iowait, irq, softirq, steal, guest and
    /// guest_nice; newer kernels may append more, so the total sums whatever is there rather
    /// than a fixed ten. iowait counts as idle: the CPU had nothing to run.
    /// </remarks>
    private static (double Total, double Idle) ParseCpuSample(string line)
    {
        var fields = line
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(field => double.TryParse(field, out var value) ? value : 0)
            .ToArray();

        var idle = fields.Length > 4 ? fields[3] + fields[4] : 0;

        return (fields.Sum(), idle);
    }

    /// <summary>Total size of Cortex's root filesystem, in GiB.</summary>
    public double DiskTotal()
    {
        var command = UbuntuCommand.ListDiskTotal(_nas.DiskPath);
        var response = RunCommandWithResponse(command);

        return Convert.ToDouble(response);
    }

    public double DiskUsage()
    {
        var command = UbuntuCommand.ListDiskUsage(_nas.DiskPath);
        var response = RunCommandWithResponse(command);

        return Convert.ToDouble(response);
    }

    /// <summary>What the dashboard was told to connect to, without connecting.</summary>
    /// <remarks>
    /// Worth having on its own. When the connection fails, the configured address and account
    /// are exactly what a reader needs in order to work out why, and a page showing nothing but
    /// an exception message leaves them guessing.
    /// </remarks>
    public CortexStatus ConfiguredTarget()
    {
        return new CortexStatus
        {
            Host = _nas.Host,
            Username = _nas.Username,
            DiskPath = _nas.DiskPath
        };
    }

    /// <summary>Cortex's identity, uptime and Docker state, in one connection.</summary>
    public CortexStatus Status()
    {
        var response = RunCommandWithResponse(UbuntuCommand.ListStatus());

        var lines = response
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .ToArray();

        string Line(int index) => index < lines.Length && lines[index].Length > 0 ? lines[index] : "-";
        int Count(int index) => int.TryParse(Line(index), out var value) ? value : 0;

        var status = ConfiguredTarget();

        status.Hostname = Line(0);
        status.Kernel = Line(1);
        status.Uptime = Line(2);
        status.DockerVersion = Line(3);
        status.RunningContainers = Count(4);
        status.TotalContainers = Count(5);

        return status;
    }

    public string Uptime()
    {
        var command = UbuntuCommand.ListUptime();
        var response = RunCommandWithResponse(command);

        return response;
    }

    public string BootTime()
    {
        var command = UbuntuCommand.ListBootTime();
        var response = RunCommandWithResponse(command);

        return response;
    }

    /// <summary>
    /// Used space per mounted filesystem, largest first.
    /// </summary>
    public List<DiskMount> DiskBreakdown()
    {
        var response = RunCommandWithResponse(UbuntuCommand.ListDiskBreakdown());
        var mounts = new List<DiskMount>();

        foreach (var line in response.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // "<source> <mount> <bytes>". A mount point can contain spaces, so read the device
            // off the front and the used figure off the end rather than assuming three fields.
            if (parts.Length < 3 || !double.TryParse(parts[^1], out var usedBytes))
            {
                continue;
            }

            mounts.Add(new DiskMount
            {
                Source = parts[0],
                Mount = string.Join(' ', parts[1..^1]),
                UsedGb = Math.Round(usedBytes / 1024 / 1024 / 1024, 2)
            });
        }

        // One filesystem can be mounted in several places -- on Cortex /volume1 and /home are
        // the same device, as are / and /overlay. Charting both would double-count the pool and
        // show two identical strands, so keep the first mount of each device.
        return mounts
            .GroupBy(mount => mount.Source)
            .Select(group => group.First())
            .OrderByDescending(mount => mount.UsedGb)
            .ToList();
    }

    public List<SshLogin> SshLogins()
    {
        var today = DateTime.Today;

        var daysSinceSunday = (int)today.DayOfWeek;

        var weekStart = today.AddDays(-daysSinceSunday);
        var weekEnd = weekStart.AddDays(6);

        var command = UbuntuCommand.ListSshLogins(
            weekStart.ToString("yyyy-MM-dd"), 
            weekEnd.ToString("yyyy-MM-dd"));
        
        var response = RunCommandWithResponse(command);

        var results = new List<SshLogin>();

        foreach (var line in response.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
                continue;

            results.Add(new SshLogin
            {
                Count = int.Parse(parts[0]).ToString(),
                Month = parts[1],
                Day = int.Parse(parts[2]).ToString()
            });
        }

        return results;
    }

    private string RunCommandWithResponse(string command)
    {
        // Fail with a sentence naming the missing setting. Without it SSH.NET throws from deep
        // inside Renci with a message that says nothing about which value is blank, and in a
        // component's OnInitializedAsync that takes the whole Blazor circuit down with it.
        if (!_nas.IsConfigured)
        {
            throw new InvalidOperationException(
                "Cortex SSH settings are missing. Set NAS_HOST, NAS_USERNAME and NAS_PASSWORD "
                + "in .env, then restart the app. See deploy/README.md.");
        }

        using var client = new SshClient(_nas.Host, _nas.Username, _nas.Password);
        client.Connect();

        var response = client.RunCommand(command).Execute();

        client.Disconnect();

        return response;
    }
}