#!/usr/bin/env bash
# Deploys the API and its database on the EC2 instance with Docker Compose.
# Usage: deploy.sh <commit-sha>
# The image ghcr.io/grand-cinema-studi/grand-cinema-backend:<commit-sha> must have been pushed.
set -euo pipefail

COMMIT="$1"
REGION="eu-west-3"
APP_DIR="/opt/grandcinema"
RAW_URL="https://raw.githubusercontent.com/Grand-Cinema-Studi/grand-cinema-backend/$COMMIT/deploy"

parameter() {
    aws ssm get-parameter --region "$REGION" --name "/grandcinema/recette/$1" --with-decryption --query Parameter.Value --output text
}

mkdir -p "$APP_DIR"
cd "$APP_DIR"

curl --proto '=https' --tlsv1.2 -fsSL "$RAW_URL/docker-compose.prod.yml" -o docker-compose.yml

umask 077
cat > .env <<EOF
POSTGRES_PASSWORD=$(parameter postgres-password)
SWAGGER_ENABLED=$(parameter swagger-enabled)
JWT_SIGNING_KEY=$(parameter jwt-signing-key)
API_IMAGE=ghcr.io/grand-cinema-studi/grand-cinema-backend:$COMMIT
EOF

docker compose pull
docker compose up -d --remove-orphans
docker image prune -af

for attempt in $(seq 1 30); do
    if curl -fs http://localhost/health > /dev/null; then
        echo "Deployed commit $COMMIT"
        exit 0
    fi
    sleep 2
done

echo "The API did not become healthy" >&2
docker compose logs --tail 50 migrate api >&2
exit 1
