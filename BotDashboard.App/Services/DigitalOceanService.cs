using System.ComponentModel;
using BotDashboard.App.Commands;
using BotDashboard.App.Events;
using BotDashboard.App.Secrets;
using BotDashboard.Models;
using Renci.SshNet;

namespace BotDashboard.App.Services;

public class DigitalOceanService
{
    public void RunImage(string imageName)
    {
        var dockerCommand = new DockerCommand();
        var command = dockerCommand.Run(imageName);
        RunCommand(command);
    }

    public void StopImage(string containerName)
    {
        var dockerCommand = new DockerCommand();
        var command = dockerCommand.Stop(containerName);
        RunCommand(command);
    }
    
    public List<Containers> ListContainers()
    {
        var dockerCommand = new DockerCommand();
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.HostKeyReceived += SshEvents.ClientOnHostKeyReceived;
        client.Connect();

        var command = client.CreateCommand(dockerCommand.ListContainers());
        var response = command.Execute();
        Console.WriteLine(response); //Remove test case

        client.HostKeyReceived -= SshEvents.ClientOnHostKeyReceived;
        client.Disconnect();

        return response.ToList();
    }

    private void RunCommand(string command)
    {
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.HostKeyReceived += SshEvents.ClientOnHostKeyReceived;
        client.Connect();
        client.RunCommand(command);
        client.HostKeyReceived -= SshEvents.ClientOnHostKeyReceived;
        client.Disconnect();
    }
}