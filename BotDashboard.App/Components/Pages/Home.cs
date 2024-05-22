using System.Diagnostics;
using System.Globalization;
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
        ListContainers();
    }
    
    private void ListContainers()
    {
        Containers = DigitalOceanService.ListContainers();
        Time = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }
    
    private void StopImage(string containerId)
    {
        DigitalOceanService.StopImage(containerId);
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }

    private void RunImage(string imageName)
    {
        DigitalOceanService.RunImage(imageName);
        //Add locking mechanism if already running
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }
}