using System.Globalization;
using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;

namespace BotDashboard.App.Components.Pages;

public partial class Home
{
    [Inject] private DockerService DockerService { get; set; }
    [Inject] private UbuntuService UbuntuService { get; set; }
    private List<Container> Containers { get; set; } = new();
    private static double MemoryUsagePercentage { get; set; }
    public double[] Data { get; set; }
    public string[] Labels { get; set; } = {"Used Memory", "Total Memory"};
    private string ContainerFetchTime { get; set; }
    private string MemoryFetchTime { get; set; }

    protected override async Task OnInitializedAsync()
    {
      if (Switch.OnInitialized == false)
      {
          MemoryUsagePercentage = 0;
          await Task.CompletedTask;
          return;
      }
      
      ListContainers();
      ListMemoryUsagePercentage();
    }
    
    private void RunImage(string imageName)
    {
        DockerService.RunImage(imageName);
        ListContainers();
    }

    private void StopContainer(string containerId)
    {
        DockerService.StopContainer(containerId);
        ListContainers();
    }

    private void StopAllContainers()
    {
        DockerService.StopAllContainers();
        ListContainers();
    }

    private void RestartContainer(string containerId)
    {
        DockerService.RestartContainer(containerId);
        ListContainers();
    }

    private void RestartAllContainers()
    {
        DockerService.RestartAllContainers();
        ListContainers();
    }

    private void ListContainers()
    {
        Containers = DockerService.ListContainers();
        ContainerFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }

    private void ListMemoryUsagePercentage()
    {
        MemoryUsagePercentage = UbuntuService.MemoryUsagePercentage();
        Data = [MemoryUsagePercentage, 100 - MemoryUsagePercentage];
        MemoryFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }
}