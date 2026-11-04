namespace BotDashboard.App.Commands;

/// <summary>
/// Shell commands for the Ollama server that backs Jam Junction's AI song recommender.
/// </summary>
/// <remarks>
/// Ollama runs on the DigitalOcean droplet as a systemd unit named <c>ollama</c>, listening on
/// the droplet's loopback at <c>127.0.0.1:11434</c>. It is deliberately not exposed off the box:
/// Ollama has no authentication of its own, so the only reason this is safe is that nothing
/// outside the droplet can reach the port. Jam Junction gets to it by running with
/// <c>--network host</c> on the same droplet.
///
/// These run over SSH as the droplet's login user, which is root, so no <c>sudo</c> is needed.
/// If that login ever changes to a non-root user, every <c>systemctl</c> call here needs a
/// <c>sudo</c> prefix and a NOPASSWD rule.
/// </remarks>
public class OllamaCommand
{
    /// <summary>Whether the service is running, plus the tail of its unit status.</summary>
    public string Status()
    {
        // --no-pager stops systemctl piping into less, which would hang a non-interactive SSH
        // session waiting for a keypress that never comes.
        return "systemctl is-active ollama; echo '---'; systemctl status ollama --no-pager --lines=10";
    }

    public string Start()
    {
        return "systemctl start ollama && systemctl is-active ollama";
    }

    public string Stop()
    {
        return "systemctl stop ollama && systemctl is-active ollama || echo inactive";
    }

    public string Restart()
    {
        return "systemctl restart ollama && systemctl is-active ollama";
    }

    /// <summary>Models pulled onto the droplet's disk.</summary>
    public string ListModels()
    {
        return "ollama list";
    }

    /// <summary>Models currently loaded in memory, with their VRAM/RAM footprint and expiry.</summary>
    public string RunningModels()
    {
        return "ollama ps";
    }

    /// <summary>
    /// Pulls a model. The droplet is memory-constrained, which is why Jam Junction runs
    /// <c>llama3.2:1b</c> rather than the image's baked-in 3B default.
    /// </summary>
    public string PullModel(string model)
    {
        return $"ollama pull {model}";
    }

    /// <summary>Frees the memory a loaded model is holding without stopping the service.</summary>
    public string UnloadModel(string model)
    {
        return $"ollama stop {model}";
    }

    /// <summary>
    /// Asks the HTTP API directly, which is what Jam Junction actually depends on. A healthy
    /// systemd unit that is not answering on 11434 looks identical to a broken one from the
    /// bot's point of view, so this is the check that matters.
    /// </summary>
    public string ApiHealth()
    {
        return "curl -s -o /dev/null -w 'http_status=%{http_code}\n' --max-time 10 "
               + "http://127.0.0.1:11434/api/tags || echo 'http_status=unreachable'";
    }

    public string Version()
    {
        return "ollama --version";
    }

    /// <summary>Memory headroom, since an oversized model is the usual failure on a small droplet.</summary>
    public string MemoryUsage()
    {
        return "free -h";
    }
}
