using System.Text;

namespace BotDashboard.App.Commands;

/// <summary>
/// Shell commands for the Ollama server that backs Jam Junction's AI song recommender.
/// </summary>
/// <remarks>
/// Ollama runs on the home server PC, a Windows machine, as an NSSM-wrapped Windows service
/// named <c>Ollama</c> -- the same arrangement as the <c>LavalinkNode</c> service beside it, so
/// it starts at boot without anyone signing in. The service sets <c>OLLAMA_HOST=0.0.0.0</c>
/// so Jam Junction can reach it from Cortex over the tailnet, and a Windows Firewall rule
/// limits 11434 to tailnet addresses: Ollama has no authentication of its own. Setup is in
/// deploy/README.md.
///
/// Everything except <see cref="TailnetProbe"/> runs on the server PC over SSH. Windows'
/// OpenSSH hands the command to <c>cmd.exe</c> by default, whose quoting rules would mangle
/// anything non-trivial, so each one is a PowerShell script sent as
/// <c>-EncodedCommand</c>: base64 has no characters either shell treats specially, and it
/// behaves the same if the default shell is ever switched to PowerShell.
/// </remarks>
public class OllamaCommand
{
    /// <summary>The Windows service name NSSM registers.</summary>
    public const string ServiceName = "Ollama";

    /// <summary>
    /// Finds ollama.exe. The Windows installer puts it under the installing user's
    /// <c>%LOCALAPPDATA%</c> and on that user's PATH; looking in both survives either being
    /// missing from an SSH login's environment.
    /// </summary>
    private const string FindOllama =
        "$ollama = (Get-Command ollama -ErrorAction SilentlyContinue).Source; "
        + "if (-not $ollama) { $ollama = Join-Path $env:LOCALAPPDATA 'Programs\\Ollama\\ollama.exe' }; ";

    /// <summary>
    /// Stops a model command when the service is down, rather than letting the CLI fix it.
    /// </summary>
    /// <remarks>
    /// On Windows, <c>ollama list</c> with no server running does not fail: it launches a
    /// server of its own as the SSH user and waits on it. That never returns over SSH, holds
    /// the shared session for the full command timeout, and leaves a stray server on 11434
    /// that the real service then cannot bind. Checked first so a stopped service reads as
    /// stopped.
    /// </remarks>
    private const string RequireRunning =
        "if ((Get-Service " + ServiceName + " -ErrorAction SilentlyContinue).Status -ne 'Running') { "
        + "'The " + ServiceName + " service is not running. Press Start first.'; exit 1 }; ";

    /// <summary>
    /// Service state as a single word: <c>Running</c>, <c>Stopped</c>, and so on, or
    /// <c>NotInstalled</c> when the service does not exist yet.
    /// </summary>
    public string ServiceState()
    {
        return PowerShell(
            $"try {{ (Get-Service {ServiceName} -ErrorAction Stop).Status.ToString() }} "
            + "catch { 'NotInstalled' }");
    }

    /// <summary>Whether the service is running, plus the processes behind it.</summary>
    public string Status()
    {
        return PowerShell(
            $"Get-Service {ServiceName} -ErrorAction Stop | Format-List Name, DisplayName, Status, StartType | Out-String; "
            + "Get-Process ollama*, 'ollama app' -ErrorAction SilentlyContinue | "
            + "Format-Table Id, ProcessName, @{n='MemoryMB';e={[int]($_.WorkingSet64/1MB)}}, StartTime -AutoSize | Out-String");
    }

    public string Start()
    {
        return PowerShell($"Start-Service {ServiceName} -ErrorAction Stop; (Get-Service {ServiceName}).Status.ToString()");
    }

    public string Stop()
    {
        return PowerShell($"Stop-Service {ServiceName} -ErrorAction Stop; (Get-Service {ServiceName}).Status.ToString()");
    }

    public string Restart()
    {
        return PowerShell($"Restart-Service {ServiceName} -ErrorAction Stop; (Get-Service {ServiceName}).Status.ToString()");
    }

    /// <summary>Models pulled onto the server PC's disk.</summary>
    public string ListModels()
    {
        return PowerShell(RequireRunning + FindOllama + "& $ollama list");
    }

    /// <summary>Models currently loaded in memory, with their VRAM/RAM footprint and expiry.</summary>
    public string RunningModels()
    {
        return PowerShell(RequireRunning + FindOllama + "& $ollama ps");
    }

    /// <summary>Pulls a model.</summary>
    public string PullModel(string model)
    {
        return PowerShell(RequireRunning + FindOllama + $"& $ollama pull {Quote(model)}");
    }

    /// <summary>Frees the memory a loaded model is holding without stopping the service.</summary>
    public string UnloadModel(string model)
    {
        return PowerShell(RequireRunning + FindOllama + $"& $ollama stop {Quote(model)}");
    }

    /// <summary>
    /// Asks the HTTP API directly, which is what Jam Junction actually depends on. A running
    /// service that is not answering on 11434 looks identical to a broken one from the bot's
    /// point of view, so this is the check that matters.
    /// </summary>
    public string ApiHealth()
    {
        return PowerShell(
            "try { $r = Invoke-WebRequest -UseBasicParsing -TimeoutSec 10 http://127.0.0.1:11434/api/tags; "
            + "\"http_status=$($r.StatusCode)\" } "
            + "catch { if ($_.Exception.Response) { \"http_status=$([int]$_.Exception.Response.StatusCode)\" } "
            + "else { 'http_status=unreachable' } }");
    }

    /// <summary>
    /// Asks Ollama's HTTP API from inside the container that owns the tailnet route.
    /// </summary>
    /// <remarks>
    /// This one runs on Cortex, not on the server PC, and is plain sh. Jam Junction has no
    /// network stack of its own -- it runs with <c>network_mode: service:bots-tailscale</c> --
    /// so the only honest test of "can the bot reach Ollama" is one issued from inside that
    /// sidecar. Asking the server PC to call its own loopback would always succeed and prove
    /// nothing about the tailnet or the firewall rule.
    ///
    /// busybox wget rather than curl: the Tailscale image carries no curl. The timing is taken
    /// around the whole <c>docker exec</c>, so it includes a few milliseconds of exec overhead
    /// -- it is a health signal, not a benchmark.
    /// </remarks>
    public string TailnetProbe(string container, string baseUrl)
    {
        return "start=$(date +%s%3N); "
               + "body=$(docker exec " + container + " wget -q -T 8 -O - "
               + baseUrl + "/api/tags 2>/dev/null); "
               + "rc=$?; "
               + "end=$(date +%s%3N); "
               + "echo \"rc=$rc\"; "
               + "echo \"ms=$((end-start))\"; "
               + "echo \"$body\"";
    }

    public string Version()
    {
        return PowerShell(FindOllama + "& $ollama --version");
    }

    /// <summary>Memory headroom, since an oversized model is the usual failure.</summary>
    public string MemoryUsage()
    {
        return PowerShell(
            "$os = Get-CimInstance Win32_OperatingSystem; "
            + "$total = $os.TotalVisibleMemorySize / 1MB; $free = $os.FreePhysicalMemory / 1MB; "
            + "'Total: {0,6:N1} GB' -f $total; "
            + "'Used:  {0,6:N1} GB' -f ($total - $free); "
            + "'Free:  {0,6:N1} GB' -f $free; ''; "
            + "Get-Process ollama* -ErrorAction SilentlyContinue | "
            + "Format-Table ProcessName, Id, @{n='MemoryMB';e={[int]($_.WorkingSet64/1MB)}} -AutoSize | Out-String");
    }

    /// <summary>
    /// Wraps a script so it runs identically whether sshd's default shell is cmd or PowerShell.
    /// </summary>
    /// <remarks>
    /// Progress output is silenced first: PowerShell started with redirected streams
    /// serialises progress records onto stderr as CLIXML, which would otherwise fill the log
    /// panel with XML on every call.
    /// </remarks>
    private static string PowerShell(string script)
    {
        var full = "$ProgressPreference = 'SilentlyContinue'; " + script;
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(full));

        return $"powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}";
    }

    /// <summary>A PowerShell single-quoted literal, so a model name is never parsed as code.</summary>
    private static string Quote(string value) => "'" + value.Replace("'", "''") + "'";
}
