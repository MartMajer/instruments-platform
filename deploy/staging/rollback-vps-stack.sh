#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: bash deploy/staging/rollback-vps-stack.sh [options]

Options:
  --env-file <path>                   Compose env file. Defaults to deploy/staging/.env.
  --rollback-revision <rev>           Revision to deploy first. Defaults to HEAD~1.
  --restore-revision <rev>            Revision to restore after rollback proof. Defaults to current HEAD.
  --evidence-dir <path>               Evidence directory. Defaults to /tmp/instruments-platform-vps-rollback-<utc>.
  --require-authenticated-session     Pass fail-closed authenticated proof through to release checks.
  --session-cookie-file <path>        Ignored file containing a browser Cookie header for authenticated proof.
  --help                              Show this help.

Checks out a previous revision, redeploys it, runs VPS release checks, then
checks out the restore revision, redeploys it, and runs VPS release checks
again. Requires a clean git worktree. Does not use git reset.
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
rollback_revision=""
restore_revision=""
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
    --rollback-revision)
      [[ $# -ge 2 ]] || { echo "--rollback-revision requires a revision." >&2; exit 2; }
      rollback_revision="$2"
      shift 2
      ;;
    --restore-revision)
      [[ $# -ge 2 ]] || { echo "--restore-revision requires a revision." >&2; exit 2; }
      restore_revision="$2"
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
  echo "Rollback proof must run inside the git checkout." >&2
  exit 1
fi

git diff --quiet
git diff --cached --quiet

current_branch="$(git rev-parse --abbrev-ref HEAD)"
current_revision="$(git rev-parse HEAD)"

if [[ -z "$rollback_revision" ]]; then
  rollback_revision="$(git rev-parse HEAD~1)"
else
  rollback_revision="$(git rev-parse "$rollback_revision")"
fi

if [[ -z "$restore_revision" ]]; then
  restore_revision="$current_revision"
else
  restore_revision="$(git rev-parse "$restore_revision")"
fi

if [[ "$rollback_revision" == "$restore_revision" ]]; then
  echo "Rollback revision and restore revision must be different." >&2
  exit 1
fi

if [[ "$current_branch" == "HEAD" ]]; then
  restore_checkout_ref="$restore_revision"
else
  restore_checkout_ref="$current_branch"
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
    echo "COMPOSE_PROJECT_NAME contains unsupported characters for this rollback smoke." >&2
    exit 1
  fi

  compose_cmd+=(-p "$compose_project_name")
fi

api_origin="$(read_env_value STAGING_API_ORIGIN || read_env_value PUBLIC_API_BASE_URL || true)"
web_origin="$(read_env_value STAGING_WEB_ORIGIN || read_env_value Cors__AllowedOrigins__0 || true)"
legacy_web_origin="$(read_env_value STAGING_LEGACY_WEB_ORIGIN || true)"
if [[ -z "$api_origin" || -z "$web_origin" ]]; then
  echo "VPS rollback requires STAGING_API_ORIGIN/PUBLIC_API_BASE_URL and STAGING_WEB_ORIGIN/Cors__AllowedOrigins__0 in $env_file." >&2
  exit 1
fi

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="/tmp/instruments-platform-vps-rollback-$timestamp"
fi

mkdir -p "$evidence_dir"

release_args_for_dir() {
  local target_dir="$1"
  printf '%s\n' --evidence-dir "$target_dir"
  printf '%s\n' --api-origin "$api_origin"
  printf '%s\n' --web-origin "$web_origin"
  if [[ -n "$legacy_web_origin" ]]; then
    printf '%s\n' --legacy-web-origin "$legacy_web_origin"
  fi
  if [[ "$require_authenticated_session" == "true" ]]; then
    printf '%s\n' --require-authenticated-session
  fi
  if [[ -n "$session_cookie_file" ]]; then
    printf '%s\n' --session-cookie-file "$session_cookie_file"
  fi
}

compose_up() {
  "${compose_cmd[@]}" up -d --build
}

run_release_checks() {
  local target_dir="$1"
  mapfile -t release_args < <(release_args_for_dir "$target_dir")
  bash deploy/staging/run-vps-release-checks.sh "${release_args[@]}"
}

completed=false
cleanup() {
  if [[ "$completed" == "true" ]]; then
    return
  fi

  echo "Rollback proof did not complete; attempting to restore checkout and stack." >&2
  git checkout "$restore_checkout_ref" >/dev/null 2>&1 || git checkout --detach "$restore_revision" >/dev/null 2>&1 || true
  compose_up >/dev/null 2>&1 || true
}
trap cleanup EXIT

rollback_started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
git checkout --detach "$rollback_revision"
compose_up
run_release_checks "$evidence_dir/rollback-release-evidence"
rollback_finished_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

restore_started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
git checkout "$restore_checkout_ref"
if [[ "$(git rev-parse HEAD)" != "$restore_revision" ]]; then
  echo "Restore checkout did not land on the expected revision." >&2
  exit 1
fi
compose_up
run_release_checks "$evidence_dir/restore-release-evidence"
restore_finished_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

finished_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
restore_checkout_ref_json="$(json_escape "$restore_checkout_ref")"

cat >"$evidence_dir/rollback-evidence.json" <<JSON
{
  "schemaVersion": 1,
  "generatedAt": "$finished_at",
  "runner": "deploy/staging/rollback-vps-stack.sh",
  "status": "passed",
  "rollback": {
    "startedAt": "$rollback_started_at",
    "finishedAt": "$rollback_finished_at",
    "rollbackRevision": "$rollback_revision"
  },
  "restore": {
    "startedAt": "$restore_started_at",
    "finishedAt": "$restore_finished_at",
    "restoreRevision": "$restore_revision",
    "restoreCheckoutRef": "$restore_checkout_ref_json"
  },
  "proofScope": {
    "rollbackProven": true,
    "restoreProven": true,
    "releaseChecksAfterRollbackProven": true,
    "releaseChecksAfterRestoreProven": true,
    "legalGdprReady": false,
    "operationalNotificationEmailReady": false
  },
  "evidenceFiles": {
    "rollbackReleaseEvidenceDirectory": "rollback-release-evidence",
    "rollbackReleaseEvidence": "rollback-release-evidence/release-evidence.json",
    "restoreReleaseEvidenceDirectory": "restore-release-evidence",
    "restoreReleaseEvidence": "restore-release-evidence/release-evidence.json",
    "rollbackEvidence": "rollback-evidence.json"
  },
  "limitations": [
    "Q-053 blocks real-person production legal/GDPR/DPA claims; this is VPS staging rollback engineering proof only.",
    "Q-054 blocks outbound operational-notification email routing and related product claims.",
    "This proof uses git checkout round-tripping and Docker Compose rebuild/recreate on the current VPS; it is not GitHub Actions, registry promotion, Terraform, or managed database proof."
  ]
}
JSON

completed=true
echo "VPS rollback proof passed."
echo "Evidence directory: $evidence_dir"
echo "Rollback evidence: rollback-evidence.json"
echo "Rollback release evidence directory: rollback-release-evidence"
echo "Restore release evidence directory: restore-release-evidence"
