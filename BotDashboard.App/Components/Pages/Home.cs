using System.Globalization;
using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;

namespace BotDashboard.App.Components.Pages;

public partial class Home
{
    [Inject]
    private DockerService DockerService { get; set; }
    private List<Containers> Containers { get; set; } = new List<Containers>();
    private string Time { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        ListContainers();
    }
    
    private void RunImage(string imageName)
    {
        DockerService.RunImage(imageName);
        //Add locking mechanism if already running
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }
    
    private void StopImage(string containerId)
    {
        DockerService.StopImage(containerId);
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }

    private void StopAllImages()
    {
        DockerService.StopAllImages();
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }
    
    private void RestartAllImages()
    {
        DockerService.RestartAllImages();
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }
    
    private void ListContainers()
    {
        Containers = DockerService.ListContainers();
        Time = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }
}