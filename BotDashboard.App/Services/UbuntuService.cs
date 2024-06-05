using BotDashboard.App.Commands;
using BotDashboard.App.Events;
using BotDashboard.App.Secrets;
using BotDashboard.Models;
using Renci.SshNet;

namespace BotDashboard.App.Services;

public class UbuntuService
{
    public readonly UbuntuCommand _UbuntuCommand;

    public UbuntuService(UbuntuCommand ubuntuCommand)
    {
        _UbuntuCommand = ubuntuCommand;
    }
    
    public string MemoryUsagePercentage()
    {
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.HostKeyReceived += SshEvents.ClientOnHostKeyReceived;
        client.Connect();

        var command = client.CreateCommand(_UbuntuCommand.ListMemoryUsage());
        var response = command.Execute();

        client.HostKeyReceived -= SshEvents.ClientOnHostKeyReceived;
        client.Disconnect();

        var trimmedResponse = response.Substring(14);
        trimmedResponse = trimmedResponse.Remove(trimmedResponse.Length - 2);
        return trimmedResponse;
    }

    public string CpuUsagePercentage()
    {
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.HostKeyReceived += SshEvents.ClientOnHostKeyReceived;
        client.Connect();

        var command = client.CreateCommand(_UbuntuCommand.ListCpuUsage());
        var response = command.Execute();

        client.HostKeyReceived -= SshEvents.ClientOnHostKeyReceived;
        client.Disconnect();

        var trimmedResponse = response.Substring(11);
        trimmedResponse = trimmedResponse.Remove(trimmedResponse.Length - 2);
        return trimmedResponse;
    }
}