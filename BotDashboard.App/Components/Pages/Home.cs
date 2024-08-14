using System.Globalization;
using BotDashboard.App.Constants;
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
    public double[] Data { get; set; }
    public string[] Labels { get; set; } = {"Used Memory", "Total Memory"};
    private string ContainerFetchTime { get; set; }
    private string MemoryFetchTime { get; set; }

    protected override async Task OnInitializedAsync()
    {
        MemoryUsagePercentage = 0;
    }

    private void StartAllImages()
    {
        DockerService.RunImage(DockerRepository.JokeBot);
        DockerService.RunImage(DockerRepository.TriviaBot);
        DockerService.RunImage(DockerRepository.FactBot);
        DockerService.RunImage(DockerRepository.PremBot);
        DockerService.RunImage(DockerRepository.JamJunction);
        Task.Delay(TimeSpan.FromSeconds(3));
        ListContainers();
    }
    
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
        MemoryUsagePercentage = Math.Round(Convert.ToDouble(UbuntuService.MemoryUsagePercentage()));
        Data = new[] {MemoryUsagePercentage, 100 - MemoryUsagePercentage};
        MemoryFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }
}