using BotDashboard.App.Commands;
using BotDashboard.App.Secrets;
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

    private void RunCommand(string command)
    {
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        client.Connect();
        client.RunCommand(command);
        client.Disconnect();
    }
}