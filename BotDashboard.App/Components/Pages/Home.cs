using System.Diagnostics;
using BotDashboard.App.Secrets;
using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using Renci.SshNet;

namespace BotDashboard.App.Components.Pages;

public partial class Home
{
    [Inject]
    private DigitalOceanService DigitalOceanService { get; set; }
    private List<Containers> Containers { get; set; } = new List<Containers>();
    private string Time { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Containers = DigitalOceanService.ListContainers();
        Time = LastFetched();
    }
    
    private void StopImage(string containerName)
    {
        DigitalOceanService.StopImage(containerName);
    }

    private void ListContainers()
    {
        Containers = DigitalOceanService.ListContainers();
        Time = LastFetched();
    }
    
    private string LastFetched()
    {
        return DateTime.UtcNow.ToLocalTime().ToString();
    }
}