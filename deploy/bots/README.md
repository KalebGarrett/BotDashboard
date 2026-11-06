# Discord bots on Cortex

Five Discord bots, as a Compose stack separate from Bot Harbor.
Bot Harbor manages containers; these are the containers. Keeping them in their own project
means restarting a bot never touches the dashboard that restarts it.

```
/volume1/appdata/bots/        this stack
/volume1/appdata/botharbor/   the dashboard
```

## Before the first start

**One copy per bot token.** Discord permits one gateway connection per token. The same bot
running here and anywhere else means both receive every interaction: duplicate replies, and two
instances racing on the same command. Nothing warns you — it just looks like the bot has
started answering twice.

## Start

```bash
cd /volume1/appdata/bots
sudo docker compose up -d
```

That starts JokeBot, TriviaBot, FactBot and PremBot. They need no configuration: their secrets
are compiled into the images, and they need nothing but outbound internet.

## Jam Junction

Jam Junction and its `bots-tailscale` sidecar sit behind the `audio` profile and do not start with the command above:

```bash
sudo docker compose --profile audio up -d
```

Jam Junction has two external dependencies, both on the home server PC and both reached over
the tailnet, because neither has any authentication of its own.

| Dependency | Where it runs |
| --- | --- |
| Lavalink | home server PC, `100.121.114.24:2333` (NSSM service `LavalinkNode`) |
| Ollama | home server PC, `100.121.114.24:11434` (NSSM service `Ollama`) |

Ollama's setup on the PC is in [`deploy/README.md`](../README.md#ollama-on-the-home-server-pc).

The bot also supports a SOCKS5 proxy for YouTube metadata. It existed only to give YouTube a
residential egress IP while the bot ran on the droplet; Cortex is already on the home
connection, so `PROXY_ENABLED=false` sends requests out directly. Leave it off.

`TS_AUTHKEY` must be set in `.env` for the `audio` profile — the sidecar needs its own key,
separate from Bot Harbor's.

## Images

**`kalebg08/jamjunction` is private on Docker Hub.** `docker compose pull` will fail for it
unless Cortex is logged in (`docker login -u kalebg08` with a read-only access token). The
deploy path used instead needs no registry at all, and works regardless of visibility:

```bash
docker build -t kalebg08/jamjunction:latest -f JamJunction.App/Dockerfile JamJunction.App
docker save kalebg08/jamjunction:latest | gzip | ssh Kalebg808@192.168.1.167 "gunzip -c | docker load"
```

That only works because the Cortex account is in the `docker` group -- `sudo` would need a
password on the same stdin the image is travelling down. It also has to be built on a machine
that has `Secrets/`, since that folder is gitignored and absent from a fresh clone.

The other four bots remain public: the free Docker Hub plan allows one private repository.
Their Discord tokens are compiled into publicly pullable images and should be treated as
known. Jam Junction's token was rotated on 2026-11-04.



All six are `linux/amd64` and run natively on the DXP4800. They are built elsewhere and pulled
here; none builds from source on the NAS, because every bot's `Secrets/` folder is gitignored
and so absent from a fresh clone.

```bash
sudo docker compose pull && sudo docker compose up -d
```
