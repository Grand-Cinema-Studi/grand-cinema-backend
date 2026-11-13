#!/usr/bin/env bash
# Non-disruptive restore test (production criterion "automatic backup active and restore tested"):
# restores a backup into a separate database, compares it with the database in service, then drops it.
# The API and its database are not touched. deploy.sh installs it as /opt/grandcinema/restore-test.sh.
# Run it after each release, and record the result in the deployment guide of the documentation repository.
# Usage, on the EC2 instance (Session Manager): sudo /opt/grandcinema/restore-test.sh [backups/grandcinema-YYYY-MM-DD.sql.gz]
# Without an argument, the latest backup is restored.
set -euo pipefail

APP_DIR="/opt/grandcinema"
TEST_DB="grandcinemadb_restore_test"
cd "$APP_DIR"

BACKUP="${1:-$(find backups -maxdepth 1 -name 'grandcinema-*.sql.gz' | sort | tail -n 1)}"
if [[ -z "$BACKUP" || ! -f "$BACKUP" ]]; then
    echo "No backup to restore (${BACKUP:-backups/ is empty})." >&2
    exit 1
fi

# psql in the database container, with the user of the container; the database is the first argument,
# empty for the database in service (POSTGRES_DB)
psql_on() {
    local database="$1"
    shift
    docker compose exec -T -e TARGET_DB="$database" db \
        sh -c 'psql --quiet -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "${TARGET_DB:-$POSTGRES_DB}" "$@"' psql "$@"
}

drop_test_db() {
    docker compose exec -T -e TARGET_DB="$TEST_DB" db sh -c 'dropdb -U "$POSTGRES_USER" --if-exists "$TARGET_DB"'
}

# A test database left by an interrupted run is dropped first, and this one is dropped whatever happens
drop_test_db
trap drop_test_db EXIT
docker compose exec -T -e TARGET_DB="$TEST_DB" db sh -c 'createdb -U "$POSTGRES_USER" "$TARGET_DB"'

START=$(date +%s%N)
gunzip -c "$BACKUP" | psql_on "$TEST_DB" > /dev/null
ELAPSED_MS=$(( ($(date +%s%N) - START) / 1000000 ))

COUNTS='SELECT (SELECT count(*) FROM "__EFMigrationsHistory"), (SELECT count(*) FROM "Users"), (SELECT count(*) FROM "Movies"),
    (SELECT count(*) FROM "Screenings"), (SELECT count(*) FROM "Reservations"), (SELECT count(*) FROM "ReservedSeats");'
RESTORED=$(psql_on "$TEST_DB" --tuples-only --no-align -c "$COUNTS")
IN_SERVICE=$(psql_on "" --tuples-only --no-align -c "$COUNTS")

echo "Restore test of $BACKUP ($(du -h "$BACKUP" | cut -f1), written $(date -r "$BACKUP" '+%d/%m/%Y %H:%M')), run $(date '+%d/%m/%Y %H:%M')"
printf 'Restored in %d.%03d s\n' $((ELAPSED_MS / 1000)) $((ELAPSED_MS % 1000))
echo "             migrations|users|movies|screenings|reservations|reserved_seats"
echo "restored:    $RESTORED"
echo "in service:  $IN_SERVICE"
if [[ "$RESTORED" == "$IN_SERVICE" ]]; then
    echo "Result: identical."
else
    echo "Result: different. Normal only for what was entered in the acceptance environment since the backup was written."
fi
