#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: bash deploy/staging/redeploy-vps-stack.sh [options]

Options:
  --env-file <path>                   Compose env file. Defaults to deploy/staging/.env.
  --evidence-dir <path>               Evidence directory. Defaults to /tmp/instruments-platform-vps-redeploy-<utc>.
  --require-authenticated-session     Pass fail-closed authenticated proof through to release checks.
  --session-cookie-file <path>        Ignored file containing a browser Cookie header for authenticated proof.
  --help                              Show this help.

Rebuilds/recreates the VPS staging stack with Docker Compose, then runs the VPS
release checks. The generated evidence proves redeploy plus post-redeploy
release checks. It does not prove rollback.
USAGE
}

if [[ -n "${REPO_ROOT:-}" ]]; then
  repo_root="$REPO_ROOT"
else
  repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
fi
cd "$repo_root"

env_file="deploy/staging/.env"
compose_file="deploy/staging/docker-compose.yml"
vps_compose_file="deploy/staging/docker-compose.vps.yml"
evidence_dir=""
require_authenticated_session=false
session_cookie_file=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --env-file)
      [[ $# -ge 2 ]] || { echo "--env-file requires a path." >&2; exit 2; }
      env_file="$2"
      shift 2
      ;;
    --evidence-dir)
      [[ $# -ge 2 ]] || { echo "--evidence-dir requires a path." >&2; exit 2; }
      evidence_dir="$2"
      shift 2
      ;;
    --require-authenticated-session)
      require_authenticated_session=true
      shift
      ;;
    --session-cookie-file)
      [[ $# -ge 2 ]] || { echo "--session-cookie-file requires a path." >&2; exit 2; }
      session_cookie_file="$2"
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

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "Redeploy proof must run inside the git checkout." >&2
  exit 1
fi

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

json_escape() {
  sed 's/\\/\\\\/g; s/"/\\"/g' <<<"$1" | tr -d '\n'
}

compose_cmd=(docker compose --env-file "$env_file" -f "$compose_file" -f "$vps_compose_file")
compose_project_name="$(read_env_value COMPOSE_PROJECT_NAME || true)"
if [[ -n "$compose_project_name" ]]; then
  if [[ ! "$compose_project_name" =~ ^[A-Za-z0-9][A-Za-z0-9_-]*$ ]]; then
    echo "COMPOSE_PROJECT_NAME contains unsupported characters for this redeploy smoke." >&2
    exit 1
  fi

  compose_cmd+=(-p "$compose_project_name")
fi

api_origin="$(read_env_value STAGING_API_ORIGIN || read_env_value PUBLIC_API_BASE_URL || true)"
web_origin="$(read_env_value STAGING_WEB_ORIGIN || read_env_value Cors__AllowedOrigins__0 || true)"
legacy_web_origin="$(read_env_value STAGING_LEGACY_WEB_ORIGIN || true)"
if [[ -z "$api_origin" || -z "$web_origin" ]]; then
  echo "VPS redeploy requires STAGING_API_ORIGIN/PUBLIC_API_BASE_URL and STAGING_WEB_ORIGIN/Cors__AllowedOrigins__0 in $env_file." >&2
  exit 1
fi

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="/tmp/instruments-platform-vps-redeploy-$timestamp"
fi

mkdir -p "$evidence_dir"
release_evidence_dir="$evidence_dir/release-evidence"
mkdir -p "$release_evidence_dir"

before_revision="$(git rev-parse HEAD)"
before_branch="$(git rev-parse --abbrev-ref HEAD)"

"${compose_cmd[@]}" up -d --build

after_revision="$(git rev-parse HEAD)"
after_branch="$(git rev-parse --abbrev-ref HEAD)"
if [[ "$after_revision" != "$before_revision" ]]; then
  echo "Git revision changed during redeploy; aborting evidence capture." >&2
  exit 1
fi

release_args=(--evidence-dir "$release_evidence_dir" --api-origin "$api_origin" --web-origin "$web_origin")
if [[ -n "$legacy_web_origin" ]]; then
  release_args+=(--legacy-web-origin "$legacy_web_origin")
fi
if [[ "$require_authenticated_session" == "true" ]]; then
  release_args+=(--require-authenticated-session)
fi
if [[ -n "$session_cookie_file" ]]; then
  release_args+=(--session-cookie-file "$session_cookie_file")
fi

bash deploy/staging/run-vps-release-checks.sh "${release_args[@]}"

finished_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
before_branch_json="$(json_escape "$before_branch")"
after_branch_json="$(json_escape "$after_branch")"

cat >"$evidence_dir/redeploy-evidence.json" <<JSON
{
  "schemaVersion": 1,
  "generatedAt": "$finished_at",
  "runner": "deploy/staging/redeploy-vps-stack.sh",
  "status": "passed",
  "redeploy": {
    "startedAt": "$started_at",
    "finishedAt": "$finished_at",
    "beforeRevision": "$before_revision",
    "afterRevision": "$after_revision",
    "beforeBranch": "$before_branch_json",
    "afterBranch": "$after_branch_json"
  },
  "proofScope": {
    "redeployProven": true,
    "releaseChecksProven": true,
    "rollbackProven": false,
    "legalGdprReady": false,
    "operationalNotificationEmailReady": false
  },
  "evidenceFiles": {
    "releaseEvidenceDirectory": "release-evidence",
    "releaseEvidence": "release-evidence/release-evidence.json",
    "redeployEvidence": "redeploy-evidence.json"
  },
  "limitations": [
    "Q-053 blocks real-person production legal/GDPR/DPA claims; this is VPS staging redeploy engineering proof only.",
    "Q-054 blocks outbound operational-notification email routing and related product claims.",
    "This proof rebuilds/recreates the current checkout and then runs release checks; it does not prove rollback to a previous revision."
  ]
}
JSON

echo "VPS redeploy proof passed."
echo "Evidence directory: $evidence_dir"
echo "Redeploy evidence: redeploy-evidence.json"
echo "Release evidence directory: release-evidence"
