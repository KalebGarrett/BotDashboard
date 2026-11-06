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
    [Inject] public OllamaService OllamaService { get; set; }
    [Inject] private NotificationService Notifications { get; set; }

    private List<DockerImage> DockerImages { get; set; } = new();
    private string ImageFetchTime { get; set; }

    private string[] Headings { get; set; } = ["Repository", "Tag", "Image Id", "Created", "Size", "Actions"];

    private List<Container> Containers { get; set; } = new();
    private string ContainerFetchTime { get; set; }

    private string Output { get; set; }
    private string OutputFetchTime { get; set; }
    
    private OllamaHealth OllamaHealth { get; set; }
    private string OllamaHealthFetchTime { get; set; }

    private bool Locked { get; set; }

    /// <summary>
    /// The launch sections, matching the dashboard's so the two cards read the same way.
    /// </summary>
    private static readonly (string Heading, ContainerGroup Group, Color Colour)[] LaunchGroups =
    [
        ("Launch a bot", ContainerGroup.Bot, Color.Tertiary),
        ("Music and audio", ContainerGroup.Audio, Color.Tertiary),
        ("Infrastructure", ContainerGroup.Infrastructure, Color.Primary)
    ];

    protected override async Task OnInitializedAsync()
    {
        if (Switch.OnInitialized == false)
        {
            await Task.CompletedTask;
            return;
        }

        await ListContainers(isInternalCall: true);
        await ListImages(isInternalCall: true);
        await CheckOllamaHealth(showSnackbar: false);
    }

    /// <summary>
    /// Refreshes the Ollama panel: the service state over SSH, and whether Cortex can reach
    /// the HTTP API across the tailnet.
    /// </summary>
    private async Task CheckOllamaHealth(bool showSnackbar = true)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        try
        {
            OllamaHealth = await OllamaService.HealthAsync();
            OllamaHealthFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

            FileOllamaState();

            if (showSnackbar)
            {
                CreateSnackbarMessage(
                    OllamaHealth.Healthy ? "Ollama is reachable!" : "Ollama is not fully healthy.",
                    OllamaHealth.Healthy ? Severity.Success : Severity.Warning);
            }
        }
        catch (Exception exception)
        {
            OllamaHealth = new OllamaHealth { Error = exception.Message };
            OllamaHealthFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);

            FileOllamaState();

            if (showSnackbar)
            {
                CreateSnackbarMessage("Could not check Ollama.", Severity.Error);
            }
        }

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Turns the Ollama panel's two answers into bell rows, and withdraws them when they clear.
    /// </summary>
    /// <remarks>
    /// Jam Junction's song recommender is the only consumer, and it fails quietly: the bot
    /// stays online and simply stops recommending. Without a row here, nothing says so.
    /// </remarks>
    private void FileOllamaState()
    {
        var conditions = new List<(string, string, string, NotificationLevel)>();

        if (OllamaHealth is not null && !OllamaHealth.ServiceActive)
        {
            conditions.Add(("service", "Ollama service is not active",
                $"The Ollama service reports \"{OllamaHealth.ServiceState}\" on the server PC.",
                NotificationLevel.Error));
        }

        if (OllamaHealth is not null && !OllamaHealth.Reachable)
        {
            conditions.Add(("reachable", "Ollama unreachable from Cortex",
                string.IsNullOrWhiteSpace(OllamaHealth.Error)
                    ? "The tailnet path from the bots sidecar is not answering."
                    : OllamaHealth.Error,
                NotificationLevel.Warning));
        }

        Notifications.ReconcileState("ollama:", conditions);
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

    // ---------------------------------------------------------------------------------------
    // Ollama
    //
    // Every one of these is a read or a service action on the server PC, and all of them write
    // their result into the same Logs panel the Docker commands use, so the output has one
    // place to land rather than a second panel to look at.
    // ---------------------------------------------------------------------------------------

    private async Task RunOllamaCommand(Func<string> command, string successMessage)
    {
        Locked = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        try
        {
            Output = command();
            OutputFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
            CreateSnackbarMessage(successMessage, Severity.Success);
        }
        catch (Exception exception)
        {
            // An unreachable server PC or a refused SSH login throws out of SshClient. Without
            // this the whole Blazor circuit tears down and the page dies with the generic
            // "Oops... something went wrong", which says nothing about what actually failed.
            Output = exception.Message;
            OutputFetchTime = DateTime.UtcNow.ToLocalTime().ToString(CultureInfo.CurrentCulture);
            CreateSnackbarMessage("Ollama command failed. See the log panel.", Severity.Error);
        }

        Locked = false;
        await InvokeAsync(StateHasChanged);
    }

    private Task OllamaStatus() =>
        RunOllamaCommand(() => OllamaService.Status(), "Fetched Ollama status!");

    private Task OllamaApiHealth() =>
        RunOllamaCommand(() => OllamaService.ApiHealth(), "Checked the Ollama API!");

    private Task OllamaMemoryUsage() =>
        RunOllamaCommand(() => OllamaService.MemoryUsage(), "Fetched server PC memory!");

    private Task OllamaListModels() =>
        RunOllamaCommand(() => OllamaService.ListModels(), "Listed Ollama models!");

    private Task OllamaRunningModels() =>
        RunOllamaCommand(() => OllamaService.RunningModels(), "Listed loaded models!");

    private Task OllamaPullModel(string model) =>
        RunOllamaCommand(() => OllamaService.PullModel(model), $"Pulled {model}!");

    private Task OllamaStart() =>
        RunOllamaCommand(() => OllamaService.Start(), "Started Ollama!");

    private Task OllamaRestart() =>
        RunOllamaCommand(() => OllamaService.Restart(), "Restarted Ollama!");

    private Task OllamaStop() =>
        RunOllamaCommand(() => OllamaService.Stop(), "Stopped Ollama!");

    private void CreateSnackbarMessage(string message, Severity severity)
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        Snackbar.Add(message, severity);
    }
}