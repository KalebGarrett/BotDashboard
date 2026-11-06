using System.Text;
using System.Text.Json;
using BotDashboard.App.Commands;
using BotDashboard.App.Credentials;
using BotDashboard.Models;
using Renci.SshNet;

namespace BotDashboard.App.Services;

/// <summary>
/// Runs Ollama management commands on the home server PC over SSH.
/// </summary>
/// <remarks>
/// Two connections, and they are deliberately different shapes. Server PC commands go through
/// the shared <see cref="ServerPcSshSession"/>, because a Windows SSH login is slow and the
/// panel has nine buttons. The Cortex call keeps the short-lived client
/// <see cref="UbuntuService"/> uses: a fresh client per call cannot leave a dead socket behind.
/// </remarks>
public class OllamaService
{
    private readonly ServerPcSshSession _serverPc;
    private readonly NasCredentials _nas;
    private readonly OllamaEndpoint _endpoint;

    public OllamaService(
        ServerPcSshSession serverPc,
        NasCredentials nas,
        OllamaEndpoint endpoint)
    {
        _serverPc = serverPc;
        _nas = nas;
        _endpoint = endpoint;
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
    /// Two machines, two connections. The service state comes from the server PC, which is
    /// where Ollama runs. The reachability half comes from Cortex, issued inside the container
    /// whose network namespace Jam Junction shares -- see <see cref="OllamaCommand.TailnetProbe"/>.
    ///
    /// Asking the server PC to call its own loopback would always succeed and prove nothing
    /// about the tailnet or its firewall rule.
    /// </remarks>
    public async Task<OllamaHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        var health = new OllamaHealth { Endpoint = _endpoint.BaseUrl };

        // Half one: the Windows service, over SSH to the server PC.
        try
        {
            health.ServiceState = _serverPc.IsConfigured
                ? RunCommandWithResponse(OllamaCommand.ServiceState()).Trim()
                : "unknown";
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

        // Half two: the HTTP API, asked from Jam Junction's own network namespace on Cortex.
        try
        {
            var output = await Task.Run(
                () => RunOnCortex(OllamaCommand.TailnetProbe(_endpoint.ProbeContainer, _endpoint.BaseUrl)),
                cancellationToken);

            var exitCode = -1;
            var body = new StringBuilder();

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("rc=", StringComparison.Ordinal))
                {
                    int.TryParse(trimmed[3..], out exitCode);
                }
                else if (trimmed.StartsWith("ms=", StringComparison.Ordinal))
                {
                    health.LatencyMs = long.TryParse(trimmed[3..], out var elapsed) ? elapsed : 0;
                }
                else
                {
                    body.Append(trimmed);
                }
            }

            if (exitCode != 0)
            {
                // The useful context is which container was asked and for what: a wrong
                // container name otherwise looks exactly like a dead tailnet.
                health.Error = $"No answer from {_endpoint.BaseUrl}/api/tags inside "
                               + $"{_endpoint.ProbeContainer} on Cortex (wget exit {exitCode}).";
                return health;
            }

            health.Reachable = true;

            using var document = JsonDocument.Parse(body.ToString());
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
            // Either Cortex is unreachable or the reply was not Ollama's JSON. Both are worth
            // seeing verbatim rather than flattened into "no route".
            health.Reachable = false;
            health.Error = exception.Message;
        }

        return health;
    }

    /// <summary>Runs a command on Cortex, where the bots stack and its tailnet sidecar live.</summary>
    private string RunOnCortex(string command)
    {
        if (!_nas.IsConfigured)
        {
            throw new InvalidOperationException(
                "Cortex SSH settings are missing, so the tailnet path to Ollama cannot be "
                + "checked. Set NAS_HOST, NAS_USERNAME and NAS_PASSWORD in .env.");
        }

        using var client = new SshClient(_nas.Host, _nas.Username, _nas.Password);
        client.Connect();

        var sshCommand = client.CreateCommand(command);
        sshCommand.CommandTimeout = TimeSpan.FromSeconds(30);
        var response = sshCommand.Execute();

        client.Disconnect();

        return response;
    }

    /// <summary>Runs a command on the server PC over the shared session.</summary>
    private string RunCommandWithResponse(string command)
    {
        // Fail with something the operator can act on. Without this the SSH client throws
        // deep inside Renci with an argument-null message that says nothing about the cause.
        if (!_serverPc.IsConfigured)
        {
            return "Server PC SSH settings are missing. Set SERVER_PC_HOST, SERVER_PC_USERNAME and "
                   + "SERVER_PC_PASSWORD in .env, then restart the app. See deploy/README.md.";
        }

        return _serverPc.Run(command);
    }
}
