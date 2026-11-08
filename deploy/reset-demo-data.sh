#!/usr/bin/env bash
# Resets the demo data of the acceptance environment, as scripts/reset-demo-screenings.sh does on a developer's computer:
# deletes the demo screenings (identifiers starting with 33333333-) and their reservations, then reloads seed-dev.sql
# and seed-posters.sql of the given branch or commit, in one transaction, and checks that no room holds two screenings
# at the same time. Reloading seed-dev.sql alone on another day added a second programme next to the first one
# (bug 0002: two movies in the same room at the same time).
# Kept: accounts, movies (those added by the manager too) and the screenings created in the programming screen.
# Usage, on the EC2 instance (Session Manager): sudo bash reset-demo-data.sh [branch-or-commit, dev by default]
set -euo pipefail

REF="${1:-dev}"
APP_DIR="/opt/grandcinema"
RAW_URL="https://raw.githubusercontent.com/Grand-Cinema-Studi/grand-cinema-backend/$REF/docs/database"
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT

curl --proto '=https' --tlsv1.2 -fsSL "$RAW_URL/seed-dev.sql" -o "$WORK_DIR/seed-dev.sql"
curl --proto '=https' --tlsv1.2 -fsSL "$RAW_URL/seed-posters.sql" -o "$WORK_DIR/seed-posters.sql"

cd "$APP_DIR"

# psql in the database container, with the user and the database of the container (POSTGRES_USER, POSTGRES_DB)
psql_db() {
    docker compose exec -T db sh -c 'psql --quiet -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" "$@"' psql "$@"
}

{
    cat <<'SQL'
DELETE FROM "Reservations" WHERE "ScreeningId"::text LIKE '33333333-%';
DELETE FROM "Screenings" WHERE "Id"::text LIKE '33333333-%';
SQL
    cat "$WORK_DIR/seed-dev.sql" "$WORK_DIR/seed-posters.sql"
} | psql_db --single-transaction

# The rule of the API (409 room-not-available): a room is busy from the start of a screening to the end of its movie, plus 15 minutes of cleaning
OVERLAPS=$(psql_db --tuples-only --no-align <<'SQL'
SELECT count(*)
FROM "Screenings" s1
JOIN "Movies" m1 ON m1."Id" = s1."MovieId"
JOIN "Screenings" s2 ON s2."RoomId" = s1."RoomId" AND s2."Id" > s1."Id" AND s2."CancelledAt" IS NULL
JOIN "Movies" m2 ON m2."Id" = s2."MovieId"
WHERE s1."CancelledAt" IS NULL
  AND s1."StartsAt" < s2."StartsAt" + make_interval(mins => m2."DurationMinutes" + 15)
  AND s2."StartsAt" < s1."StartsAt" + make_interval(mins => m1."DurationMinutes" + 15);
SQL
)

psql_db -c 'SELECT (SELECT count(*) FROM "Movies") AS movies, (SELECT count(*) FROM "Screenings" WHERE "StartsAt" >= now()) AS upcoming_screenings;'

if [[ "$OVERLAPS" != "0" ]]; then
    echo "$OVERLAPS overlapping screenings left: cancel the screenings created by hand on those slots (Programmation), then run this script again." >&2
    exit 1
fi
echo "Demo data reset from $REF: no room holds two screenings at the same time."
