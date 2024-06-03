using System.Globalization;
using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BotDashboard.App.Components.Pages;

public partial class Home
{
    [Inject] private DockerService DockerService { get; set; }
    [Inject] private UbuntuService UbuntuService { get; set; }
    private List<Container> Containers { get; set; } = new();
    private static double MemoryUsagePercentage { get; set; }
    public double[] Data { get; set; } = {MemoryUsagePercentage, 100 - MemoryUsagePercentage};
    public string[] Labels { get; set; } = {"Used Memory", "Total Memory"};
    private string ContainerFetchTime { get; set; }
    private string MemoryFetchTime { get; set; }
    
    private void RunImage(string imageName)
    {
        DockerService.RunImage(imageName);
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }

    private void StopContainer(string containerId)
    {
        DockerService.StopContainer(containerId);
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }

    private void StopAllContainers()
    {
        DockerService.StopAllContainers();
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }

    private void RestartContainer(string containerId)
    {
        DockerService.RestartContainer(containerId);
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }

    private void RestartAllContainers()
    {
        DockerService.RestartAllContainers();
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }

    private void ListContainers()
    {
        Containers = DockerService.ListContainers();
        ContainerFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }

    private void ListMemoryUsagePercentage()
    {
        MemoryUsagePercentage = Convert.ToDouble(UbuntuService.MemoryUsagePercentage());
        MemoryFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }
}