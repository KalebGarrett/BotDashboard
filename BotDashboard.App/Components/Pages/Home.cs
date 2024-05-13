using System.Diagnostics;
using BotDashboard.App.Secrets;
using BotDashboard.App.Services;
using Microsoft.AspNetCore.Components;
using Renci.SshNet;

namespace BotDashboard.App.Components.Pages;

public partial class Home
{
    [Inject]
    private DigitalOceanService DigitalOceanService { get; set; }
    
    private void RunImage(string imageName)
    {
        using var client = new SshClient(DigitalOcean.Host, DigitalOcean.Username, DigitalOcean.Password);
        DigitalOceanService.RunImage(imageName);
    }
    
    private void StopImage(string containerName)
    {
        DigitalOceanService.StopImage(containerName);
    }
}