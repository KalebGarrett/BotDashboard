#!/usr/bin/env bash
#
# Fill in the secrets in .env without an editor.
#
#   ./deploy/configure.sh
#
# Secrets are read with the terminal echo off, so nothing appears on screen, and because
# they are typed at a prompt rather than passed as arguments they never reach your shell
# history or the process list. Values already set are shown as "set" and left alone unless
# you ask to replace them, so the script is safe to re-run.

set -euo pipefail

ENV_FILE="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/.env"
EXAMPLE_FILE="$(dirname "$ENV_FILE")/.env.example"

if [[ ! -f "$ENV_FILE" ]]; then
    if [[ ! -f "$EXAMPLE_FILE" ]]; then
        echo "error: neither .env nor .env.example found next to this script's parent." >&2
        exit 1
    fi
    echo "No .env yet — starting from .env.example."
    cp "$EXAMPLE_FILE" "$ENV_FILE"
    chmod 600 "$ENV_FILE"
fi

current() {
    # First match wins, and the value keeps any '=' it contains. Strips the single quotes
    # set_var adds, so a re-run compares against the real value.
    sed -n "s/^$1=//p" "$ENV_FILE" | head -1 | sed -E "s/^'(.*)'$/\1/"
}

# Rewrite one key in place. The value travels through the environment rather than through
# awk's argv or a sed replacement, so characters like & | / and \ need no escaping there.
#
# Values are always written single-quoted. Compose interpolates .env before handing the
# result to a service, so a bare or double-quoted `p$ssword` silently loses `$ssword` and
# you get an authentication failure with nothing in the logs to explain it. Single quotes
# are the only form that survives: verified against Compose v5.1.3, where bare and
# double-quoted `a$b#c|d` both resolved to `a#c|d` while the single-quoted form came
# through intact.
set_var() {
    local key="$1" value="$2" tmp
    tmp="$(mktemp "${ENV_FILE}.XXXXXX")"
    chmod 600 "$tmp"
    VALUE="'$value'" awk -v key="$key" '
        BEGIN { FS = "="; done = 0 }
        !done && $1 == key { print key "=" ENVIRON["VALUE"]; done = 1; next }
        { print }
        END { if (!done) print key "=" ENVIRON["VALUE"] }
    ' "$ENV_FILE" > "$tmp"
    mv "$tmp" "$ENV_FILE"
}

# A single quote inside a single-quoted .env value cannot be escaped, so there is no safe
# encoding for it. Refuse rather than write something Compose will mangle without saying so.
reject_apostrophe() {
    if printf '%s' "$1" | grep -q "'"; then
        echo "  that value contains an apostrophe, which .env cannot represent safely." >&2
        echo "  Change the credential, or set it in the shell environment instead." >&2
        return 1
    fi
    return 0
}

# ask <KEY> <description> <secret:yes|no> <validator-or-empty>
ask() {
    local key="$1" description="$2" secret="$3" validator="${4:-}"
    local existing value

    existing="$(current "$key")"
    if [[ -n "$existing" ]]; then
        local shown="set"
        [[ "$secret" == "no" ]] && shown="$existing"
        read -r -p "$key is already $shown. Replace it? [y/N] " reply
        [[ "$reply" =~ ^[Yy]$ ]] || { echo "  kept"; return; }
    fi

    while true; do
        echo
        echo "$key — $description"
        if [[ "$secret" == "yes" ]]; then
            read -r -s -p "  value (hidden): " value
            echo
        else
            read -r -p "  value: " value
        fi

        if [[ -z "$value" ]]; then
            echo "  cannot be empty." >&2
            continue
        fi
        if ! reject_apostrophe "$value"; then
            continue
        fi
        if [[ -n "$validator" ]] && ! $validator "$value"; then
            continue
        fi
        break
    done

    set_var "$key" "$value"
    echo "  saved"
}

check_tailscale_key() {
    [[ "$1" == tskey-* ]] && return 0
    echo "  that does not look like a Tailscale key (they start with 'tskey-')." >&2
    echo "  mint one at https://login.tailscale.com/admin/settings/keys" >&2
    return 1
}

check_tunnel_token() {
    # Cloudflare tunnel tokens are base64 JSON, so they always start with the encoding
    # of {"a": -- catching a half-copied paste here beats a connector that silently
    # refuses to register later.
    case "$1" in
        eyJ*) return 0 ;;
    esac
    echo "  that does not look like a tunnel token (they start with eyJ)." >&2
    return 1
}

check_tailnet_address() {
    # Tailscale hands out 100.64.0.0/10. A public IP here would mean reaching an
    # unauthenticated LLM server across the internet, so reject it rather than warn.
    case "$1" in
        100.*)    return 0 ;;
        *.ts.net) return 0 ;;
    esac
    echo "  not a tailnet address. Tailscale addresses start with 100. or end in .ts.net." >&2
    echo "  Ollama has no authentication, so it must not be reached over the public" >&2
    echo "  internet. Run: tailscale status   on the server PC to find its address." >&2
    return 1
}

echo "Filling in $ENV_FILE"
echo "Press Ctrl-C at any point; nothing is written until a value is accepted."

ask NAS_HOST "IP or hostname of Cortex. Containers, images and the Stats page all read from here." no

ask NAS_USERNAME "SSH user on Cortex. Must be in the docker and systemd-journal groups." no

ask NAS_PASSWORD "Password for that Cortex account." yes

ask OLLAMA_HOST "The server PC TAILNET address (100.x.y.z), where Ollama runs." no check_tailnet_address

ask TS_AUTHKEY \
    "Tailscale auth key. Must be Reusable, with Ephemeral OFF, or the node disappears on restart." \
    yes check_tailscale_key

ask SERVER_PC_HOST "IP or hostname of the home server PC, where Ollama runs as a Windows service." no
ask SERVER_PC_USERNAME "SSH user on the server PC. Must be a local administrator." no
ask SERVER_PC_PASSWORD "Password for that server PC account." yes

echo
read -r -p "Set up the Cloudflare Tunnel as well? [y/N] " want_cloudflare
if [[ "$want_cloudflare" =~ ^[Yy]$ ]]; then
    ask CLOUDFLARE_TUNNEL_TOKEN "Tunnel token from the tunnel Overview tab in the Cloudflare dashboard." yes check_tunnel_token
fi

chmod 600 "$ENV_FILE"

echo
echo "Done. Remaining blanks (optional ones are fine to leave):"
grep -nE '^[A-Z_]+=$' "$ENV_FILE" | sed 's/^/  /' || echo "  none"
echo
echo "Next:"
echo "  sudo docker compose up -d && sudo docker compose logs -f"
