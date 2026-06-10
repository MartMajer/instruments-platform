#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FRAMEWORKS_DIR="$SCRIPT_DIR"
BIN_DIR="$FRAMEWORKS_DIR/bin"

mkdir -p "$BIN_DIR"

usage() {
  cat <<'USAGE'
Usage: bash tools/frameworks/ensure-tool-links.sh

Creates stable command shims in tools/frameworks/bin for the tools used by local
development startup scripts. Existing shims are overwritten.

Optional env vars:
  DOTNET_BIN
  POWERSHELL_BIN
  NODE_BIN
  NPM_BIN
  DOCKER_BIN
  DOCKER_COMPOSE_BIN
USAGE
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

warn() {
  printf '%s\n' "$*" >&2
}

resolve_tool() {
  local tool="$1"
  local override_var="${2:-}"
  local found

  if [[ -n "$override_var" ]]; then
    if [[ ! -x "$override_var" ]]; then
      warn "Override for ${tool}: $override_var is not executable. Skipping."
      return 1
    fi
    printf '%s\n' "$override_var"
    return 0
  fi

  found="$(command -v "$tool" 2>/dev/null || true)"
  if [[ -n "$found" && -x "$found" ]]; then
    printf '%s\n' "$found"
    return 0
  fi

  return 1
}

link_tool() {
  local name="$1"
  local source_path="$2"

  ln -sf "$source_path" "$BIN_DIR/$name"
  printf 'Linked %-18s -> %s\n' "$name" "$source_path"
}

link_or_warn() {
  local name="$1"
  local override_var="$2"
  local path

  if path="$(resolve_tool "$name" "$override_var")"; then
    link_tool "$name" "$path"
    return 0
  fi

  printf 'Skipping %s: not found on PATH and override not set.\n' "$name" >&2
  return 1
}

DOTNET_BIN="${DOTNET_BIN:-}"
POWERSHELL_BIN="${POWERSHELL_BIN:-}"
NODE_BIN="${NODE_BIN:-}"
NPM_BIN="${NPM_BIN:-}"
DOCKER_BIN="${DOCKER_BIN:-}"
DOCKER_COMPOSE_BIN="${DOCKER_COMPOSE_BIN:-}"

link_or_warn dotnet "$DOTNET_BIN" || true
if ! link_or_warn pwsh "$POWERSHELL_BIN"; then
  if link_or_warn powershell "$POWERSHELL_BIN"; then
    if [[ -x "$BIN_DIR/powershell" && ! -L "$BIN_DIR/pwsh" ]]; then
      ln -sf "$BIN_DIR/powershell" "$BIN_DIR/pwsh"
      printf 'Linked %-18s -> %s\n' "pwsh(alias)" "$BIN_DIR/powershell"
    fi
  fi
fi
link_or_warn node "$NODE_BIN" || true
link_or_warn npm "$NPM_BIN" || true
link_or_warn docker "$DOCKER_BIN" || true

if ! link_or_warn docker-compose "$DOCKER_COMPOSE_BIN"; then
  docker_path="$(resolve_tool docker "$DOCKER_BIN" || true)"
  if [[ -n "${docker_path:-}" ]] && "$docker_path" compose version >/dev/null 2>&1; then
    cat > "$BIN_DIR/docker-compose" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
"$SCRIPT_DIR/docker" compose "$@"
EOF
    chmod +x "$BIN_DIR/docker-compose"
    printf 'Linked %-18s -> %s\n' "docker-compose(shim)" "$docker_path compose"
  else
    printf 'Skipping docker-compose: docker compose not available.\n' >&2
  fi
fi

echo
echo "Generated tool shims in $BIN_DIR."
echo "Next: source $FRAMEWORKS_DIR/activate.sh before starting local services."
