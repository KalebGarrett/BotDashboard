using BotDashboard.App.Commands;
using Renci.SshNet;

namespace BotDashboard.App.Services;

public class DigitalOceanService
{
    public readonly SshClient _sshClient;
    public readonly DockerCommand _dockerCommand;

    public DigitalOceanService(SshClient sshClient, DockerCommand dockerCommand)
    {
        _sshClient = sshClient;
        _dockerCommand = dockerCommand;
    }

    public void RunImage(string imageName)
    {
        _sshClient.Connect();
        _sshClient.RunCommand($"{_dockerCommand.Run(imageName)}");
        _sshClient.Disconnect();
    }
}