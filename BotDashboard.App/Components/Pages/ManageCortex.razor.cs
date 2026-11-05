using BotDashboard.App.Services;
using BotDashboard.Models;
using Microsoft.AspNetCore.Components;

namespace BotDashboard.App.Components.Pages;

/// <summary>
/// Cortex's own page: what the NAS is, whether the dashboard can reach it, and the setting
/// that decides whether the other pages talk to it on open.
/// </summary>
/// <remarks>
/// Auto Load Data used to sit on Manage Droplet, which was the wrong home for it. The droplet
/// does nothing now but host Ollama, while the setting governs SSH round trips to Cortex --
/// so it belongs with the machine it costs time on.
/// </remarks>
public partial class ManageCortex
{
    [Inject] private UbuntuService UbuntuService { get; set; }
    [Inject] private PreferencesService Preferences { get; set; }

    private CortexStatus Status { get; set; } = new();

    /// <summary>Whether a fetch has been made at all, so "unreachable" is not claimed before one.</summary>
    private bool Loaded { get; set; }

    private bool Reachable { get; set; }

    private bool Locked { get; set; }

    private string ErrorMessage { get; set; }

    private bool AutoLoadData { get; set; }

    private IEnumerable<KeyValuePair<string, string>> Rows =>
    [
        new("Address", Or(Status.Host)),
        new("Hostname", Or(Status.Hostname)),
        new("SSH user", Or(Status.Username)),
        new("Kernel", Or(Status.Kernel)),
        new("Uptime", Or(Status.Uptime)),
        new("Storage pool", Or(Status.DiskPath)),
        new("Docker engine", Or(Status.DockerVersion)),
        new("Containers", Loaded && Reachable
            ? $"{Status.RunningContainers} running of {Status.TotalContainers}"
            : "-")
    ];

    protected override async Task OnInitializedAsync()
    {
        AutoLoadData = Preferences.AutoLoadData;

        // Honours the very setting this page owns: the fetch below is six commands over SSH.
        if (Switch.OnInitialized)
        {
            await GetStatus();
        }
        else
        {
            // Configuration alone, with no connection made, so the page still says what it
            // would be connecting to.
            Status = UbuntuService.ConfiguredTarget();
        }
    }

    private async Task GetStatus()
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        try
        {
            // Off the renderer's thread: SSH.NET is synchronous, and a handshake that stalls
            // would otherwise hold the circuit and freeze the whole page.
            Status = await Task.Run(() => UbuntuService.Status());
            Reachable = true;
            ErrorMessage = null;
        }
        catch (Exception exception)
        {
            // Keep the circuit alive so the page can say what is wrong. An exception escaping
            // here reaches the renderer and the user only sees "Oops... something went wrong".
            Status = UbuntuService.ConfiguredTarget();
            Reachable = false;
            ErrorMessage = exception.Message;
        }

        Loaded = true;
        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private void OnAutoLoadChanged()
    {
        Preferences.SetAutoLoadData(AutoLoadData);
        Switch.OnInitialized = AutoLoadData;
    }

    private static string Or(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
}
