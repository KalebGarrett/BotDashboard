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
| SOCKS5 proxy for YouTube metadata | droplet loopback `127.0.0.1:14625` | Not needed |

The proxy turned out to be unnecessary here rather than broken. It exists for one reason: to
give YouTube a **residential egress IP**, because the droplet egresses from a datacenter
address YouTube throttles. A reverse SSH tunnel from the home PC publishes a SOCKS5 listener on
the droplet's loopback, and the bot reaches it only because it runs on that droplet with
`--network host`.

Cortex is on that same home connection. Running here, YouTube requests already egress from the
identical residential IP, so `PROXY_ENABLED=false` sends them out directly. That is strictly
better than re-pointing the tunnel: it removes a dependency on the home server PC being awake,
which today takes YouTube metadata down with it whenever it sleeps.

On the droplet nothing changes. With the variable unset the proxy stays on and uses the
compiled-in address exactly as before.

`TS_AUTHKEY` must be set in `.env` for the `audio` profile — the sidecar needs its own key,
separate from Bot Harbor's.

## Images

All six are `linux/amd64` and run natively on the DXP4800. They are built elsewhere and pulled
here; none builds from source on the NAS, because every bot's `Secrets/` folder is gitignored
and so absent from a fresh clone.

```bash
sudo docker compose pull && sudo docker compose up -d
```
