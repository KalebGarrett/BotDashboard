using BotDashboard.App.Commands;
using BotDashboard.App.Credentials;
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

    public OllamaService(DigitalOceanDropletCredentials digitalOcean)
    {
        _digitalOcean = digitalOcean;
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
