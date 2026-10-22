#!/usr/bin/env bash
# Resets the demo screenings of the development database: deletes them, then reloads docs/database/seed-dev.sql,
# so that the demo schedule (several weeks of screenings) always starts from today,
# then applies docs/database/seed-posters.sql when it exists (posters, release dates and running times from TMDB).
# Demo screenings are recognised by their identifier, which starts with 33333333- (see seed-dev.sql).
# Usage, from anywhere in the repository: scripts/reset-demo-screenings.sh
set -euo pipefail

cd "$(dirname "$0")/.."

env_value() {
    grep -E "^$1=" .env | head -n 1 | cut -d '=' -f 2- | tr -d '\r'
}

DB_USER=$(env_value POSTGRES_USER)
DB_NAME=$(env_value POSTGRES_DB)

if [ -z "$(docker compose ps --quiet --status running db)" ]; then
    echo "PostgreSQL is not running: start it with 'docker compose up -d'." >&2
    exit 1
fi

{
    cat <<'SQL'
DELETE FROM "Reservations" WHERE "ScreeningId"::text LIKE '33333333-%';
DELETE FROM "Screenings" WHERE "Id"::text LIKE '33333333-%';
SQL
    cat docs/database/seed-dev.sql
    if [ -f docs/database/seed-posters.sql ]; then
        cat docs/database/seed-posters.sql
    fi
} | docker compose exec -T db psql --quiet --single-transaction -v ON_ERROR_STOP=1 -U "$DB_USER" -d "$DB_NAME"

echo "Demo screenings reset from today ($(date +%F)) for the coming weeks."
if [ ! -f docs/database/seed-posters.sql ]; then
    echo "No posters yet: run TMDB_TOKEN=<token> node scripts/fetch-tmdb-posters.mjs, then this script again."
fi
