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

    public double MemoryUsagePercentage()
    {
        var command = UbuntuCommand.ListMemoryUsage();
        var response = RunCommandWithResponse(command);

        var trimmedResponse = response.Substring(14).Trim();
        var usedMemory = trimmedResponse.Remove(trimmedResponse.LastIndexOf('%'));

        return Convert.ToDouble(usedMemory);
    }

    public double CpuUsagePercentage()
    {
        var command = UbuntuCommand.ListCpuUsage();
        var response = RunCommandWithResponse(command);

        var trimmedResponse = response.Substring(10).Trim();
        var usedCpu = trimmedResponse.Remove(trimmedResponse.LastIndexOf('%'));

        return Convert.ToDouble(usedCpu);
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

            // "<mount> <bytes>". A mount point can contain spaces, so take the used figure
            // from the end rather than assuming the line splits into exactly two fields.
            if (parts.Length < 2 || !double.TryParse(parts[^1], out var usedBytes))
            {
                continue;
            }

            mounts.Add(new DiskMount
            {
                Mount = string.Join(' ', parts[..^1]),
                UsedGb = Math.Round(usedBytes / 1024 / 1024 / 1024, 2)
            });
        }

        return mounts.OrderByDescending(mount => mount.UsedGb).ToList();
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