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
    private string ContainerFetchTime { get; set; }

    public double[] MemoryUsageData { get; set; }
    public string[] MemoryUsageLabels { get; set; } = ["Used Memory", "Total Memory"];
    private static double MemoryUsagePercentage { get; set; }

    private double[] CpuUsageData { get; set; }
    private string[] CpuUsageLabels { get; set; } = ["Used CPU", "Total CPU"];
    private static double CpuUsagePercentage { get; set; }

    private string StatFetchTime { get; set; }

    private bool Locked { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Switch.OnInitialized == false)
        {
            MemoryUsagePercentage = 0;
            CpuUsagePercentage = 0;
            await Task.CompletedTask;
            return;
        }

        await ListContainers(isInternalCall: true);
        await ListStats(isInternalCall: true);
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
        await ListContainers(isInternalCall: false);
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
        await ListContainers(isInternalCall: false);
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
        await ListContainers(isInternalCall: false);
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
        await ListContainers(isInternalCall: false);
        CreateSnackbarMessage("Successfully restarted all containers!", Severity.Success);

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

    private async Task ListStats(bool isInternalCall = false)
    {
        if (!isInternalCall)
        {
            Locked = true;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }

        MemoryUsagePercentage = UbuntuService.MemoryUsagePercentage();
        MemoryUsageData = [MemoryUsagePercentage, 100 - MemoryUsagePercentage];

        CpuUsagePercentage = UbuntuService.CpuUsagePercentage();
        CpuUsageData = [CpuUsagePercentage, 100 - CpuUsagePercentage];

        StatFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (!isInternalCall)
        {
            CreateSnackbarMessage("Successfully listed stats!", Severity.Success);
        }

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private void CreateSnackbarMessage(string message, Severity severity)
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        Snackbar.Add(message, severity);
    }
}