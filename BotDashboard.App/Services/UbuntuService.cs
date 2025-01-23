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

        var trimmedResponse = response.Substring(14).Trim();
        var usedMemory = trimmedResponse.Remove(trimmedResponse.LastIndexOf('%'));
        
        return Convert.ToDouble(usedMemory);
    }

    public double CpuUsagePercentage()
    {
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.Connect();

        var command = client.CreateCommand(_UbuntuCommand.ListCpuUsage());
        var response = command.Execute();

        client.Disconnect();
        
        var trimmedResponse = response.Substring(10).Trim();
        var usedCpu = trimmedResponse.Remove(trimmedResponse.LastIndexOf('%'));
        
        return Convert.ToDouble(usedCpu);
    }
}