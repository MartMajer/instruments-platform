#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
RUN_USER="${SUDO_USER:-${USER}}"

usage() {
  cat <<'USAGE'
Usage: sudo tools/frameworks/bootstrap-linux-dev.sh

Runs the full Linux bootstrap flow:
  1) Install/update local framework dependencies
  2) Generate stable command shims in tools/frameworks/bin
  3) Start PostgreSQL migration/seeding + API + web in one pass

You can also run with --help for this message.
USAGE
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run this script with sudo so dependencies can be installed:"
  echo "  sudo tools/frameworks/bootstrap-linux-dev.sh"
  exit 1
fi

printf '\nRunning installer...\n'
bash "$SCRIPT_DIR/install-linux-toolchain.sh"

printf '\nRefreshing local tool shims...\n'
bash "$SCRIPT_DIR/ensure-tool-links.sh"

printf '\nStarting local dev stack...\n'
if [[ -n "${RUN_USER}" && "${RUN_USER}" != "root" ]]; then
  sudo -u "$RUN_USER" -H bash -lc "source '$SCRIPT_DIR/activate.sh' && cd '$REPO_ROOT' && ./start-all.sh"
else
  # shellcheck disable=SC1090
  source "$SCRIPT_DIR/activate.sh"
  cd "$REPO_ROOT"
  ./start-all.sh
fi
