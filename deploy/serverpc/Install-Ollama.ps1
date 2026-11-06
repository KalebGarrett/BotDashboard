#Requires -Version 5.1
<#
.SYNOPSIS
    Installs Ollama on the home server PC as an NSSM-wrapped Windows service.

.DESCRIPTION
    Does every step in deploy/README.md, "Ollama on the home server PC":

      1. Downloads and silently installs Ollama for the signed-in user.
      2. Stops the tray app and removes its login shortcut, so it never fights the
         service for port 11434.
      3. Registers an `Ollama` service with NSSM (the same way `LavalinkNode` is set up),
         listening on 0.0.0.0 so Cortex can reach it over the tailnet.
      4. Allows 11434 in Windows Firewall from Tailscale addresses (100.64.0.0/10) only,
         and removes any broader rule Windows made for ollama.exe.
      5. Waits for the API, then pulls the model Jam Junction uses.

    Safe to run again: a step that is already done is skipped or brought back in line.

    Run it as the `Server` account, from an ELEVATED PowerShell:

        powershell -ExecutionPolicy Bypass -File "C:\Users\Server\Documents\Ollama\Install-Ollama.ps1"

.PARAMETER Model
    The model to pull. Defaults to llama3.2:1b, which is what Jam Junction's OLLAMA_MODEL
    is set to in deploy/bots/.env.
#>
param(
    [string] $Model = 'llama3.2:1b',
    [string] $Nssm = (Join-Path $env:USERPROFILE 'Documents\nssm-2.24\win64\nssm.exe')
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # Invoke-WebRequest is many times slower with the progress bar

$ServiceName = 'Ollama'
$OllamaDir   = Join-Path $env:LOCALAPPDATA 'Programs\Ollama'
$OllamaExe   = Join-Path $OllamaDir 'ollama.exe'
$ModelsDir   = Join-Path $env:USERPROFILE '.ollama\models'
$LogDir      = Join-Path $env:USERPROFILE 'Documents\Ollama'
$LogFile     = Join-Path $LogDir 'ollama.log'
$RuleName    = 'Ollama (tailnet only)'

function Step($text) { Write-Host "`n==> $text" -ForegroundColor Cyan }

# --- Preconditions ------------------------------------------------------------------------

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this from an elevated PowerShell (right-click > Run as administrator).'
}

if (-not (Test-Path $Nssm)) {
    throw "NSSM not found at $Nssm. Pass -Nssm with the path to nssm.exe."
}

New-Item -ItemType Directory -Force -Path $LogDir, $ModelsDir | Out-Null

# --- 1. Install ---------------------------------------------------------------------------

Step 'Installing Ollama'
if (Test-Path $OllamaExe) {
    Write-Host "Already installed: $(& $OllamaExe --version 2>&1 | Select-Object -Last 1)"
}
else {
    $setup = Join-Path $env:TEMP 'OllamaSetup.exe'
    Write-Host 'Downloading OllamaSetup.exe from ollama.com...'
    Invoke-WebRequest -UseBasicParsing -Uri 'https://ollama.com/download/OllamaSetup.exe' -OutFile $setup
    Write-Host 'Running the installer silently...'
    Start-Process -FilePath $setup -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' -Wait
    Remove-Item $setup -ErrorAction SilentlyContinue

    if (-not (Test-Path $OllamaExe)) {
        throw "The installer finished but $OllamaExe does not exist."
    }
    Write-Host 'Installed.'
}

# --- 2. Tray app out of the way -----------------------------------------------------------

Step 'Stopping the tray app and its login shortcut'
# The installer launches the tray app, which starts its own `ollama serve` on 11434.
# Only kill processes that are not the service's own.
$servicePid = (Get-CimInstance Win32_Service -Filter "Name='$ServiceName'" -ErrorAction SilentlyContinue).ProcessId
Get-Process 'ollama app', 'ollama' -ErrorAction SilentlyContinue |
    Where-Object { $_.Id -ne $servicePid -and (Get-CimInstance Win32_Process -Filter "ProcessId=$($_.Id)").ParentProcessId -ne $servicePid } |
    Stop-Process -Force -ErrorAction SilentlyContinue

$startup = Join-Path ([Environment]::GetFolderPath('Startup')) 'Ollama.lnk'
if (Test-Path $startup) { Remove-Item $startup; Write-Host "Removed $startup" } else { Write-Host 'No login shortcut.' }

# --- 3. Service ---------------------------------------------------------------------------

Step "Registering the $ServiceName service with NSSM"
if (-not (Get-Service $ServiceName -ErrorAction SilentlyContinue)) {
    & $Nssm install $ServiceName $OllamaExe serve | Out-Null
    Write-Host 'Service created.'
}
else {
    & $Nssm stop $ServiceName 2>&1 | Out-Null
    & $Nssm set $ServiceName Application $OllamaExe | Out-Null
    & $Nssm set $ServiceName AppParameters serve | Out-Null
    Write-Host 'Service already existed; settings refreshed.'
}

& $Nssm set $ServiceName DisplayName 'Ollama (Jam Junction)' | Out-Null
& $Nssm set $ServiceName Description 'Ollama for Jam Junction''s song recommender. Managed from Bot Harbor.' | Out-Null
& $Nssm set $ServiceName AppDirectory $OllamaDir | Out-Null
& $Nssm set $ServiceName AppEnvironmentExtra "OLLAMA_HOST=0.0.0.0" "OLLAMA_MODELS=$ModelsDir" | Out-Null
& $Nssm set $ServiceName AppStdout $LogFile | Out-Null
& $Nssm set $ServiceName AppStderr $LogFile | Out-Null
& $Nssm set $ServiceName AppRotateFiles 1 | Out-Null
& $Nssm set $ServiceName AppRotateBytes 10485760 | Out-Null
& $Nssm set $ServiceName Start SERVICE_AUTO_START | Out-Null

& $Nssm start $ServiceName | Out-Null
Write-Host "Service state: $((Get-Service $ServiceName).Status)"

# --- 4. Firewall --------------------------------------------------------------------------

Step 'Limiting port 11434 to the tailnet'
# Windows may have created an allow-everything rule for ollama.exe. Drop it: Ollama has no
# authentication, so the only thing keeping it private is the rule below.
Get-NetFirewallApplicationFilter -ErrorAction SilentlyContinue |
    Where-Object { $_.Program -like '*\Ollama\ollama*.exe' } |
    Get-NetFirewallRule -ErrorAction SilentlyContinue |
    ForEach-Object { Write-Host "Removing broader rule: $($_.DisplayName)"; $_ | Remove-NetFirewallRule }

Get-NetFirewallRule -DisplayName $RuleName -ErrorAction SilentlyContinue | Remove-NetFirewallRule
New-NetFirewallRule -DisplayName $RuleName -Direction Inbound -Protocol TCP -LocalPort 11434 `
    -RemoteAddress 100.64.0.0/10 -Action Allow -Profile Any | Out-Null
Write-Host "Rule '$RuleName' allows 11434 from 100.64.0.0/10 only."

# --- 5. API and model ---------------------------------------------------------------------

Step 'Waiting for the API'
$up = $false
foreach ($i in 1..30) {
    try { Invoke-WebRequest -UseBasicParsing -TimeoutSec 3 http://127.0.0.1:11434/api/tags | Out-Null; $up = $true; break }
    catch { Start-Sleep -Seconds 1 }
}
if (-not $up) { throw "Ollama did not answer on 11434 within 30s. Check $LogFile." }
Write-Host 'API is answering.'

Step "Pulling $Model (this can take a few minutes)"
& $OllamaExe pull $Model
if ($LASTEXITCODE -ne 0) { throw "ollama pull $Model failed." }

$listening = Get-NetTCPConnection -LocalPort 11434 -State Listen -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty LocalAddress -Unique
$tailnet = (Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -like '100.*' }).IPAddress

Step 'Done'
& $OllamaExe list
Write-Host ''
Write-Host "Listening on: $($listening -join ', ')   (want 0.0.0.0)"
Write-Host "Tailnet IP:   $tailnet   (use this as OLLAMA_HOST)"
Write-Host "Log file:     $LogFile"
