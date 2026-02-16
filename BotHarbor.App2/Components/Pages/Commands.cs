using System.Globalization;
using BotDashboard.App;
using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BotHarbor.App2.Components.Pages;

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
    
    private bool Locked { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Switch.OnInitialized == false)
        {
            await Task.CompletedTask;
            return;
        }

        await ListContainers(isInternalCall: true);
        await ListImages(isInternalCall: true);
    }

   private async Task RunImage(string imageName)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        DockerService.RunImage(imageName);
        await ListContainers(isInternalCall: true);
        CreateSnackbarMessage("Successfully ran image!", Severity.Success);

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }
    
    private async Task RunYtCipher()
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        
        DockerService.RunYtCipher();
        await ListContainers(isInternalCall: true);
        CreateSnackbarMessage("Successfully ran image!", Severity.Success);

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task StopContainer(string containerId)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        DockerService.StopContainer(containerId);
        await ListContainers(isInternalCall: true);
        CreateSnackbarMessage("Successfully stopped container!", Severity.Success);

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task StopAllContainers()
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        DockerService.StopAllContainers();
        await ListContainers(isInternalCall: true);
        CreateSnackbarMessage("Successfully stopped all containers!", Severity.Success);

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task RestartContainer(string containerId)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        DockerService.RestartContainer(containerId);
        await ListContainers(isInternalCall: true);
        CreateSnackbarMessage("Successfully restarted container!", Severity.Success);

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task RestartAllContainers()
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        DockerService.RestartAllContainers();
        await ListContainers(isInternalCall: true);
        CreateSnackbarMessage("Successfully restarted all containers!", Severity.Success);

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task PullImage(string imageName)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        
        Output = DockerService.PullImage(imageName);
        await ListImages(isInternalCall: true);
        CreateSnackbarMessage("Successfully pulled image!", Severity.Success);
        
        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task RemoveImage(string imageId)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        
        Output = DockerService.RemoveImage(imageId);
        await ListImages(isInternalCall: true);
        CreateSnackbarMessage("Successfully removed image!", Severity.Success);
        
        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ListImages(bool isInternalCall = false)
    {
        if (!isInternalCall)
        {
            Locked = true;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }
        
        DockerImages = DockerService.ListImages();
        ImageFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
        
        if (!isInternalCall)
        {
            CreateSnackbarMessage("Successfully listed images!", Severity.Success);
        }
        
        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ListContainers(bool isInternalCall = false)
    {
        if (!isInternalCall)
        {
            Locked = true;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }

        Containers = DockerService.ListContainers();
        ContainerFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (!isInternalCall)
        {
            CreateSnackbarMessage("Successfully listed containers!", Severity.Success);
        }

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task LogContainer(string containerId)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        
        Output = DockerService.LogContainerCommand(containerId);
        OutputFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
        CreateSnackbarMessage("Successfully logged container!", Severity.Success);
        
        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private void CreateSnackbarMessage(string message, Severity severity)
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        Snackbar.Add(message, severity);
    }
}