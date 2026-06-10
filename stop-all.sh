#!/usr/bin/env bash

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$REPO_ROOT/deploy/local/docker-compose.yml"

PURGE_DATA=0
SKIP_DOCKER=0

for arg in "$@"; do
  case "$arg" in
    --purge-data)
      PURGE_DATA=1
      ;;
    --skip-docker)
      SKIP_DOCKER=1
      ;;
    --help|-h)
      cat <<'USAGE'
Usage: ./stop-all.sh [--purge-data] [--skip-docker]

Stops local dev processes started by start-all.sh:
  - Backend: dotnet run --project src/Platform.Api --launch-profile http
  - Frontend: node node_modules/vite/bin/vite.js dev
  - Local Postgres from deploy/local/docker-compose.yml

Options:
  --purge-data  Also removes local postgres volume (fresh restart state equivalent)
  --skip-docker Skip stopping docker services (processes only)
USAGE
      exit 0
      ;;
    *)
      echo "Unknown option: $arg" >&2
      echo "Run ./stop-all.sh --help" >&2
      exit 1
      ;;
  esac
done

stop_processes() {
  local label="$1"
  local pattern="$2"
  local pids
  pids="$(pgrep -f -- "$pattern" || true)"

  if [ -z "$pids" ]; then
    echo "No $label process found."
    return 0
  fi

  echo "Stopping $label process(es):"
  echo "$pids"
  while read -r pid; do
    if [ -n "$pid" ]; then
      kill "$pid" 2>/dev/null || true
    fi
  done <<<"$pids"

  sleep 1
  local remaining=0
  while read -r pid; do
    if [ -n "$pid" ] && kill -0 "$pid" 2>/dev/null; then
      remaining=1
    fi
  done <<<"$pids"

  if [ "$remaining" -eq 1 ]; then
    while read -r pid; do
      if [ -n "$pid" ] && kill -0 "$pid" 2>/dev/null; then
        kill -9 "$pid" 2>/dev/null || true
      fi
    done <<<"$pids"
  fi
}

compose_down() {
  if [ "$SKIP_DOCKER" -eq 1 ]; then
    return 0
  fi

  local compose_cmd=()
  if command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1; then
    compose_cmd=(docker compose)
  elif command -v docker-compose >/dev/null 2>&1; then
    compose_cmd=(docker-compose)
  else
    echo "Docker compose command not found; skipping local postgres stop."
    return 0
  fi

  if [ ! -f "$COMPOSE_FILE" ]; then
    echo "Compose file missing: $COMPOSE_FILE"
    return 0
  fi

  local down_cmd=("${compose_cmd[@]}" -f "$COMPOSE_FILE" down)
  if [ "$PURGE_DATA" -eq 1 ]; then
    down_cmd+=("--volumes")
  fi

  echo "Stopping local postgres stack with: ${down_cmd[*]}"
  "${down_cmd[@]}" || true

  if [ "$PURGE_DATA" -eq 1 ]; then
    echo "Data purge enabled: local postgres volume will be removed."
  fi
}

stop_processes "backend" "dotnet run --project src/Platform.Api --launch-profile http"
stop_processes "frontend" "node node_modules/vite/bin/vite.js dev --host"
compose_down

echo "Stop completed."
