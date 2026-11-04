using System.Diagnostics;
using System.Text.Json;
using BotDashboard.App.Commands;
using BotDashboard.App.Credentials;
using BotDashboard.Models;
using Renci.SshNet;

namespace BotDashboard.App.Services;

/// <summary>
/// Runs Ollama management commands on the droplet over SSH.
/// </summary>
/// <remarks>
/// Mirrors <see cref="UbuntuService"/>: one short-lived <see cref="SshClient"/> per call, which
/// keeps a dropped droplet connection from leaving the dashboard holding a dead socket.
/// </remarks>
public class OllamaService
{
    private readonly DigitalOceanDropletCredentials _digitalOcean;
    private readonly OllamaEndpoint _endpoint;
    private readonly IHttpClientFactory _httpClientFactory;

    public OllamaService(
        DigitalOceanDropletCredentials digitalOcean,
        OllamaEndpoint endpoint,
        IHttpClientFactory httpClientFactory)
    {
        _digitalOcean = digitalOcean;
        _endpoint = endpoint;
        _httpClientFactory = httpClientFactory;
    }

    private OllamaCommand OllamaCommand { get; } = new();

    public string Status() => RunCommandWithResponse(OllamaCommand.Status());

    public string Start() => RunCommandWithResponse(OllamaCommand.Start());

    public string Stop() => RunCommandWithResponse(OllamaCommand.Stop());

    public string Restart() => RunCommandWithResponse(OllamaCommand.Restart());

    public string ListModels() => RunCommandWithResponse(OllamaCommand.ListModels());

    public string RunningModels() => RunCommandWithResponse(OllamaCommand.RunningModels());

    public string PullModel(string model) => RunCommandWithResponse(OllamaCommand.PullModel(model));

    public string UnloadModel(string model) => RunCommandWithResponse(OllamaCommand.UnloadModel(model));

    public string ApiHealth() => RunCommandWithResponse(OllamaCommand.ApiHealth());

    public string Version() => RunCommandWithResponse(OllamaCommand.Version());

    public string MemoryUsage() => RunCommandWithResponse(OllamaCommand.MemoryUsage());

    /// <summary>
    /// Whether Ollama is up, and whether Cortex can actually reach it.
    /// </summary>
    /// <remarks>
    /// The HTTP half does not go through SSH. Bot Harbor shares the Tailscale sidecar's network
    /// namespace, so it calls the droplet's tailnet address directly -- the same path Jam
    /// Junction would take from Cortex. Asking the droplet to curl its own loopback would
    /// always succeed and would prove nothing about whether Cortex can get there.
    /// </remarks>
    public async Task<OllamaHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        var health = new OllamaHealth { Endpoint = _endpoint.BaseUrl };

        // Half one: the systemd unit, over SSH to the droplet.
        try
        {
            health.ServiceState = RunCommandWithResponse("systemctl is-active ollama").Trim();
        }
        catch (Exception exception)
        {
            health.ServiceState = "unknown";
            health.Error = $"Could not read the service state: {exception.Message}";
        }

        if (!_endpoint.IsConfigured)
        {
            health.Error = "Ollama__Host is not configured. Set OLLAMA_HOST in .env.";
            return health;
        }

        // Half two: the HTTP API, from here, across the tailnet.
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var client = _httpClientFactory.CreateClient("ollama");
            using var response = await client.GetAsync(
                $"{_endpoint.BaseUrl}/api/tags", cancellationToken);

            stopwatch.Stop();
            health.LatencyMs = stopwatch.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                health.Error = $"HTTP {(int)response.StatusCode} from {_endpoint.BaseUrl}/api/tags";
                return health;
            }

            health.Reachable = true;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("models", out var models))
            {
                foreach (var model in models.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var name))
                    {
                        health.Models.Add(name.GetString());
                    }
                }
            }
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            health.LatencyMs = stopwatch.ElapsedMilliseconds;
            // Nearly always a timeout, which here means the tailnet path is down rather than
            // that Ollama is broken -- worth distinguishing, so report the message verbatim.
            health.Error = exception.Message;
        }

        return health;
    }

    private string RunCommandWithResponse(string command)
    {
        // Fail with something the operator can act on. Without this the SSH client throws
        // deep inside Renci with an argument-null message that says nothing about the cause,
        // which is exactly what the blank DROPLET_ID produced on the Manage Droplet page.
        if (string.IsNullOrWhiteSpace(_digitalOcean.Host)
            || string.IsNullOrWhiteSpace(_digitalOcean.Username)
            || string.IsNullOrWhiteSpace(_digitalOcean.Password))
        {
            return "Droplet SSH settings are missing. Set DROPLET_HOST, DROPLET_USERNAME and "
                   + "DROPLET_PASSWORD in .env, then restart the app. See deploy/README.md.";
        }

        using var client = new SshClient(_digitalOcean.Host, _digitalOcean.Username, _digitalOcean.Password);
        client.Connect();

        // Pulling a model downloads gigabytes, so the default 30s command timeout is not enough.
        var sshCommand = client.CreateCommand(command);
        sshCommand.CommandTimeout = TimeSpan.FromMinutes(10);

        var response = sshCommand.Execute();
        var error = sshCommand.Error;

        client.Disconnect();

        // systemctl and ollama both report real failures on stderr with an empty stdout, so a
        // blank panel would otherwise be the only sign that anything went wrong.
        return string.IsNullOrWhiteSpace(error) ? response : $"{response}\n{error}".Trim();
    }
}
