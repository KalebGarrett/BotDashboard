# Discord bots on Cortex

Five Discord bots plus a YouTube cipher helper, as a Compose stack separate from Bot Harbor.
Bot Harbor manages containers; these are the containers. Keeping them in their own project
means restarting a bot never touches the dashboard that restarts it.

```
/volume1/appdata/bots/        this stack
/volume1/appdata/botharbor/   the dashboard
```

## Before the first start

**Stop the droplet copies.** Discord permits one gateway connection per bot token. The same bot
running here and on the droplet means both receive every interaction: duplicate replies, and
two instances racing on the same command. Nothing warns you — it just looks like the bot has
started answering twice.

Once the `DROPLET_*` settings are in Bot Harbor's `.env`, **Commands → Stop All Containers**
does it. Otherwise, on the droplet:

```bash
docker stop $(docker ps -q)
```

## Start

```bash
cd /volume1/appdata/bots
sudo docker compose up -d
```

That starts JokeBot, TriviaBot, FactBot and PremBot. They need no configuration: their secrets
are compiled into the images, and they need nothing but outbound internet.

## Jam Junction and yt-cipher

Both sit behind the `audio` profile and do not start with the command above:

```bash
sudo docker compose --profile audio up -d
```

Jam Junction has three external dependencies, and only two of them survive the move to the NAS.

| Dependency | Where it runs | Works from the NAS? |
| --- | --- | --- |
| Lavalink | home server PC, `100.121.114.24:2333` | Yes, over Tailscale |
| Ollama | droplet `bots-ytcipher`, `100.120.70.97:11434` | Yes, over Tailscale |
| SOCKS5 proxy for YouTube metadata | droplet loopback `127.0.0.1:14625` | **No** |

The proxy is a reverse SSH tunnel: a Windows scheduled task on the home PC runs
`ssh -N -R 14625 root@<droplet>`, publishing a SOCKS5 listener on the **droplet's** loopback.
Jam Junction reaches it only because it runs on that droplet with `--network host`. From the
NAS there is no such listener, and `ProxySecrets.Address` is a compile-time `const`, so it
cannot be repointed with an environment variable the way Lavalink and Ollama now can.

Moving Jam Junction here therefore needs, first:

1. The tunnel re-pointed at the NAS (`ssh -N -R 14625 <user>@<nas>`), and
2. `ProxySecrets` made environment-overridable, the same way `LavalinkSecrets` was, then a
   rebuild and push of `kalebg08/jamjunction`.

Until both are done, Jam Junction on the NAS loses YouTube metadata resolution. It will still
start, and other platforms still work, so the failure looks like a bot bug rather than a
missing tunnel.

yt-cipher is the one thing still running on the droplet, with `restart: unless-stopped`,
publishing port 8001. Lavalink on the home PC already reaches it there. Moving it here
would add a hop that does not exist today, so it is parked behind the profile.

`TS_AUTHKEY` must be set in `.env` for the `audio` profile — the sidecar needs its own key,
separate from Bot Harbor's.

## Images

All six are `linux/amd64` and run natively on the DXP4800. They are built elsewhere and pulled
here; none builds from source on the NAS, because every bot's `Secrets/` folder is gitignored
and so absent from a fresh clone.

```bash
sudo docker compose pull && sudo docker compose up -d
```
