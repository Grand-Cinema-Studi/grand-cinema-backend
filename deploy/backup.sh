#!/usr/bin/env bash
# Daily backup of the acceptance database. deploy.sh installs it as /etc/cron.daily/grandcinema-backup:
# a new server receives it with its first deployment, and every deployment keeps it up to date.
# - The dump is written under a temporary name, then checked: a failed pg_dump never leaves a truncated file under a valid name.
# - The backups are readable by root only: they hold the password hashes of the accounts.
# - Eight dumps are kept on the server: today's and the seven before.
# - When the parameter /grandcinema/recette/backups-bucket exists, each dump is also copied to that private bucket
#   (expired after 30 days by its lifecycle rule): the loss of the server no longer takes its backups with it.
# Usage, by hand on the EC2 instance (Session Manager): sudo /etc/cron.daily/grandcinema-backup
set -euo pipefail
umask 077

REGION="eu-west-3"
APP_DIR="/opt/grandcinema"
BACKUP_DIR="$APP_DIR/backups"
BACKUP="$BACKUP_DIR/grandcinema-$(date +%F).sql.gz"
PARTIAL="$BACKUP.partial"
trap 'rm -f "$PARTIAL"' EXIT

mkdir -p "$BACKUP_DIR"
chmod 700 "$BACKUP_DIR"
cd "$APP_DIR"

# pg_dump in the database container, with the user and the database of the container (POSTGRES_USER, POSTGRES_DB)
docker compose exec -T db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB"' | gzip > "$PARTIAL"
gzip --test "$PARTIAL"
mv "$PARTIAL" "$BACKUP"
# The dumps written before this script, by the job installed by hand, were readable by everyone
chmod 600 "$BACKUP_DIR"/*.sql.gz

find "$BACKUP_DIR" -name '*.sql.gz' -mtime +7 -delete

BUCKET=$(aws ssm get-parameter --region "$REGION" --name /grandcinema/recette/backups-bucket --query Parameter.Value --output text 2>/dev/null || true)
if [[ -z "$BUCKET" ]]; then
    echo "No parameter /grandcinema/recette/backups-bucket: $BACKUP stays on the server only." >&2
    exit 0
fi
aws s3 cp "$BACKUP" "s3://$BUCKET/database/$(basename "$BACKUP")" --region "$REGION" --only-show-errors
echo "Backup $BACKUP written and copied to s3://$BUCKET/database/."
