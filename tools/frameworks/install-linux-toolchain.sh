#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

warn() {
  printf '%s\n' "$*" >&2
}

usage() {
  cat <<'USAGE'
Usage: sudo tools/frameworks/install-linux-toolchain.sh

Installs the local Linux dev toolchain for this repository:
  - dotnet SDK 9
  - node + npm (Node.js 20.x for frontend tooling)
  - docker.io + docker-compose plugin
  - PowerShell 7
  - helper framework shims at tools/frameworks/bin

Notes:
  - Run with sudo because this edits system apt sources and installs packages.
  - If sudo is not preferred, run as root.
  - You may need a fresh shell after script completion for Docker group changes.
USAGE
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

if [[ "$(id -u)" -ne 0 ]]; then
  if command -v sudo >/dev/null 2>&1; then
    warn "Run this script with sudo:"
    warn "  sudo tools/frameworks/install-linux-toolchain.sh"
  else
    warn "This script requires root privileges and sudo is not available."
  fi
  exit 1
fi

if [ ! -f /etc/os-release ]; then
  warn "Missing /etc/os-release; unable to verify this is a supported Ubuntu/Debian family OS."
  exit 1
fi

. /etc/os-release

if [[ "${ID}" != "ubuntu" && "${ID_LIKE:-}" != *"ubuntu"* && "${ID}" != "debian" && "${ID_LIKE:-}" != *"debian"* ]]; then
  warn "Unsupported base: ${ID}. This installer is written for Ubuntu/Debian-family systems."
  exit 1
fi

if [[ "${ID}" == "ubuntu" || "${ID_LIKE:-}" == *"ubuntu"* ]]; then
  DISTRO_CODENAME="${UBUNTU_CODENAME:-${VERSION_CODENAME:-}}"
  MICROSOFT_REPO_DISTRO="ubuntu"
else
  DISTRO_CODENAME="${VERSION_CODENAME:-}"
  MICROSOFT_REPO_DISTRO="debian"
fi

if [[ -z "${DISTRO_CODENAME}" && -z "${VERSION_ID:-}" ]]; then
  warn "Could not detect Ubuntu/Debian codename or version from /etc/os-release."
  exit 1
fi

APT_CACHE_DIR="$(mktemp -d)"
cleanup() {
  rm -rf "$APT_CACHE_DIR"
}
trap cleanup EXIT

install_cmd() {
  apt-get install -y "$@"
}

has_apt_package() {
  apt-cache show "$1" >/dev/null 2>&1
}

install_dotnet_via_script() {
  local target_dir="/usr/share/dotnet"
  local installer="$APT_CACHE_DIR/dotnet-install.sh"

  printf '\nInstalling .NET SDK 9.0 via dotnet-install.sh ...\n'
  curl -fsSL "https://dot.net/v1/dotnet-install.sh" -o "$installer"
  bash "$installer" --channel 9.0 --install-dir "$target_dir"

  if [[ ! -x "$target_dir/dotnet" ]]; then
    warn "dotnet-install.sh completed but dotnet binary was not placed at ${target_dir}/dotnet"
    return 1
  fi

  ln -sfn "$target_dir/dotnet" /usr/local/bin/dotnet
  printf 'Installed .NET SDK 9.0 via fallback installer in %s.\n' "$target_dir"
}

install_dotnet_apt_or_fallback() {
  if has_apt_package dotnet-sdk-9.0; then
    if install_cmd dotnet-sdk-9.0; then
      return 0
    fi
    warn "dotnet-sdk-9.0 appears unavailable from apt on this image."
  fi

  warn "dotnet-sdk-9.0 package is not available from apt repos on this platform."
  warn "Falling back to Microsoft's dotnet-install.sh script."
  install_dotnet_via_script
}

install_pkg_if_available() {
  local pkg="$1"

  if has_apt_package "$pkg"; then
    if install_cmd "$pkg"; then
      return 0
    fi
    warn "Attempt to install ${pkg} from apt failed; it may be a stale package entry in this environment."
  fi

  return 1
}

install_docker_apt_or_fallback() {
  if install_pkg_if_available docker-compose-plugin; then
    return 0
  fi

  if install_pkg_if_available docker-compose; then
    if command -v docker-compose >/dev/null 2>&1 && docker-compose --version >/dev/null 2>&1; then
      warn "Package docker-compose-plugin is unavailable; using docker-compose package fallback."
      return 0
    fi
    warn "Installed docker-compose is present but not runnable (often Python distutils mismatch on newer Ubuntu)."
  fi

  if command -v curl >/dev/null 2>&1; then
    install_docker_compose_binary
    if command -v /usr/local/bin/docker-compose >/dev/null 2>&1 && /usr/local/bin/docker-compose --version >/dev/null 2>&1; then
      return 0
    fi
    if command -v docker-compose >/dev/null 2>&1 && docker-compose --version >/dev/null 2>&1; then
      return 0
    fi
  fi

  warn "No apt package for docker compose plugin found; continuing with docker only."
  warn "If this machine already has compose support, startup scripts can still proceed when docker compose is available."
}

node_version_is_supported() {
  local version="${1#v}"
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

install_nodejs_from_nodesource() {
  local setup_script="$APT_CACHE_DIR/nodesource-setup.sh"
  local setup_url="https://deb.nodesource.com/setup_20.x"

  printf '\nInstalling NodeSource apt repo for Node.js 20.x ...\n'
  if ! curl -fsSL "$setup_url" -o "$setup_script"; then
    warn "Failed to download NodeSource setup script from $setup_url."
    return 1
  fi

  if ! bash "$setup_script"; then
    warn "NodeSource setup script failed."
    return 1
  fi

  apt-get update
  if command -v npm >/dev/null 2>&1; then
    warn "Removing existing distro npm package to avoid NodeSource package conflicts."
    apt-get remove -y npm || true
  fi

  apt-mark unhold nodejs npm >/dev/null 2>&1 || true

  if ! apt-get install -y --allow-change-held-packages nodejs; then
    warn "apt could not install NodeSource nodejs package (possible dependency/held package conflict)."
    warn "Attempting best-effort broken-package recovery."
    apt-get install -y -f || true
    return 1
  fi

  if ! command -v node >/dev/null 2>&1; then
    warn "Node.js still not found after NodeSource install."
    return 1
  fi

  if node_version_is_supported "$(node --version)"; then
    printf '\nNode.js %s installed successfully via NodeSource.\n' "$(node --version)"
    return 0
  fi

  warn "NodeSource install completed, but Node.js version is still too old."
  return 1
}

ensure_nodejs_runtime() {
  if command -v node >/dev/null 2>&1; then
    local detected_node
    detected_node="$(node --version)"
    if node_version_is_supported "$detected_node"; then
      return 0
    fi
    warn "Detected Node.js $detected_node which is below this repo minimum."
    warn "Attempting Node.js 20.x upgrade from NodeSource."
  fi

  if ! install_nodejs_from_nodesource; then
    return 1
  fi
}

install_docker_compose_binary() {
  local architecture
  local compose_arch
  local release_json="$APT_CACHE_DIR/docker-compose-release.json"
  local compose_version=""
  local compose_url

  architecture="$(uname -m)"
  case "$architecture" in
    x86_64) compose_arch="x86_64" ;;
    aarch64|arm64) compose_arch="aarch64" ;;
    *)
      warn "Unsupported CPU architecture for compose binary fallback: ${architecture}"
      return 1
      ;;
  esac

  if [[ -n "${DOCKER_COMPOSE_VERSION:-}" ]]; then
    compose_version="${DOCKER_COMPOSE_VERSION#v}"
  fi

  if [[ -z "$compose_version" ]] && curl -fsSL -H "Accept: application/vnd.github+json" \
    "https://api.github.com/repos/docker/compose/releases/latest" -o "$release_json"; then
    compose_version="$(grep -Eo '\"tag_name\"[[:space:]]*:[[:space:]]*\"v[0-9][^\"]*\"' "$release_json" | head -n 1 | sed -E 's/.*\"v([0-9][^\"]*)\"/\1/' || true)"
  fi

  if [[ -z "$compose_version" ]] && command -v curl >/dev/null 2>&1; then
    local redirect_url
    redirect_url="$(curl -Ls -o /dev/null -w '%{url_effective}' https://github.com/docker/compose/releases/latest || true)"
    compose_version="$(printf '%s\n' "$redirect_url" | sed -E 's#^.*/v##' || true)"
  fi

  if [[ -n "$compose_version" ]]; then
    compose_url="https://github.com/docker/compose/releases/download/v${compose_version}/docker-compose-linux-${compose_arch}"
  else
    compose_url="https://github.com/docker/compose/releases/latest/download/docker-compose-linux-${compose_arch}"
  fi

  if ! curl -fsSL "$compose_url" -o /usr/local/bin/docker-compose; then
    warn "Failed to download Docker Compose from ${compose_url}."
    return 1
  fi

  chmod 0755 /usr/local/bin/docker-compose
  if /usr/local/bin/docker-compose --version >/dev/null 2>&1; then
    if [[ -n "$compose_version" ]]; then
      printf 'Installed docker-compose v%s to /usr/local/bin/docker-compose.\n' "$compose_version"
    else
      printf 'Installed docker-compose (latest tag) to /usr/local/bin/docker-compose.\n'
    fi
    return 0
  fi

  warn "Downloaded docker-compose binary did not execute."
  return 1
}

printf 'Detected distro: %s (%s)\n' "${NAME}" "${DISTRO_CODENAME}"

dotnet_repo_path="$APT_CACHE_DIR/packages-microsoft-prod.deb"

declare -a MS_REPO_CANDIDATES=()
if [[ "$MICROSOFT_REPO_DISTRO" == "ubuntu" ]]; then
  if [[ -n "${VERSION_ID:-}" ]]; then
    MS_REPO_CANDIDATES+=("${VERSION_ID}")
  fi

  if [[ -n "${DISTRO_CODENAME:-}" ]]; then
    MS_REPO_CANDIDATES+=("${DISTRO_CODENAME}")
  fi

  # Fallback to known Ubuntu LTS repos in case new codenames are not yet on mirror.
  if [[ "${VERSION_ID}" != "22.04" ]]; then
    MS_REPO_CANDIDATES+=("22.04")
  fi
  MS_REPO_CANDIDATES+=("jammy")
else
  if [[ -n "${VERSION_ID:-}" ]]; then
    MS_REPO_CANDIDATES+=("${VERSION_ID}")
  fi

  if [[ -n "${DISTRO_CODENAME:-}" ]]; then
    MS_REPO_CANDIDATES+=("${DISTRO_CODENAME}")
  fi

  if [[ "${VERSION_ID}" != "12" ]]; then
    MS_REPO_CANDIDATES+=("12")
  fi
fi

if [[ "${#MS_REPO_CANDIDATES[@]}" -eq 0 ]]; then
  warn "Could not generate any Microsoft repository fallback candidate."
  exit 1
fi

declare -A MS_REPO_SEEN=()
MS_REPO_URL=""
MS_REPO_BASE="https://packages.microsoft.com/config"

for candidate in "${MS_REPO_CANDIDATES[@]}"; do
  if [[ -z "${candidate}" ]]; then
    continue
  fi

  if [[ -n "${MS_REPO_SEEN[$candidate]:-}" ]]; then
    continue
  fi
  MS_REPO_SEEN["$candidate"]=1

  candidate_url="${MS_REPO_BASE}/${MICROSOFT_REPO_DISTRO}/${candidate}/packages-microsoft-prod.deb"
  printf '\nTrying Microsoft repository URL: %s\n' "$candidate_url"

  if curl -fsSL "$candidate_url" -o "$dotnet_repo_path"; then
    MS_REPO_URL="$candidate_url"
    break
  fi
done

if [[ -z "$MS_REPO_URL" ]]; then
  warn "Could not download packages-microsoft-prod.deb from any known URL."
  warn "Checked candidates: ${MS_REPO_CANDIDATES[*]}"
  exit 1
fi

printf 'Using Microsoft repository URL: %s\n' "$MS_REPO_URL"
printf '\nInstalling package prerequisites...\n'
apt-get update
install_cmd ca-certificates curl gnupg apt-transport-https software-properties-common

printf '\nInstalling Microsoft repository package (for dotnet-sdk-9.0 and powershell)...\n'
dpkg -i "$dotnet_repo_path"

printf '\nUpdating package lists after repo change...\n'
apt-get update

printf '\nInstalling runtime/tooling packages...\n'
install_dotnet_apt_or_fallback
install_cmd powershell
ensure_nodejs_runtime
install_cmd docker.io
install_docker_apt_or_fallback

printf '\nVerifying command availability...\n'
for cmd in dotnet node npm docker pwsh; do
  if ! command -v "$cmd" >/dev/null 2>&1; then
    warn "Expected command not found after install: $cmd"
  fi
done

if command -v docker-compose >/dev/null 2>&1; then
  if docker-compose --version >/dev/null 2>&1; then
    :
  else
    warn "docker-compose is installed but not runnable (`docker-compose --version` failed)."
  fi
fi

printf '\nConfiguring Docker group for the non-root user (%s) if present...\n' "$SUDO_USER"
TARGET_USER="${SUDO_USER:-${USER}}"
if [[ -n "$TARGET_USER" && "$TARGET_USER" != "root" ]]; then
  usermod -aG docker "$TARGET_USER"
  printf 'Added %s to docker group.\n' "$TARGET_USER"
else
  warn "Could not determine a non-root user for docker group assignment; skipping."
fi

printf '\nRegistering stable local command shims in tools/frameworks/bin...\n'
DOCKER_COMPOSE_BIN_PATH=""
if [ -x /usr/local/bin/docker-compose ] && /usr/local/bin/docker-compose --version >/dev/null 2>&1; then
  DOCKER_COMPOSE_BIN_PATH="/usr/local/bin/docker-compose"
fi

if [[ "$TARGET_USER" != "root" ]]; then
  if command -v sudo >/dev/null 2>&1; then
    sudo -u "$TARGET_USER" \
      DOTNET_BIN="$(command -v dotnet)" \
      POWERSHELL_BIN="$(command -v pwsh)" \
      NODE_BIN="$(command -v node)" \
      NPM_BIN="$(command -v npm)" \
      DOCKER_BIN="$(command -v docker)" \
      DOCKER_COMPOSE_BIN="$DOCKER_COMPOSE_BIN_PATH" \
      bash "$SCRIPT_DIR/ensure-tool-links.sh"
  else
    bash "$SCRIPT_DIR/ensure-tool-links.sh"
  fi
else
  DOCKER_COMPOSE_BIN="$DOCKER_COMPOSE_BIN_PATH" \
    DOTNET_BIN="$(command -v dotnet)" \
    POWERSHELL_BIN="$(command -v pwsh)" \
    NODE_BIN="$(command -v node)" \
    NPM_BIN="$(command -v npm)" \
    DOCKER_BIN="$(command -v docker)" \
    bash "$SCRIPT_DIR/ensure-tool-links.sh"
fi

cat <<EOF

Install complete.

If you already open a shell, run:

    source tools/frameworks/activate.sh
    ./start-all.sh

If this is your first time running Docker in this shell session:
  - close/reopen terminal, or run:
  - newgrp docker
EOF
