using System.Globalization;
using BotDashboard.App.Constants;
using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BotDashboard.App.Components.Pages;

public partial class Commands
{
    [Inject] public DockerService DockerService { get; set; }

    private List<DockerImage> DockerImages { get; set; } = new();
    private string ImageFetchTime { get; set; }

    private string[] Headings { get; set; } = ["Repository", "Tag", "Image Id", "Created", "Size", "Actions"];

    private List<Container> Containers { get; set; } = new();
    private string ContainerFetchTime { get; set; }

    private string Output { get; set; }
    private string OutputFetchTime { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Switch.OnInitialized == false)
        {
            await Task.CompletedTask;
            return;
        }

        ListContainers(showSnackbar: false);
        ListImages(showSnackbar: false);
    }

    private void RunImage(string imageName)
    {
        DockerService.RunImage(imageName);
        ListContainers(showSnackbar: false);
        CreateSnackbarMessage("Successfully ran image!", Severity.Success);
    }

    private void StopContainer(string containerId)
    {
        DockerService.StopContainer(containerId);
        ListContainers(showSnackbar: false);
        CreateSnackbarMessage("Successfully stopped container!", Severity.Success);
    }

    private void StopAllContainers()
    {
        DockerService.StopAllContainers();
        ListContainers(showSnackbar: false);
        CreateSnackbarMessage("Successfully stopped all containers!", Severity.Success);
    }

    private void RestartContainer(string containerId)
    {
        DockerService.RestartContainer(containerId);
        ListContainers(showSnackbar: false);
        CreateSnackbarMessage("Successfully restarted container!", Severity.Success);
    }

    private void RestartAllContainers()
    {
        DockerService.RestartAllContainers();
        ListContainers(showSnackbar: false);
        CreateSnackbarMessage("Successfully restarted all containers!", Severity.Success);
    }

    private void PullImage(string imageName)
    {
        Output = DockerService.PullImage(imageName);
        ListImages();
        CreateSnackbarMessage("Successfully pulled image!", Severity.Success);
    }

    private void RemoveImage(string imageId)
    {
        Output = DockerService.RemoveImage(imageId);
        ListImages();
        CreateSnackbarMessage("Successfully removed image!", Severity.Success);
    }

    private void ListImages(bool showSnackbar = true)
    {
        DockerImages = DockerService.ListImages();
        ImageFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
        
        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed images!", Severity.Success);
        }
    }

    private void ListContainers(bool showSnackbar = true)
    {
        Containers = DockerService.ListContainers();
        ContainerFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
        
        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed containers!", Severity.Success);
        }
    }

    private void LogContainer(string containerId)
    {
        Output = DockerService.LogContainerCommand(containerId);
        OutputFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
        CreateSnackbarMessage("Successfully logged container!", Severity.Success);
    }

    private void CreateSnackbarMessage(string message, Severity severity)
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        Snackbar.Add(message, severity);
    }
}