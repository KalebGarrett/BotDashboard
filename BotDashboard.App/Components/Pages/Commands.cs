using System.Globalization;
using BotDashboard.App.Constants;
using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;

namespace BotDashboard.App.Components.Pages;

public partial class Commands
{
    [Inject] public DockerService DockerService { get; set; }
    
    private List<DockerImage> DockerImages { get; set; } = new();
    private string ImageFetchTime { get; set; }
    
    private string[] Headings { get; set; } = ["Repository", "Tag", "Image Id", "Created", "Size", "Actions"];
    
    private List<Container> Containers { get; set; } = new();
    private string ContainerFetchTime { get; set; }
    private string ContainerLogs { get; set; }
    private string ContainerId { get; set; }
    private Dictionary<string, string> ContainerLogsMap { get; set; } = new();
    
    private bool Open { get; set; }
    
    private string Output { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        if (Switch.OnInitialized == false)
        {
            await Task.CompletedTask;
            return;
        }
        
        ListContainers();
        ListImages();
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

    private void PullImage(string imageName)
    {
        Output = DockerService.PullImage(imageName);
        ListImages();
    }

    private void RemoveImage(string imageId)
    {
        Output = DockerService.RemoveImage(imageId);
        ListImages();
    }
    
    private void ListImages()
    {
        DockerImages = DockerService.ListImages();
        ImageFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }

    private void ListContainers()
    {
        Containers = DockerService.ListContainers();
        ContainerFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
    }
    
    private void LogContainer(string containerId)
    {
        Output = DockerService.LogContainerCommand(containerId);
    }
}