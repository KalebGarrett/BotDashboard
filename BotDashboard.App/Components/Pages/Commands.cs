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
    private string[] Headings { get; set; } = ["Repository", "Tag", "Image Id", "Created", "Size", "Actions"];
    private string CommandResult { get; set; }
    private string ImageFetchTime { get; set; }
    private string ContainerFetchTime { get; set; }
    private List<Container> Containers { get; set; } = new();
    
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
        CommandResult = DockerService.PullImage(imageName);
        ListImages();
    }

    private void RemoveImage(string imageId)
    {
        CommandResult = DockerService.RemoveImage(imageId);
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
}