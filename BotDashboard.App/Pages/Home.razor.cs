using BotDashboard.App.Commands;
using BotDashboard.App.Constants;
using BotDashboard.App.Secrets;
using BotDashboard.App.Services;
using Renci.SshNet;

namespace BotDashboard.App.Pages;

public partial class Home
{
    public void RunImage()
    {
        var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        var command = new DockerCommand();
        var sshService = new DigitalOceanService(client, command);
        sshService.RunImage(DockerImages.JokeBot);
    }
}