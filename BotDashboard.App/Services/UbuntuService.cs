using BotDashboard.App.Commands;
using BotDashboard.App.Secrets;
using BotDashboard.Models;
using Renci.SshNet;

namespace BotDashboard.App.Services;

public class UbuntuService
{
    private readonly UbuntuCommand _UbuntuCommand;

    public UbuntuService(UbuntuCommand ubuntuCommand)
    {
        _UbuntuCommand = ubuntuCommand;
    }

    public double MemoryUsagePercentage()
    {
        var command = _UbuntuCommand.ListMemoryUsage();
        var response = RunCommandWithResponse(command);

        var trimmedResponse = response.Substring(14).Trim();
        var usedMemory = trimmedResponse.Remove(trimmedResponse.LastIndexOf('%'));

        return Convert.ToDouble(usedMemory);
    }

    public double CpuUsagePercentage()
    {
        var command = _UbuntuCommand.ListCpuUsage();
        var response = RunCommandWithResponse(command);

        var trimmedResponse = response.Substring(10).Trim();
        var usedCpu = trimmedResponse.Remove(trimmedResponse.LastIndexOf('%'));

        return Convert.ToDouble(usedCpu);
    }

    public double DiskUsage()
    {
        var command = _UbuntuCommand.ListDiskUsage();
        var response = RunCommandWithResponse(command);

        return Convert.ToDouble(response);
    }

    public string Uptime()
    {
        var command = _UbuntuCommand.ListUptime();
        var response = RunCommandWithResponse(command);

        return response;
    }

    public string BootTime()
    {
        var command = _UbuntuCommand.ListBootTime();
        var response = RunCommandWithResponse(command);

        return response;
    }

    public List<SshLogin> SshLogins()
    {
        var today = DateTime.Today;

        var daysSinceSunday = (int)today.DayOfWeek;

        var weekStart = today.AddDays(-daysSinceSunday);
        var weekEnd = weekStart.AddDays(6);

        var command = _UbuntuCommand.ListSshLogins(
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
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.Connect();

        var response = client.RunCommand(command).Execute();

        client.Disconnect();

        return response;
    }
}