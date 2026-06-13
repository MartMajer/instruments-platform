#!/usr/bin/env bash

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TARGET="src/Platform.Api/Platform.Api.csproj"
VERBOSITY="minimal"
USE_SOLUTION=0
CLEAR_CACHE=0
RUN_NETWORK_CHECK=1

usage() {
  cat <<'USAGE'
Usage: ./tools/frameworks/fix-dotnet-restore.sh [options]

Runs a reliable .NET restore with repository-local cache/home paths.

Options:
  --solution                   Restore the root Platform.slnx instead of API project.
  --project <path>             Restore a specific project (default: src/Platform.Api/Platform.Api.csproj)
  --clear-cache                Clear NuGet locals before restoring.
  --no-network-check           Skip the quick NuGet API connectivity check.
  --help                       Show this help message.

Example:
  ./tools/frameworks/fix-dotnet-restore.sh
  ./tools/frameworks/fix-dotnet-restore.sh --solution
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --solution)
      USE_SOLUTION=1
      shift
      ;;
    --project)
      if [[ $# -lt 2 ]]; then
        echo "--project requires a path argument" >&2
        exit 1
      fi
      TARGET="$2"
      shift 2
      ;;
    --clear-cache)
      CLEAR_CACHE=1
      shift
      ;;
    --no-network-check)
      RUN_NETWORK_CHECK=0
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if [[ "$USE_SOLUTION" -eq 1 ]]; then
  TARGET="Platform.slnx"
fi

if [[ ! -f "$REPO_ROOT/$TARGET" && "$TARGET" != *".slnx" ]]; then
  echo "Restore target not found: $REPO_ROOT/$TARGET" >&2
  exit 1
fi

if [[ "$RUN_NETWORK_CHECK" -eq 1 ]]; then
  if command -v curl >/dev/null 2>&1; then
    if ! curl -I --max-time 8 https://api.nuget.org/v3/index.json >/dev/null 2>&1; then
      echo "Warning: cannot reach https://api.nuget.org/v3/index.json right now." >&2
      echo "Restore may fail until networking is available. Continuing anyway..." >&2
    fi
  else
    echo "curl not found; skipping network pre-check." >&2
  fi
fi

export DOTNET_CLI_HOME="$REPO_ROOT/.dotnet-home"
export HOME="$REPO_ROOT/.home"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export NUGET_PACKAGES="$DOTNET_CLI_HOME/.nuget/packages"
export NUGET_HTTP_CACHE_PATH="$DOTNET_CLI_HOME/.nuget/http-cache"
export NUGET_PACKAGES_CACHE="$DOTNET_CLI_HOME/.nuget/packages"

mkdir -p "$DOTNET_CLI_HOME/.nuget/packages" "$HOME/.nuget/packages" "$DOTNET_CLI_HOME/.nuget/http-cache"

cd "$REPO_ROOT"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet command not found. Run frameworks bootstrap first: sudo tools/frameworks/install-linux-toolchain.sh" >&2
  exit 1
fi

if [[ "$CLEAR_CACHE" -eq 1 ]]; then
  echo "Clearing NuGet local cache..."
  dotnet nuget locals all --clear
fi

restore_project() {
  local attempt="$1"
  shift
  local extra_args=("$@")
  echo "Restore attempt ${attempt}: dotnet restore ${TARGET} -v ${VERBOSITY} ${extra_args[*]}"
  DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    dotnet restore "$TARGET" \
    --verbosity "$VERBOSITY" \
    --nologo \
    "${extra_args[@]}"
}

if restore_project 1; then
  echo "Dotnet restore complete."
  exit 0
fi

echo "Initial restore failed. Retrying with conservative settings..."
if restore_project 2 --no-cache --disable-parallel --force --ignore-failed-sources; then
  echo "Dotnet restore complete after retry."
  exit 0
fi

cat <<'EOF' >&2
Restore failed again.
Try:
  - Clear old SDK workload cache: dotnet workload update
  - Re-run with: ./tools/frameworks/fix-dotnet-restore.sh --clear-cache
  - Check if your environment can write to HOME/.nuget and .dotnet folders.
  - If needed, retry in a fresh shell.
EOF

exit 1
