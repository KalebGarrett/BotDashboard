using System.Globalization;
using BotDashboard.App.Commands;
using BotDashboard.App.Secrets;
using Renci.SshNet;

namespace BotDashboard.App.Services;

public class UbuntuService
{
    public readonly UbuntuCommand _UbuntuCommand;

    public UbuntuService(UbuntuCommand ubuntuCommand)
    {
        _UbuntuCommand = ubuntuCommand;
    }
    
    public double MemoryUsagePercentage()
    {
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.Connect();

        var command = client.CreateCommand(_UbuntuCommand.ListMemoryUsage());
        var response = command.Execute();

        client.Disconnect();
        
        var trimmedResponse = double.Parse(response.Substring(19).Trim());
        var installedMemory = 8270224;
        var freeMemory = trimmedResponse / installedMemory * 100;
        var usedMemory = Math.Round(100 - freeMemory);
        
        return usedMemory;
    }

    public double CpuUsagePercentage()
    {
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.Connect();

        var command = client.CreateCommand(_UbuntuCommand.ListCpuUsage());
        var response = command.Execute();

        client.Disconnect();

        var usedCpu = double.Parse(response);
        
        return usedCpu;
    }
}