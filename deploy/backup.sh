#!/usr/bin/env bash
# Dump the Bot Harbor PostgreSQL database to a gzipped file and prune old dumps.
#
#   ./deploy/backup.sh [destination-dir]
#
# Defaults to /volume1/backups/botharbor on the NAS. Safe to run from the UGOS task scheduler.

set -euo pipefail

DEST="${1:-/volume1/backups/botharbor}"
RETAIN_DAYS="${RETAIN_DAYS:-30}"
COMPOSE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$COMPOSE_DIR"

# Read POSTGRES_* from .env without exporting the rest of the file.
POSTGRES_USER="$(grep -E '^POSTGRES_USER=' .env | cut -d= -f2-)"
POSTGRES_DB="$(grep -E '^POSTGRES_DB=' .env | cut -d= -f2-)"
POSTGRES_USER="${POSTGRES_USER:-botharbor}"
POSTGRES_DB="${POSTGRES_DB:-botharbor}"

mkdir -p "$DEST"

STAMP="$(date +%Y%m%d-%H%M%S)"
TARGET="$DEST/botharbor-$STAMP.sql.gz"

echo "Dumping $POSTGRES_DB -> $TARGET"

# Write to a .partial first so a failed or interrupted dump is never mistaken for a good one.
if docker compose exec -T postgres \
      pg_dump --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --clean --if-exists \
    | gzip > "$TARGET.partial"; then
  mv "$TARGET.partial" "$TARGET"
else
  rm -f "$TARGET.partial"
  echo "Dump failed, nothing written." >&2
  exit 1
fi

echo "Wrote $(du -h "$TARGET" | cut -f1) to $TARGET"

echo "Pruning dumps older than $RETAIN_DAYS days"
find "$DEST" -name 'botharbor-*.sql.gz' -type f -mtime "+$RETAIN_DAYS" -print -delete

echo "Done."
