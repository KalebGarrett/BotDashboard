using BotDashboard.App.Commands;
using BotDashboard.App.Events;
using BotDashboard.App.Secrets;
using BotDashboard.Models;
using Renci.SshNet;

namespace BotDashboard.App.Services;

public class DockerService
{
    public void RunImage(string imageName)
    {
        var dockerCommand = new DockerCommand();
        var command = dockerCommand.Run(imageName);
        RunCommand(command);
    }

    public void StopImage(string containerId)
    {
        var dockerCommand = new DockerCommand();
        var command = dockerCommand.Stop(containerId);
        RunCommand(command);
    }

    public void StopAllImages()
    {
        var dockerCommand = new DockerCommand();
        var command = dockerCommand.StopAll();
        RunCommand(command);
    }

    public void RestartAllImages()
    {
        var dockerCommand = new DockerCommand();
        var command = dockerCommand.RestartAll();
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

        client.HostKeyReceived -= SshEvents.ClientOnHostKeyReceived;
        client.Disconnect();
        
        return response.Split("\n")
            .Skip(1)
            .SkipLast(1)
            .Select(str => str.Split("   ")
                .Where(s => s != "").ToArray())
            .Select(row => new Containers
            {
                ContainerId = row[0].Trim(),
                Image = row[1].Trim(),
                Command = row[2].Trim(),
                Created = row[3].Trim(),
                Status = row[4].Trim(),
                Names = row[5].Trim()
            })
            .OrderBy(container => container.Image)
            .ToList();
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