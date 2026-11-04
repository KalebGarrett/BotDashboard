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

check_droplet_id() {
    [[ "$1" =~ ^[0-9]+$ ]] && return 0
    echo "  a droplet id is all digits. Find it in the droplet's URL on DigitalOcean." >&2
    return 1
}

echo "Filling in $ENV_FILE"
echo "Press Ctrl-C at any point; nothing is written until a value is accepted."

ask TS_AUTHKEY \
    "Tailscale auth key. Must be Reusable, with Ephemeral OFF, or the node disappears on restart." \
    yes check_tailscale_key

ask DIGITALOCEAN_TOKEN \
    "DigitalOcean API token with read and write scope." \
    yes

ask DROPLET_ID   "Numeric id of the droplet running the bots." no check_droplet_id
ask DROPLET_HOST "Droplet IP or hostname." no
ask DROPLET_USERNAME "SSH user on the droplet (often root)." no
ask DROPLET_PASSWORD "SSH password for that user." yes

chmod 600 "$ENV_FILE"

echo
echo "Done. Remaining blanks (optional ones are fine to leave):"
grep -nE '^[A-Z_]+=$' "$ENV_FILE" | sed 's/^/  /' || echo "  none"
echo
echo "Next:"
echo "  sudo docker compose up -d && sudo docker compose logs -f"
