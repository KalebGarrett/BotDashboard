# Bot Dashboard

A personal project for managing my Discord bots through a web-based dashboard. The dashboard is built using Blazor and .NET 10, with a modern UI powered by MudBlazor. It also utilizes SSH.NET for secure server communications.

## Features

- **Real-time Monitoring:** Keep track of my bots' status and activities.
- **Command Execution:** Execute Ubuntu and Docker commands directly from the dashboard.
- **Customizable UI:** Utilizes MudBlazor for a sleek and responsive interface.

## Technologies Used
| Name | Description |
| ------------- | ------------- |
| **[.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)** | Provides a robust and high-performance backend. |
| **[Blazor](https://github.com/dotnet/blazor)** | For building the interactive web UI. |
| **[MudBlazor](https://github.com/MudBlazor/MudBlazor)** | A Material Design component library for Blazor. |
| **[PostgreSQL](https://www.postgresql.org/)** | Stores the operator account. Runs in a container on the NAS. |
| **[Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/)** | Publishes the dashboard with no port forward and no public IP. Cloudflare Access sits in front of it. |
| **[Tailscale](https://tailscale.com/)** | Not used by the dashboard. It is how the bots reach Ollama and Lavalink, which have no authentication of their own. |
| **[SSH.NET](https://github.com/sshnet/SSH.NET)** | Enables secure SSH operations with remote servers. |

## Hosting

Everything is **self-hosted on Cortex**, a UGREEN DXP4800 Plus NAS, as two Docker Compose
stacks. The dashboard used to be an Azure App Service with an Azure SQL database; both are
gone, along with the bill.

```text
Browser ──HTTPS──▶ botharbor.<your-domain>
                        │  Cloudflare Access: email login before anything reaches the app
                        ▼
                   Cloudflare Tunnel  (botharbor-cloudflared)
                        │
                        ▼
                   botharbor-app ──▶ botharbor-postgres
                        │
                        ├──SSH──▶ Cortex: Docker, the bots, every figure on the Stats page
                        └──SSH──▶ home server PC: the Ollama service
```

`cloudflared` runs inside the app container's network namespace, so rebuilding the app takes
the tunnel's network with it. Recreate `cloudflared` after any app rebuild — `deploy/README.md`
has the exact sequence.

### Where the bots run

All six run on Cortex, in a second stack under `deploy/bots`:

| Stack | Containers |
| ----- | ---------- |
| `botharbor` | `botharbor-app`, `botharbor-postgres`, `botharbor-cloudflared` |
| `bots` | `jokebot`, `triviabot`, `factbot`, `prembot`, and behind the `audio` profile `jamjunction`, `bots-tailscale` |

The four simple bots need nothing but outbound internet. Jam Junction is the exception: it runs
inside `bots-tailscale`'s network namespace, because it talks to Lavalink and Ollama on the home
server PC, and neither has any authentication of its own. The tailnet is what
keeps them off the public internet rather than something the dashboard needs for itself.

The DigitalOcean droplet is retired. Ollama moved to the home server PC beside Lavalink, and the
YouTube cipher helper went with the droplet: it was only ever needed for the public Lavalink node
that used to run there.

**[Setup, operations and troubleshooting → `deploy/README.md`](deploy/README.md)**

Quick start on the NAS:

```bash
cp .env.example .env   # or run ./deploy/configure.sh, which quotes values correctly
docker compose --profile cloudflare up -d
```

Local development needs only the database. The dev overlay publishes it on loopback, which
the NAS deliberately does not do:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d postgres
dotnet run --project BotDashboard.App
```

## Stats
![Alt](https://repobeats.axiom.co/api/embed/7be079b55ec3c0da14d57536b4ec1d585e7f0076.svg "Repobeats analytics image")

## Screenshots

![image](/Documentation/Images/Dashboard.png)
