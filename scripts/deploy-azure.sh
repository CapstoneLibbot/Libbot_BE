#!/usr/bin/env bash
set -Eeuo pipefail

IMAGE_REPOSITORY="ghcr.io/capstonelibbot/libbot-api"
IMAGE_TAG="${IMAGE_TAG:-latest}"
CONTAINER_NAME="libot-api"
ENV_FILE="/var/www/libbot-api/.env"
BACKUP_NAME=""
DEPLOY_SUCCEEDED=0

if [[ -z "${GHCR_READ_TOKEN:-}" && ! -t 0 ]]; then
    IFS= read -r GHCR_READ_TOKEN || true
fi

rollback() {
    local exit_code=$?
    if [[ "$DEPLOY_SUCCEEDED" -eq 1 ]]; then
        return
    fi

    echo "Deployment failed; attempting to restore the previous container." >&2
    docker logs --tail 100 "$CONTAINER_NAME" >&2 2>/dev/null || true
    docker rm -f "$CONTAINER_NAME" >/dev/null 2>&1 || true

    if [[ -n "$BACKUP_NAME" ]] && docker container inspect "$BACKUP_NAME" >/dev/null 2>&1; then
        docker rename "$BACKUP_NAME" "$CONTAINER_NAME" || true
        docker start "$CONTAINER_NAME" || true
        echo "Previous container restored. Recent logs:" >&2
        docker logs --tail 100 "$CONTAINER_NAME" >&2 || true
    fi

    exit "$exit_code"
}
trap rollback ERR

if [[ ! -r "$ENV_FILE" ]]; then
    echo "Environment file is missing or unreadable: $ENV_FILE" >&2
    exit 1
fi

if [[ -n "${GHCR_READ_TOKEN:-}" ]]; then
    : "${GHCR_USERNAME:?GHCR_USERNAME is required when GHCR_READ_TOKEN is set}"
    printf '%s' "$GHCR_READ_TOKEN" | docker login ghcr.io \
        --username "$GHCR_USERNAME" \
        --password-stdin
fi

IMAGE="$IMAGE_REPOSITORY:$IMAGE_TAG"
echo "Pulling $IMAGE"
docker pull "$IMAGE"

if docker container inspect "$CONTAINER_NAME" >/dev/null 2>&1; then
    BACKUP_NAME="${CONTAINER_NAME}-previous-$(date +%s)"
    docker stop "$CONTAINER_NAME"
    docker rename "$CONTAINER_NAME" "$BACKUP_NAME"
fi

echo "Starting $CONTAINER_NAME from $IMAGE"
docker run -d \
    --name "$CONTAINER_NAME" \
    --restart unless-stopped \
    --add-host=host.docker.internal:host-gateway \
    --env-file "$ENV_FILE" \
    --env ASPNETCORE_URLS=http://+:8080 \
    --env Database__ApplyMigrations=false \
    -p 5205:8080 \
    "$IMAGE"

health_ok=0
for attempt in $(seq 1 12); do
    if [[ "$(docker inspect --format '{{.State.Running}}' "$CONTAINER_NAME")" != "true" ]]; then
        echo "New container stopped during startup." >&2
        docker logs --tail 100 "$CONTAINER_NAME" >&2 || true
        false
    fi

    if curl --fail --silent --show-error --max-time 3 \
        http://127.0.0.1:5205/api/health >/dev/null; then
        health_ok=1
        break
    fi

    echo "Waiting for LiBot health endpoint (attempt $attempt/12)..."
    sleep 5
done

if [[ "$health_ok" -ne 1 ]]; then
    echo "New container did not pass the health check." >&2
    docker logs --tail 100 "$CONTAINER_NAME" >&2 || true
    false
fi

if [[ -n "$BACKUP_NAME" ]]; then
    docker rm -f "$BACKUP_NAME"
    BACKUP_NAME=""
fi

DEPLOY_SUCCEEDED=1
trap - ERR
echo "Deployment succeeded: $CONTAINER_NAME uses $IMAGE"
docker ps --filter "name=^/${CONTAINER_NAME}$"
