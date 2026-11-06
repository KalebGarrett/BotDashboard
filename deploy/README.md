# Bot Harbor on Cortex

Bot Harbor used to run as an Azure App Service with its database in Azure SQL. It now runs
entirely on **Cortex**, the UGREEN DXP4800 Plus, and is reached over the existing Tailscale
tailnet. Nothing is billed, nothing is exposed to the internet, and there is no port forward.

```text
Browser (any device on the tailnet)
    │  HTTPS 443, real Let's Encrypt cert
    ▼
bot-harbor.<tailnet>.ts.net          ← tailscale sidecar, `tailscale serve`
    │  HTTP 127.0.0.1:8080 (shared network namespace)
    ▼
botharbor-app                        ← Blazor Server dashboard
    │  TCP 5432 on the compose network
    ▼
botharbor-postgres                   ← PostgreSQL 16

botharbor-app ──SSH──▶ home server PC (Windows): the Ollama service
```

The DigitalOcean droplet is retired. The bots run on Cortex (see `deploy/bots`), and Ollama runs
on the home server PC beside Lavalink.

## Why these choices

| Decision | Reason |
| --- | --- |
| PostgreSQL in a container | Replaces Azure SQL with no licence and a ~30 MB footprint. Proper concurrent access, unlike a SQLite file on a network share. |
| Tailscale sidecar, not Funnel | Funnel would publish the dashboard to the whole internet. A private tailnet node keeps it to your devices, the same call made for the Lavalink link. |
| Not Cloudflare Tunnel | Needs a paid domain and is built for public web traffic. |
| App shares the sidecar's netns | `tailscale serve` can proxy to `127.0.0.1:8080`, and the app has no address off the tailnet at all. |
| Named volumes, not SMB paths | Postgres on an SMB mount corrupts under lock contention. These volumes are local btrfs on Volume 1. |

## First run

Do this **on Cortex**, over SSH — not from the `Cortex` network drive on your PC. Docker has to
see a local path, and Postgres must never sit on an SMB mount.

### 1. Enable Docker and SSH on the NAS

In UGOS: **App Center → Docker** (install), and **Control Panel → Terminal → Enable SSH**.

### 2. Clone the repo onto Volume 1

```bash
ssh kaleb@cortex
sudo mkdir -p /volume1/docker && cd /volume1/docker
git clone https://github.com/<you>/BotDashboard.git botharbor
cd botharbor
```

### 3. Mint a Tailscale auth key

At <https://login.tailscale.com/admin/settings/keys> create a key that is **Reusable**, with
**Ephemeral off** (the node must survive restarts), and tagged `tag:container`.

### 3b. Give the Cortex account docker and journal access

Bot Harbor reads containers, images and the whole Stats page from Cortex over SSH. Two group
memberships make that work without a password prompt SSH.NET has no way to answer:

```bash
sudo usermod -aG docker,systemd-journal $USER
```

Then **log out and back in**, or the new groups will not apply to your session.

- `docker` lets `docker ps` run without `sudo`.
- `systemd-journal` lets `journalctl -u ssh` read the SSH logins the Stats chart plots.

Being in the `docker` group is equivalent to root on this machine. That is not a new privilege
if the account already has full `sudo`, but it is worth knowing before you grant it. Without
it, every container panel reports a permission error from the Docker socket.

### 4. Fill in the environment

Use the script rather than an editor:

```bash
./deploy/configure.sh
```

It prompts for each secret with the terminal echo off, so nothing appears on screen or in your
shell history, validates what you type, and is safe to re-run — values already set are left
alone unless you ask to replace them.

**It also quotes the values correctly, which matters more than it sounds.** Compose
interpolates `.env`, so a bare `p$ssword` loses everything from the `$` onward and you get an
authentication failure with nothing in the logs to explain it. Only single-quoted values
survive. If you do edit `.env` by hand, single-quote anything containing a `$`.

`POSTGRES_PASSWORD` is mandatory — compose refuses to start the database without it. Set
`BOTHARBOR_ADMIN_EMAIL` and `BOTHARBOR_ADMIN_PASSWORD` too: there is no sign-up page, so this
is the only way into a fresh database. `TS_AUTHKEY` and `SERVER_PC_*` are not guarded by
compose (see the note at the top of `docker-compose.yml`); without them the tunnel never comes
up and the app logs a warning naming what is missing.

### 5. Build and start

```bash
docker compose build
docker compose up -d
docker compose logs -f botharbor
```

Wait for `Database schema is up to date.` and `Created bootstrap user`.

### 6. Approve the node and check the hostname

```bash
docker compose exec tailscale tailscale status
docker compose exec tailscale tailscale cert --help  # confirms the tailnet cert domain
```

The dashboard is now at **`https://bot-harbor.<your-tailnet>.ts.net`** from any device signed
in to the tailnet. The first load can take a few seconds while the TLS certificate is issued.

### 7. Lock the bootstrap password away

Log in, go to **Settings → Manage Password**, change it. Then:

```bash
sed -i 's/^BOTHARBOR_ADMIN_PASSWORD=.*/BOTHARBOR_ADMIN_PASSWORD=/' .env
docker compose up -d
```

The seeder is a no-op once the account exists, so this only removes the plaintext password from
the environment.

### 8. Disable key expiry

At <https://login.tailscale.com/admin/machines>, on `bot-harbor`: **… → Disable key expiry**.
Without this the tunnel dies after 180 days. Same step you already did for `server-pc`.

## Decommissioning Azure

Only after you have logged in to the NAS instance:

1. Azure portal → delete the **App Service** and its **App Service Plan**.
2. Delete the **SQL Database**, then the **SQL Server** that hosted it.
3. Check **Cost Management → Cost analysis** next billing cycle for anything left running.
4. **Revoke the DigitalOcean API token** in the DigitalOcean console. It was set as an Azure app
   setting, and nothing uses it now the droplet is gone.

No data is carried over — the Postgres database starts empty, which is what you asked for. The
only thing in the old database was the Identity user table.

## Day to day

```bash
docker compose ps                        # what's up
docker compose logs -f botharbor         # app logs
docker compose restart botharbor         # bounce the app
docker compose pull && docker compose up -d   # update
docker compose exec tailscale tailscale status # tunnel health
curl -s localhost:8080/healthz           # from inside the tailscale netns
```

### Updating after a code change

Built on the NAS. The stack lives in `/volume1/appdata/botharbor`, and UGOS ships no `git`
binary, so the pull runs in a container against the same checkout:

```bash
cd /volume1/appdata/botharbor
docker run --rm -v /volume1/appdata/botharbor:/git -w /git alpine/git -c safe.directory=/git pull --ff-only
docker compose build botharbor && docker compose up -d botharbor
docker compose --profile cloudflare up -d --force-recreate cloudflared
```

That last line is not optional. `cloudflared` runs with `network_mode: service:botharbor`, so
it lives inside the app container's network namespace -- recreating the app pulls the namespace
out from under the tunnel and every request starts returning a Cloudflare 502 even though the
app itself is healthy. Recreating `cloudflared` afterwards puts it back in the new namespace.

Or build on your PC and push, matching the Jam Junction workflow:

```bash
docker build -t kalebg08/botharbor:latest .
docker push kalebg08/botharbor:latest
# on Cortex
docker compose pull botharbor && docker compose up -d botharbor
```

### Backups

`deploy/backup.sh` writes a compressed `pg_dump` and prunes anything older than 30 days.

```bash
./deploy/backup.sh /volume1/backups/botharbor
```

Worth adding to the UGOS task scheduler nightly. The database is small; the real reason to keep
backups is the Identity user table.

Restore:

```bash
gunzip -c botharbor-20261104.sql.gz | \
  docker compose exec -T postgres psql -U botharbor -d botharbor
```

## Two ways in

| Route | Reachable from | Protected by |
| --- | --- | --- |
| `bot-harbor.<tailnet>.ts.net` | tailnet devices only | the tailnet itself, plus Bot Harbor's login |
| `botharbor.horizonstack.app` | any browser, anywhere | **Cloudflare Access**, then Bot Harbor's login |

Both run at once and neither depends on the other. `tailscale serve` and `cloudflared` both
proxy to `127.0.0.1:8080` inside the same network namespace, which is why the `cloudflared`
service sets `network_mode: service:tailscale` -- without that, `localhost` would be the
cloudflared container itself and the tunnel would answer 502.

Start the public route with its profile; it does not come up by default:

```bash
docker compose --profile cloudflare up -d cloudflared
```

**Cloudflare Access is doing the real work on the public route.** An unauthenticated request
gets a 302 to `horizonstack.cloudflareaccess.com` with an empty body -- it never reaches
Cortex at all. That matters because `Login.razor` calls `PasswordSignInAsync` with
`lockoutOnFailure: false`, so the application itself will accept unlimited password guesses.
Exposing it publicly without Access in front would put that on the open internet.

The Access policy is `Bot Harbor owner only`, matching a single email address. It is
deliberately separate from the ORION policy on the same account, so editing one cannot
silently change the other.

### A trap when adding the hostname

Cloudflare's current Routes UI creates the tunnel route and says DNS will be configured
automatically. It was not. The hostname did not resolve until a CNAME was added by hand:

```
botharbor  CNAME  <tunnel-id>.cfargotunnel.com   (Proxied)
```

If a tunnel hostname returns nothing at all, check that the DNS record actually exists before
looking anywhere else -- the tunnel can be healthy with four registered connections while the
name simply does not resolve.

## Which host serves what

| Feature | Runs against | How |
| --- | --- | --- |
| Containers, images, run/stop/restart/pull/logs | **Cortex** | SSH, `NAS_*` |
| Stats: memory, CPU, disk, uptime, SSH logins | **Cortex** | SSH, `NAS_*` |
| Ollama: status, models, start/stop/restart | **server PC** | SSH (PowerShell), `SERVER_PC_*` |
| Ollama health panel | both | Windows service state over SSH to the server PC, **plus** an HTTP call from `bots-tailscale` on Cortex across the tailnet |

That last row is the one worth understanding. The panel reports two things separately because
they fail separately: the service can be `Running` while the port answers nothing, and the
tailnet path can be down while Ollama is perfectly healthy. Jam Junction only ever cares about
the second, so the HTTP call is made from inside `bots-tailscale` — whose network namespace Jam
Junction shares — rather than asking the server PC to call its own loopback, which would always
succeed and prove nothing.

`OLLAMA_HOST` must therefore be the server PC's **tailnet** address (`100.121.114.24`, the same
one Jam Junction uses for Lavalink). Ollama has no authentication of its own; the firewall rule
below is what keeps it to the tailnet.

## Ollama on the home server PC

Ollama runs natively on the server PC as a Windows service, wrapped by NSSM exactly like
`LavalinkNode`, so it starts at boot without anyone signing in.

**The quick way:** [`deploy/serverpc/Install-Ollama.ps1`](serverpc/Install-Ollama.ps1) does
steps 1–4 below and is safe to re-run. Copy it to the server PC and, signed in as `Server`, run
it from an **elevated** PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\Server\Documents\Ollama\Install-Ollama.ps1"
```

The steps it performs, if you would rather do them by hand (also elevated, as `Server`):

1. **Install Ollama** from <https://ollama.com/download/windows> while signed in as `Server`.
   Then stop the tray app and stop it starting at login. It binds 11434 too, and whichever
   starts first wins:

   ```powershell
   Get-Process 'ollama app', ollama -ErrorAction SilentlyContinue | Stop-Process
   Remove-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\Ollama.lnk" -ErrorAction SilentlyContinue
   ```

2. **Register the service.** `OLLAMA_HOST=0.0.0.0` makes it listen beyond loopback so Cortex can
   reach it; `OLLAMA_MODELS` keeps models in `Server`'s profile rather than the SYSTEM one.

   ```powershell
   $nssm   = 'C:\Users\Server\Documents\nssm-2.24\win64\nssm.exe'
   $ollama = 'C:\Users\Server\AppData\Local\Programs\Ollama\ollama.exe'
   & $nssm install Ollama $ollama serve
   & $nssm set Ollama AppEnvironmentExtra OLLAMA_HOST=0.0.0.0 OLLAMA_MODELS=C:\Users\Server\.ollama\models
   & $nssm set Ollama AppStdout C:\Users\Server\Documents\Ollama\ollama.log
   & $nssm set Ollama AppStderr C:\Users\Server\Documents\Ollama\ollama.log
   & $nssm set Ollama Start SERVICE_AUTO_START
   & $nssm start Ollama
   ```

3. **Firewall it to the tailnet.** Ollama has no authentication, so admit 11434 only from
   Tailscale's `100.64.0.0/10` range. Nothing on the LAN or the internet can then reach it.

   ```powershell
   New-NetFirewallRule -DisplayName 'Ollama (tailnet only)' -Direction Inbound -Protocol TCP `
       -LocalPort 11434 -RemoteAddress 100.64.0.0/10 -Action Allow
   ```

   If Windows offered an "allow ollama.exe" prompt during install, delete that broader rule in
   **Windows Defender Firewall → Inbound Rules**.

4. **Pull the model** Jam Junction uses (`OLLAMA_MODEL` in `deploy/bots/.env`):

   ```powershell
   ollama pull llama3.2:1b
   ```

   Or press **Pull llama3.2:1b** on the Commands page once `SERVER_PC_*` is set.

5. **Point Bot Harbor at it.** In `.env`: `OLLAMA_HOST=100.121.114.24`, plus `SERVER_PC_HOST`,
   `SERVER_PC_USERNAME` and `SERVER_PC_PASSWORD` for an administrator account (the server PC's
   OpenSSH server already listens on 22). Then `docker compose up -d botharbor`.

From Cortex, `docker exec bots-tailscale wget -q -O - http://100.121.114.24:11434/api/tags`
should print the model list.

## Troubleshooting

| Symptom | Cause / fix |
| --- | --- |
| `POSTGRES_PASSWORD is required` on `up` | `.env` is missing or blank. Compose fails fast on purpose rather than starting an unsecured database. |
| Node never appears in `tailscale status` | Auth key expired, already consumed (not reusable), or ephemeral. Mint a new one and `docker compose up -d --force-recreate tailscale`. |
| Browser shows a TLS error | The cert is still being issued, or HTTPS is off for the tailnet. Enable **HTTPS Certificates** under **DNS** in the admin console. |
| `Name or service not known: postgres` | Something set `TS_ACCEPT_DNS=true`. MagicDNS then replaces the resolver in the shared namespace and Docker service names stop resolving. |
| Login page loops back on itself | Data Protection keys were lost, so the auth cookie can't be decrypted. Check the `dataprotection-keys` volume is still mounted; clear cookies and log in again. |
| `relation "AspNetUsers" does not exist` | Migrations didn't run. Confirm `Database__MigrateOnStartup=true` and read `docker compose logs botharbor`. |
| Can't log in, fresh database | No bootstrap user was configured. Set `BOTHARBOR_ADMIN_EMAIL` / `_PASSWORD` in `.env` and `docker compose up -d botharbor`. |
| Ollama buttons say `Server PC SSH settings are missing` | `SERVER_PC_*` is blank in `.env`. |
| `Running without these server PC settings: ...` in the logs | Exactly what it says: those keys are blank in `.env`. Sign-in and the UI still work, so this is a warning, not a crash. |
| Ollama Start/Stop says `Access is denied` | The `SERVER_PC_USERNAME` account is not a local administrator on the server PC. |
| `permission denied ... /var/run/docker.sock` on every container panel | The Cortex account is not in the `docker` group. See step 3b, and log out and back in afterwards. |
| SSH logins chart is empty | The account is not in `systemd-journal`, or the unit is named `sshd` rather than `ssh`. The command asks for both. |
| Ollama panel: service `Running` but `no route` | The tailnet path from Cortex to the server PC is down, not Ollama. Or the firewall rule is missing, or the service lost `OLLAMA_HOST=0.0.0.0` and listens on loopback only. Check `tailscale status` on both, and `netstat -an \| findstr 11434` on the PC. |
| Ollama panel: service `NotInstalled` | The NSSM service is not registered. See **Ollama on the home server PC**. |
| Ollama panel: `no route` and service `unknown` | The server PC is unreachable entirely: asleep, rebooting for updates, or off. |
| Credentials are right but authentication fails | A value in `.env` contains a `$` and is not single-quoted, so Compose interpolated part of it away. Re-run `./deploy/configure.sh`, which quotes correctly. An apostrophe cannot be represented in `.env` at all; change that credential. |
| `up -d postgres` fails on an unrelated variable | Should not happen any more. Only `POSTGRES_PASSWORD` uses a `${VAR:?}` guard, because Compose interpolates the whole file before narrowing to the services you named, so a guard anywhere aborts every `up`. Don't add new ones. |
| `failed to bind host port 127.0.0.1:5432: address already in use` | UGOS runs its own PostgreSQL 15 on 5432 (`/usr/ugreen/etc/psql`). The stack no longer publishes a port, so this only happens if you added `docker-compose.dev.yml` on the NAS. Drop it; the app reaches `postgres:5432` over the compose network. |

## Local development

Postgres only, app from Rider:

```bash
# The dev overlay publishes 5432 on loopback; the NAS stack deliberately does not.
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d postgres
# appsettings.Development.json already points at localhost:5432
dotnet run --project BotDashboard.App
```

If something already owns 5432 on your workstation, set `POSTGRES_PORT` in `.env` and update
the connection string in `appsettings.Development.json` to match.

`BotHarbor:BehindReverseProxy` stays `false` outside the container, so local runs keep HTTPS
redirection and `UseHttpsRedirection` behaves normally.
