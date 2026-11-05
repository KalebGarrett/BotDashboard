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
    [Inject] private NotificationService Notifications { get; set; }

    private List<Container> Containers { get; set; } = new();
    private string ContainerFetchTime { get; set; }

    // The two slices are used and the remainder, so the legend has to say "Free" rather than
    // "Total". Labelled "Total Memory" the ring read as used-against-total, which is a
    // different and much smaller number than the one printed in the middle of it.
    public double[] MemoryUsageData { get; set; } = [0, 100];
    public string[] MemoryUsageLabels { get; set; } = ["Used", "Free"];
    private static double MemoryUsagePercentage { get; set; }

    private double[] CpuUsageData { get; set; } = [0, 100];
    private string[] CpuUsageLabels { get; set; } = ["Busy", "Idle"];
    private static double CpuUsagePercentage { get; set; }

    private string StatFetchTime { get; set; }

    private bool Locked { get; set; }

    /// <summary>
    /// The launch sections, in the order they are shown.
    /// </summary>
    /// <remarks>
    /// Colour carries the weight here: the bots are the everyday buttons and read as such,
    /// while infrastructure and the NAS's own apps are deliberately duller, because starting
    /// Postgres is not a thing to do casually.
    /// </remarks>
    private static readonly (string Heading, ContainerGroup Group, Color Colour)[] LaunchGroups =
    [
        ("Launch a bot", ContainerGroup.Bot, Color.Tertiary),
        ("Music and audio", ContainerGroup.Audio, Color.Tertiary),
        ("Infrastructure", ContainerGroup.Infrastructure, Color.Primary),
        ("NAS apps", ContainerGroup.App, Color.Default)
    ];

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

    private async Task StartContainer(CortexContainer container)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        DockerService.StartContainer(container.Name);
        await ListContainers(isInternalCall: true);
        CreateSnackbarMessage($"Started {container.Display}.", Severity.Success);

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

        FileContainerState();

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

    /// <summary>
    /// Files a bell row for every container that is running badly, and withdraws the rows for
    /// those that have recovered.
    /// </summary>
    /// <remarks>
    /// Deliberately only reports faults -- unhealthy, restarting -- rather than absence. The
    /// list comes from <c>docker ps</c>, so a container that is simply switched off does not
    /// appear here at all, and that is the right behaviour: yt-cipher and the UGOS apps are off
    /// on purpose most of the time, and a bell that nags about them would be ignored within a
    /// day. A container flapping in a restart loop is a different matter.
    /// </remarks>
    private void FileContainerState()
    {
        var faults = new List<(string, string, string, NotificationLevel)>();

        foreach (var container in Containers)
        {
            var status = container.Status ?? string.Empty;
            var name = ContainerDisplay.Name(container);

            if (status.Contains("unhealthy", StringComparison.OrdinalIgnoreCase))
            {
                faults.Add((container.Names, $"{name} is unhealthy",
                    $"Running, but its health check is failing: {status}", NotificationLevel.Warning));
            }
            else if (status.StartsWith("Restarting", StringComparison.OrdinalIgnoreCase))
            {
                faults.Add((container.Names, $"{name} is restarting",
                    $"Docker reports: {status}", NotificationLevel.Warning));
            }
        }

        Notifications.ReconcileState("container:", faults);
    }

    /// <summary>
    /// Maps a <c>docker ps</c> status string onto a <c>.bh-status--*</c> CSS modifier.
    /// Docker reports things like "Up 2 hours", "Restarting (1) 3 seconds ago",
    /// "Exited (0) 5 minutes ago" and "Created".
    /// </summary>
    private static string StatusModifier(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "bh-status--idle";
        }

        if (status.StartsWith("Up", StringComparison.OrdinalIgnoreCase))
        {
            // "Up 5 minutes (unhealthy)" is running, but not happily.
            return status.Contains("unhealthy", StringComparison.OrdinalIgnoreCase)
                ? "bh-status--idle"
                : "bh-status--up";
        }

        if (status.Contains("Exit", StringComparison.OrdinalIgnoreCase)
            || status.Contains("Dead", StringComparison.OrdinalIgnoreCase))
        {
            return "bh-status--down";
        }

        return "bh-status--idle";
    }
}