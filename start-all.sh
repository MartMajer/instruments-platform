#!/usr/bin/env bash

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ARTIFACTS_DIR="$REPO_ROOT/artifacts/dev"
DB_SETUP_SKIPPED=0
if [ -f "$REPO_ROOT/tools/frameworks/activate.sh" ]; then
  # shellcheck disable=SC1090
  . "$REPO_ROOT/tools/frameworks/activate.sh"
fi

for arg in "$@"; do
  case "$arg" in
    --skip-db)
      DB_SETUP_SKIPPED=1
      ;;
    --help|-h)
      echo "Usage: ./start-all.sh [--skip-db]"
      exit 0
      ;;
  esac
done

if command -v pwsh >/dev/null 2>&1; then
  POWERSHELL="pwsh"
elif command -v powershell >/dev/null 2>&1; then
  POWERSHELL="powershell"
else
  echo "PowerShell is required on Linux for existing repo setup scripts. Install pwsh." >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet is required. Install .NET 9 SDK before running this script." >&2
  exit 1
fi

if ! command -v node >/dev/null 2>&1; then
  echo "node is required. Install Node.js before running this script." >&2
  exit 1
fi

if ! command -v npm >/dev/null 2>&1; then
  echo "npm is required. Install Node.js/npm before running this script." >&2
  exit 1
fi

node_version_is_supported() {
  local node_version_raw="$1"
  local version="${node_version_raw#v}"
  local major="${version%%.*}"
  local rest="${version#*.}"
  local minor="${rest%%.*}"

  case "$major" in
    ''|*[!0-9]*)
      return 1
      ;;
  esac
  case "$minor" in
    ''|*[!0-9]*)
      return 1
      ;;
  esac

  if [ "$major" -ge 22 ]; then
    return 0
  fi

  if [ "$major" -eq 20 ] && [ "$minor" -ge 19 ]; then
    return 0
  fi

  return 1
}

NODE_VERSION="$(node --version)"
if ! node_version_is_supported "$NODE_VERSION"; then
  echo "Your Node.js version ($NODE_VERSION) is below the minimum required by this project."
  echo "Frontend requires Node >=20.19 (or any supported 22.x)."
  echo "Run: sudo tools/frameworks/install-linux-toolchain.sh, then retry."
  exit 1
fi

if [ "$DB_SETUP_SKIPPED" -eq 0 ] && ! command -v docker >/dev/null 2>&1; then
  echo "docker is required for setup-dev-db.ps1. Install Docker before running this script." >&2
  exit 1
fi

if [ "$DB_SETUP_SKIPPED" -eq 0 ]; then
  if ! docker info >/dev/null 2>&1; then
    echo "docker is installed but not accessible from this user session."
    echo "Run: newgrp docker"
    echo "or restart your shell after re-login, then retry."
    exit 1
  fi
fi

DB_SETUP_COMPLETED=0

mkdir -p "$ARTIFACTS_DIR"
mkdir -p "$REPO_ROOT/.dotnet-home" "$REPO_ROOT/.home"

export DOTNET_CLI_HOME="$REPO_ROOT/.dotnet-home"
export HOME="$REPO_ROOT/.home"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

if [ "$DB_SETUP_SKIPPED" -eq 0 ]; then
  (cd "$REPO_ROOT" && "$POWERSHELL" ./deploy/local/setup-dev-db.ps1)
  DB_SETUP_COMPLETED=1
fi

wait_for_postgres() {
  local host="127.0.0.1"
  local port="5432"
  local timeout_seconds="60"
  local wait_sec=0

  if command -v pg_isready >/dev/null 2>&1; then
    while [ "$wait_sec" -lt "$timeout_seconds" ]; do
      if pg_isready -h "$host" -p "$port" -d "instruments_platform_dev" >/dev/null 2>&1; then
        return 0
      fi

      sleep 1
      wait_sec=$((wait_sec + 1))
    done
  fi

  while [ "$wait_sec" -lt "$timeout_seconds" ]; do
    if (cat </dev/tcp/$host/$port) >/dev/null 2>&1; then
      return 0
    fi

    sleep 1
    wait_sec=$((wait_sec + 1))
  done

  return 1
}

if [ "$DB_SETUP_SKIPPED" -eq 0 ] && [ "$DB_SETUP_COMPLETED" -eq 0 ]; then
  echo "Waiting for local PostgreSQL on 127.0.0.1:5432..."
  if ! wait_for_postgres; then
    echo "PostgreSQL did not become reachable on localhost:5432."
    echo "Hint: run 'docker ps' and 'docker compose -f deploy/local/docker-compose.yml logs' or 'docker-compose -f deploy/local/docker-compose.yml logs' and rerun start-all.sh."
    exit 1
  fi
fi

if [ ! -f "$REPO_ROOT/apps/web/node_modules/vite/bin/vite.js" ]; then
  echo "Frontend dependencies missing; running npm install --prefix apps/web"
  (cd "$REPO_ROOT" && npm install --prefix apps/web)
fi

API_LOG="$ARTIFACTS_DIR/api.log"
WEB_LOG="$ARTIFACTS_DIR/web.log"

cleanup() {
  if [ -n "${WEB_PID:-}" ]; then
    kill "$WEB_PID" 2>/dev/null || true
  fi
  if [ -n "${API_PID:-}" ]; then
    kill "$API_PID" 2>/dev/null || true
  fi
}

trap cleanup EXIT

(
  cd "$REPO_ROOT"
  ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project src/Platform.Api --launch-profile http \
    >"$API_LOG" 2>&1
) &
API_PID=$!

(
  cd "$REPO_ROOT/apps/web"
  PUBLIC_DEV_AUTH_ENABLED=true \
  PUBLIC_API_BASE_URL="http://127.0.0.1:5055" \
  node node_modules/vite/bin/vite.js dev --host 127.0.0.1 \
    >"$WEB_LOG" 2>&1
) &
WEB_PID=$!

echo "Backend:  http://127.0.0.1:5055 (log: $API_LOG)"
echo "Frontend: http://127.0.0.1:5173 (log: $WEB_LOG)"
echo "Press Ctrl+C to stop both processes."

wait $API_PID $WEB_PID
