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

botharbor-app ──SSH / DigitalOcean API──▶ droplet that runs the bots
```

The droplet is unchanged. Cortex hosts the dashboard; the bots still live on DigitalOcean.

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

### 4. Fill in the environment

```bash
cp .env.example .env
nano .env
```

`POSTGRES_PASSWORD`, `TS_AUTHKEY` and `DIGITALOCEAN_TOKEN` are mandatory — compose refuses to
start without them. Set `BOTHARBOR_ADMIN_EMAIL` and `BOTHARBOR_ADMIN_PASSWORD` too: there is no
sign-up page, so this is the only way into a fresh database.

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
Without this the tunnel dies after 180 days. Same step you already did for `server-pc` and
`bots-ytcipher`.

## Decommissioning Azure

Only after you have logged in to the NAS instance:

1. Azure portal → delete the **App Service** and its **App Service Plan**.
2. Delete the **SQL Database**, then the **SQL Server** that hosted it.
3. Check **Cost Management → Cost analysis** next billing cycle for anything left running.
4. **Rotate the DigitalOcean API token and the droplet SSH password.** Both were set as Azure
   app settings, so treat them as exposed.

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

Built on the NAS:

```bash
cd /volume1/docker/botharbor
git pull
docker compose build botharbor && docker compose up -d botharbor
```

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
| Dashboard up but droplet actions fail | Droplet credentials or the DigitalOcean token are wrong — or you rotated them during Azure teardown and didn't update `.env`. |

## Local development

Postgres only, app from Rider:

```bash
docker compose up -d postgres
# appsettings.Development.json already points at localhost:5432
dotnet run --project BotDashboard.App
```

`BotHarbor:BehindReverseProxy` stays `false` outside the container, so local runs keep HTTPS
redirection and `UseHttpsRedirection` behaves normally.
