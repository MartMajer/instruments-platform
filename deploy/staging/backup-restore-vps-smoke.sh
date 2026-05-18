#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: bash deploy/staging/backup-restore-vps-smoke.sh [options]

Options:
  --env-file <path>       Compose env file. Defaults to deploy/staging/.env.
  --evidence-path <path>  Optional safe JSON evidence output path.
  --help                  Show this help.

Runs a VPS-target backup/restore rehearsal against the live staging Postgres
container without restoring over the application database. The script creates a
temporary custom-format dump, restores it into a temporary database in the same
Postgres container, verifies required platform tables, then drops the temporary
database and removes the dump.
USAGE
}

if [[ -n "${REPO_ROOT:-}" ]]; then
  repo_root="$REPO_ROOT"
else
  repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
fi
cd "$repo_root"

env_file="deploy/staging/.env"
evidence_path=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --env-file)
      if [[ $# -lt 2 ]]; then
        echo "--env-file requires a path." >&2
        exit 2
      fi
      env_file="$2"
      shift 2
      ;;
    --evidence-path)
      if [[ $# -lt 2 ]]; then
        echo "--evidence-path requires a path." >&2
        exit 2
      fi
      evidence_path="$2"
      shift 2
      ;;
    --help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

compose_file="deploy/staging/docker-compose.yml"
vps_compose_file="deploy/staging/docker-compose.vps.yml"

if [[ ! -f "$env_file" ]]; then
  echo "Staging env file not found: $env_file" >&2
  exit 1
fi

if [[ ! -f "$compose_file" || ! -f "$vps_compose_file" ]]; then
  echo "Staging compose files are missing." >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "docker was not found on PATH." >&2
  exit 1
fi

docker compose version >/dev/null

read_env_value() {
  local name="$1"
  local line value

  line="$(grep -E "^${name}=" "$env_file" | tail -n 1 || true)"
  if [[ -z "$line" ]]; then
    return 1
  fi

  value="${line#*=}"
  value="${value%$'\r'}"
  value="${value%\"}"
  value="${value#\"}"
  printf '%s' "$value"
}

compose_cmd=(docker compose --env-file "$env_file" -f "$compose_file" -f "$vps_compose_file")
compose_project_name="$(read_env_value COMPOSE_PROJECT_NAME || true)"
if [[ -n "$compose_project_name" ]]; then
  if [[ ! "$compose_project_name" =~ ^[A-Za-z0-9][A-Za-z0-9_-]*$ ]]; then
    echo "COMPOSE_PROJECT_NAME contains unsupported characters for this smoke." >&2
    exit 1
  fi

  compose_cmd+=(-p "$compose_project_name")
fi

work_dir="$(mktemp -d "${TMPDIR:-/tmp}/instruments-platform-vps-backup-restore.XXXXXX")"
backup_file="$work_dir/platform-staging.dump"
restore_db="platform_restore_probe_$(date -u +%Y%m%d%H%M%S)_$$"
restore_created=0

cleanup() {
  set +e
  if [[ "$restore_created" == "1" ]]; then
    "${compose_cmd[@]}" exec -T -e "RESTORE_DB=$restore_db" postgres sh -euc \
      'PGPASSWORD="$POSTGRES_PASSWORD" dropdb --if-exists -h localhost -U "$POSTGRES_USER" "$RESTORE_DB"' \
      >/dev/null 2>&1
  fi

  rm -rf "$work_dir"
}
trap cleanup EXIT

postgres_exec() {
  "${compose_cmd[@]}" exec -T postgres sh -euc "$1"
}

postgres_exec_restore_db() {
  local command="$1"
  "${compose_cmd[@]}" exec -T -e "RESTORE_DB=$restore_db" postgres sh -euc "$command"
}

trim_scalar() {
  tr -d '\r' | awk 'NF { value=$0 } END { gsub(/^[[:space:]]+|[[:space:]]+$/, "", value); print value }'
}

postgres_exec 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"' >/dev/null

"${compose_cmd[@]}" exec -T postgres sh -euc \
  'PGPASSWORD="$POSTGRES_PASSWORD" pg_dump -h localhost -Fc --no-owner --no-acl -U "$POSTGRES_USER" -d "$POSTGRES_DB"' \
  >"$backup_file"

if [[ ! -s "$backup_file" ]]; then
  echo "Backup file was not created or is empty." >&2
  exit 1
fi

postgres_exec_restore_db 'PGPASSWORD="$POSTGRES_PASSWORD" createdb -h localhost -U "$POSTGRES_USER" "$RESTORE_DB"'
restore_created=1

postgres_exec_restore_db \
  'PGPASSWORD="$POSTGRES_PASSWORD" pg_restore --clean --if-exists --no-owner --no-acl -h localhost -U "$POSTGRES_USER" -d "$RESTORE_DB"' \
  <"$backup_file"

required_tables="$(postgres_exec_restore_db "PGPASSWORD=\"\$POSTGRES_PASSWORD\" psql -X -A -t -h localhost -U \"\$POSTGRES_USER\" -d \"\$RESTORE_DB\" -c \"SELECT CASE WHEN to_regclass('public.tenant') IS NOT NULL AND to_regclass('public.audit_event') IS NOT NULL THEN 'true' ELSE 'false' END;\"" | trim_scalar)"
if [[ "$required_tables" != "true" ]]; then
  echo "Restored database is missing required platform tables." >&2
  exit 1
fi

table_count="$(postgres_exec_restore_db "PGPASSWORD=\"\$POSTGRES_PASSWORD\" psql -X -A -t -h localhost -U \"\$POSTGRES_USER\" -d \"\$RESTORE_DB\" -c \"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';\"" | trim_scalar)"
relation_count="$(postgres_exec_restore_db "PGPASSWORD=\"\$POSTGRES_PASSWORD\" psql -X -A -t -h localhost -U \"\$POSTGRES_USER\" -d \"\$RESTORE_DB\" -c \"SELECT COUNT(*) FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' AND c.relkind = 'r';\"" | trim_scalar)"

if [[ ! "$table_count" =~ ^[0-9]+$ || ! "$relation_count" =~ ^[0-9]+$ ]]; then
  echo "Restore verification counts were not numeric." >&2
  exit 1
fi

backup_bytes="$(wc -c <"$backup_file" | tr -d '[:space:]')"
backup_sha256="$(sha256sum "$backup_file" | awk '{print $1}')"
generated_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

if [[ -n "$evidence_path" ]]; then
  evidence_dir="$(dirname "$evidence_path")"
  mkdir -p "$evidence_dir"
  cat >"$evidence_path" <<JSON
{
  "schemaVersion": 1,
  "generatedAt": "$generated_at",
  "runner": "deploy/staging/backup-restore-vps-smoke.sh",
  "status": "passed",
  "backup": {
    "backupBytes": $backup_bytes,
    "backupSha256": "$backup_sha256"
  },
  "restore": {
    "restorePublicTableCount": $table_count,
    "restorePublicRelationCount": $relation_count,
    "requiredPlatformTablesPresent": true
  },
  "limitations": [
    "Q-053 blocks real-person production legal/GDPR/DPA claims; this VPS backup/restore evidence is engineering proof only.",
    "Evidence omits database names, users, passwords, connection strings, env file values, credential values, container ids, and host paths."
  ]
}
JSON
fi

echo "VPS backup/restore smoke passed."
echo "Backup bytes: $backup_bytes"
echo "Backup SHA-256: $backup_sha256"
echo "Restore public table count: $table_count"
echo "Restore public relation count: $relation_count"
echo "Required platform tables present: true"
if [[ -n "$evidence_path" ]]; then
  echo "Safe evidence written to: $evidence_path"
fi
