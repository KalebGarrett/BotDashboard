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

        var command = client.CreateCommand(_UbuntuCommand.ListMemoryStats());
        var response = command.Execute();

        client.HostKeyReceived -= SshEvents.ClientOnHostKeyReceived;
        client.Disconnect();

        return response.Remove(0, 83).Substring(0,2);
    }
}