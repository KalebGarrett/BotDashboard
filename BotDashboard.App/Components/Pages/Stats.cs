using System.Globalization;
using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BotDashboard.App.Components.Pages;

public partial class Stats
{
    [Inject] private UbuntuService UbuntuService { get; set; }

    private static double MemoryUsagePercentage { get; set; }
    private double[] MemoryUsageData { get; set; } = [0, 100];
    private string[] MemoryUsageLabels { get; set; } = ["Used Memory", "Total Memory"];
    private string MemoryFetchTime { get; set; }

    private static double CpuUsagePercentage { get; set; }
    private double[] CpuUsageData { get; set; } = [0, 100];
    private string[] CpuUsageLabels { get; set; } = ["Used CPU", "Total CPU"];
    private string CpuFetchTime { get; set; }

    // Built from real df output on Cortex. These used to be a hardcoded list of the droplet's
    // top-level directories with invented sizes, which told you nothing once the data moved.
    private List<SankeyChartNode> Nodes { get; set; } = [new("Cortex", 0)];
    private List<SankeyChartEdge> Edges { get; set; } = [];

    private double DiskUsed { get; set; }
    private double DiskTotal { get; set; }
    private static double DiskUsedPercentage { get; set; }
    private double FreeDisk { get; set; }
    private static double FreeDiskPercentage { get; set; }
    private string DiskFetchTime { get; set; }

    private string Uptime { get; set; }
    private DateTime BootTime { get; set; }
    private string UptimeFetchTime { get; set; }

    private AxisChartOptions AxisChartOptions { get; set; } = new();
    private int Index { get; set; } = -1;

    private List<ChartSeries> SshLoginsSeries { get; set; } =
    [
        new() { Name = "SSH Logins", Data = [0, 0, 0, 0, 0, 0, 0] }
    ];

    private string[] SshLoginsXAxisLabels { get; set; } = ["Sun", "Mon", "Tues", "Wed", "Thurs", "Fri", "Sat"];
    private List<SshLogin> SshLogins { get; set; } = new();
    private string SshLoginsFetchTime { get; set; }

    private bool Locked { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Switch.OnInitialized == false)
        {
            MemoryUsagePercentage = 0;
            CpuUsagePercentage = 0;

            DiskUsed = 0;
            DiskUsedPercentage = 0;

            FreeDisk = 0;
            FreeDiskPercentage = 0;
            
            await Task.CompletedTask;
            return;
        }

        await ListMemoryUsagePercentage(showSnackbar: false);
        await ListCpuUsagePercentage(showSnackbar: false);
        await ListDiskUsage(showSnackbar: false);
        await ListUptime(showSnackbar: false);
        await ListSshLogins(showSnackbar: false);
    }

    private async Task ListMemoryUsagePercentage(bool showSnackbar = true)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        MemoryUsagePercentage = UbuntuService.MemoryUsagePercentage();
        MemoryUsageData = [MemoryUsagePercentage, 100 - MemoryUsagePercentage];
        MemoryFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed memory usage!", Severity.Success);
        }

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ListCpuUsagePercentage(bool showSnackbar = true)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        CpuUsagePercentage = UbuntuService.CpuUsagePercentage();
        CpuUsageData = [CpuUsagePercentage, 100 - CpuUsagePercentage];
        CpuFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed CPU usage!", Severity.Success);
        }

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ListDiskUsage(bool showSnackbar = true)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        DiskUsed = UbuntuService.DiskUsage();
        DiskTotal = UbuntuService.DiskTotal();
        BuildDiskBreakdown();

        // Read the real size rather than assuming one. This used to divide by a hardcoded 25,
        // the droplet's disk; against Cortex's ~107 GiB root that reported several hundred
        // percent used and a negative amount free.
        DiskUsedPercentage = DiskTotal > 0 ? (DiskUsed / DiskTotal) * 100 : 0;

        FreeDisk = Math.Max(DiskTotal - DiskUsed, 0);
        FreeDiskPercentage = 100 - DiskUsedPercentage;

        DiskFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed Disk usage!", Severity.Success);
        }

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Feeds the Sankey chart from the filesystems Cortex actually has mounted.
    /// </summary>
    /// <remarks>
    /// Capped at the eight largest, and anything under a gigabyte is dropped: a chart with a
    /// dozen near-zero strands is harder to read than one showing where the space really went.
    /// </remarks>
    private void BuildDiskBreakdown()
    {
        var mounts = UbuntuService.DiskBreakdown()
            .Where(mount => mount.UsedGb >= 1)
            .Take(8)
            .ToList();

        var nodes = new List<SankeyChartNode> { new("Cortex", 0) };
        var edges = new List<SankeyChartEdge>();

        foreach (var mount in mounts)
        {
            nodes.Add(new SankeyChartNode(mount.Mount, 1));
            edges.Add(new SankeyChartEdge("Cortex", mount.Mount, mount.UsedGb));
        }

        Nodes = nodes;
        Edges = edges;
    }

    private async Task ListUptime(bool showSnackbar = true)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        Uptime = UbuntuService.Uptime().Replace("up", "");

        var rawTime = UbuntuService.BootTime().Replace("system boot", "");
        BootTime = Convert.ToDateTime(rawTime);

        UptimeFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed Uptime!", Severity.Success);
        }

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ListSshLogins(bool showSnackbar = true)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        SshLogins = UbuntuService.SshLogins();
        
        var weeklyLoginCount = new List<double>();
        foreach (var login in SshLogins)
        {
            weeklyLoginCount.Add(Convert.ToDouble(login.Count));
        }

        SshLoginsSeries =
        [
            new()
            {
                Name = "SSH Logins",
                Data = weeklyLoginCount.ToArray()
            }
        ];
        
        SshLoginsFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        if (showSnackbar)
        {
            CreateSnackbarMessage("Successfully listed SSH Logins!", Severity.Success);
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