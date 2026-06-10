#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: ./setup-vps-ssh.sh [options]

Options:
  --alias <host-alias>          SSH host alias (default: instruments-vps-codex)
  --host <hostname-or-ip>       SSH host (default: srv713203.hstgr.cloud)
  --user <user>                 SSH user (default: codex)
  --port <port>                 SSH port (default: 23146)
  --source-dir <path>           Folder containing exported Windows keys
                               (default: /media/$USER/Media/BACKUP_SSH,
                                then ~/Documents/Backups/BACKUP_SSH)
  --key-name <name>             Private key file name in source-dir
                               (default: instruments_codex_ed25519)
  --source-key <path>           Full path to the private key. When set,
                                overrides --source-dir + --key-name.
  --test                        Run a connection probe after setup
  -h, --help                   Show this help
USAGE
}

ALIAS="instruments-vps-codex"
HOST="srv713203.hstgr.cloud"
USER="codex"
PORT="23146"
SOURCE_DIR="/media/martin/Media/BACKUP_SSH"
KEY_NAME="instruments_codex_ed25519"
SOURCE_KEY=""
SOURCE_KEY_EXPLICIT=0
SOURCE_DIR_EXPLICIT=0
RUN_TEST=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --alias) ALIAS="$2"; shift 2 ;;
    --host) HOST="$2"; shift 2 ;;
    --user) USER="$2"; shift 2 ;;
    --port) PORT="$2"; shift 2 ;;
    --source-dir) SOURCE_DIR="$2"; SOURCE_DIR_EXPLICIT=1; shift 2 ;;
    --key-name) KEY_NAME="$2"; shift 2 ;;
    --source-key) SOURCE_KEY="$2"; SOURCE_KEY_EXPLICIT=1; shift 2 ;;
    --test) RUN_TEST=1; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1"; usage; exit 1 ;;
  esac
done

if [[ "$SOURCE_DIR_EXPLICIT" -ne 1 ]]; then
  for fallback_source_dir in \
    "/media/$USER/Media/BACKUP_SSH" \
    "$HOME/Documents/Backups/BACKUP_SSH" \
    "$HOME/Documents/Backups"; do
    if [[ -d "$fallback_source_dir" ]]; then
      SOURCE_DIR="$fallback_source_dir"
      break
    fi
  done
fi

if [[ -z "$SOURCE_KEY" ]]; then
  SOURCE_KEY="$SOURCE_DIR/$KEY_NAME"
fi

DEST_DIR="$HOME/.ssh/instruments-platform"
DEST_KEY="$DEST_DIR/$KEY_NAME"
DEST_KEY_PUB="$DEST_DIR/$KEY_NAME.pub"

resolve_source_key() {
  local candidates=()

  if [[ "$SOURCE_KEY_EXPLICIT" -eq 1 ]]; then
    candidates+=("$SOURCE_KEY")
    if [[ ! -f "${candidates[0]}" ]]; then
      return 1
    fi
    SOURCE_KEY="${candidates[0]}"
    return 0
  fi

  if [[ -n "$SOURCE_DIR" && -d "$SOURCE_DIR" ]]; then
    candidates+=("$SOURCE_DIR/$KEY_NAME")
  fi

  # Common copy locations if the key is already there.
  candidates+=(
    "$HOME/.ssh/instruments-platform/$KEY_NAME"
    "$HOME/.ssh/$KEY_NAME"
  )

  # Extra fallback: look for removable media copies
  while IFS= read -r key_path; do
    candidates+=("$key_path")
  done < <(find /media -type f -name "$KEY_NAME" 2>/dev/null)

  # Some people mount into /mnt instead of /media.
  while IFS= read -r key_path; do
    candidates+=("$key_path")
  done < <(find /mnt -type f -name "$KEY_NAME" 2>/dev/null)

  for candidate in "${candidates[@]}"; do
    if [[ -n "${candidate}" && -f "$candidate" ]]; then
      SOURCE_KEY="$candidate"
      return 0
    fi
  done

  return 1
}

if ! resolve_source_key; then
  if [[ "$SOURCE_KEY_EXPLICIT" -eq 1 ]]; then
    echo "Could not find private key at --source-key: $SOURCE_KEY" >&2
    echo "Tip: check the path and rerun with an absolute path to the private key file." >&2
    exit 1
  fi

  echo "Could not find private key '$KEY_NAME'." >&2
  echo "Checked:"
  echo "  - explicit --source-key value" >&2
  echo "  - $SOURCE_DIR/$KEY_NAME (if dir exists)" >&2
  echo "  - $HOME/.ssh/instruments-platform/$KEY_NAME" >&2
  echo "  - $HOME/.ssh/$KEY_NAME" >&2
  echo "  - /media/* for '$KEY_NAME'" >&2
  echo "  - /home/$USER/Documents/Backups/* for '$KEY_NAME'" >&2
  echo "Tip: run: find /media /mnt "$HOME" -type f -name 'instruments_codex_ed25519*'" >&2
  exit 1
fi

if [[ ! -f "$SOURCE_KEY" ]]; then
  echo "Private key not found: $SOURCE_KEY" >&2
  exit 1
fi

echo "Using private key: $SOURCE_KEY"

SOURCE_KEY_PUB="$SOURCE_KEY.pub"

mkdir -p "$DEST_DIR"
mkdir -p "$HOME/.ssh"
chmod 700 "$HOME/.ssh" "$DEST_DIR"

if [[ "$SOURCE_KEY" != "$DEST_KEY" ]]; then
  cp "$SOURCE_KEY" "$DEST_KEY"
else
  chmod 600 "$DEST_KEY"
fi
chmod 600 "$DEST_KEY"

if [[ -f "$SOURCE_KEY_PUB" ]]; then
  if [[ "$SOURCE_KEY_PUB" != "$DEST_KEY_PUB" ]]; then
    cp "$SOURCE_KEY_PUB" "$DEST_KEY_PUB"
  fi
  chmod 644 "$DEST_KEY_PUB"
fi

CFG="$HOME/.ssh/config"
TMP_CFG="$CFG.tmp.$$"

if [[ -f "$CFG" ]]; then
  cp "$CFG" "$CFG.bak.$(date +%Y%m%d%H%M%S)"
fi

if [[ -f "$CFG" ]]; then
  awk -v host="$ALIAS" '
    function has_host_token(line, host, n, i, token, tmp) {
      n = split(line, tmp, /[[:space:]]+/)
      for (i = 2; i <= n; i++) {
        if (tmp[i] == host) {
          return 1
        }
      }
      return 0
    }

    $1 == "Host" && has_host_token($0, host) {in_host=1; next}
    in_host && $1 == "Host" {in_host=0}
    !in_host {print}
  ' "$CFG" > "$TMP_CFG"

  # Remove trailing duplicates of blank lines from removal pass
  sed -i '/^$/N;/^\n$/D' "$TMP_CFG"
else
  : > "$TMP_CFG"
fi

cat >> "$TMP_CFG" <<EOF

# Added by setup-vps-ssh.sh
Host ${ALIAS}
  HostName ${HOST}
  User ${USER}
  Port ${PORT}
  IdentityFile ${DEST_KEY}
  IdentitiesOnly yes
  PubkeyAuthentication yes
  AddKeysToAgent yes
  ServerAliveInterval 20
  ServerAliveCountMax 6
EOF

mv "$TMP_CFG" "$CFG"
chmod 600 "$CFG"

echo "Wrote SSH alias '$ALIAS' -> ${HOST}:${PORT} using ${DEST_KEY}"

echo "Current merged host entry:"
grep -nE "^Host[[:space:]]+$ALIAS|^\s{2}(HostName|Port|User|IdentityFile|IdentitiesOnly|PubkeyAuthentication|AddKeysToAgent|ServerAliveInterval|ServerAliveCountMax)" "$CFG" | sed -n "1,20p"

if [[ "$RUN_TEST" -eq 1 ]]; then
  echo "Running SSH test..."
  ssh -o ConnectTimeout=12 -G "$ALIAS" | grep -E 'hostname|port|user|identityfile'
  ssh -o ConnectTimeout=12 -o StrictHostKeyChecking=accept-new "$ALIAS" "hostname; echo ok" || true

  echo "Direct host test (in case alias resolution still points elsewhere):"
  ssh -o ConnectTimeout=12 -p "$PORT" -i "$DEST_KEY" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new "${USER}@${HOST}" "hostname; echo ok" || true
fi

echo "Done."
