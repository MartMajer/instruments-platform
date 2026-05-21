#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: bash deploy/staging/run-vps-release-checks.sh [options]

Options:
  --api-origin <url>                 API origin. Defaults to validatedscale API staging.
  --web-origin <url>                 Web origin. Defaults to validatedscale web staging.
  --tenant-id <id>                   Tenant id for tenant-scoped auth probes.
  --evidence-dir <path>              Evidence directory. Defaults to /tmp/instruments-platform-vps-release-evidence-<utc>.
  --session-cookie-file <path>       Ignored file containing a browser Cookie header for authenticated proof.
  --require-authenticated-session    Fail if no authenticated session cookie is supplied.
  --help                             Show this help.

STAGING_SESSION_COOKIE may be used instead of --session-cookie-file. Cookies are
never printed or written to release evidence.
USAGE
}

if [[ -n "${REPO_ROOT:-}" ]]; then
  repo_root="$REPO_ROOT"
else
  repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
fi
cd "$repo_root"

api_origin="https://validatedscale-api-staging.croat.dev"
web_origin="https://validatedscale-staging.croat.dev"
tenant_id="11111111-1111-4111-8111-111111111111"
evidence_dir=""
session_cookie_file=""
require_authenticated_session=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --api-origin)
      [[ $# -ge 2 ]] || { echo "--api-origin requires a URL." >&2; exit 2; }
      api_origin="$2"
      shift 2
      ;;
    --web-origin)
      [[ $# -ge 2 ]] || { echo "--web-origin requires a URL." >&2; exit 2; }
      web_origin="$2"
      shift 2
      ;;
    --tenant-id)
      [[ $# -ge 2 ]] || { echo "--tenant-id requires an id." >&2; exit 2; }
      tenant_id="$2"
      shift 2
      ;;
    --evidence-dir)
      [[ $# -ge 2 ]] || { echo "--evidence-dir requires a path." >&2; exit 2; }
      evidence_dir="$2"
      shift 2
      ;;
    --session-cookie-file)
      [[ $# -ge 2 ]] || { echo "--session-cookie-file requires a path." >&2; exit 2; }
      session_cookie_file="$2"
      shift 2
      ;;
    --require-authenticated-session)
      require_authenticated_session=true
      shift
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

api_origin="${api_origin%/}"
web_origin="${web_origin%/}"

if [[ -n "$session_cookie_file" && -n "${STAGING_SESSION_COOKIE:-}" ]]; then
  echo "Use either --session-cookie-file or STAGING_SESSION_COOKIE, not both." >&2
  exit 2
fi

if [[ -n "$session_cookie_file" ]]; then
  if [[ ! -f "$session_cookie_file" ]]; then
    echo "Session cookie file not found." >&2
    exit 1
  fi
  session_cookie="$(tr -d '\r\n' < "$session_cookie_file")"
elif [[ -n "${STAGING_SESSION_COOKIE:-}" ]]; then
  session_cookie="${STAGING_SESSION_COOKIE}"
else
  session_cookie=""
fi

if [[ -z "$session_cookie" && "$require_authenticated_session" == "true" ]]; then
  echo "Authenticated session proof required, but no session cookie was supplied." >&2
  echo "Supply --session-cookie-file <ignored-file> or STAGING_SESSION_COOKIE." >&2
  exit 1
fi

if ! command -v curl >/dev/null 2>&1; then
  echo "curl was not found on PATH." >&2
  exit 1
fi

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
generated_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

if [[ -z "$evidence_dir" ]]; then
  evidence_dir="/tmp/instruments-platform-vps-release-evidence-$timestamp"
fi

mkdir -p "$evidence_dir"
work_dir="$(mktemp -d "${TMPDIR:-/tmp}/instruments-platform-vps-release-checks.XXXXXX")"

cleanup() {
  rm -rf "$work_dir"
}
trap cleanup EXIT

fail() {
  echo "$1" >&2
  exit 1
}

require_status() {
  local name="$1"
  local expected="$2"
  local actual="$3"

  if [[ "$actual" != "$expected" ]]; then
    fail "$name returned HTTP $actual; expected $expected."
  fi
}

wait_for_status() {
  local name="$1"
  local expected="$2"
  local url="$3"
  local attempts="$4"
  local delay_seconds="$5"
  local actual=""

  for ((attempt = 1; attempt <= attempts; attempt++)); do
    actual="$(curl -sS -o /dev/null -w '%{http_code}' "$url" || true)"
    if [[ "$actual" == "$expected" ]]; then
      printf '%s' "$actual"
      return 0
    fi

    if [[ "$attempt" -lt "$attempts" ]]; then
      sleep "$delay_seconds"
    fi
  done

  fail "$name returned HTTP $actual after $attempts attempts; expected $expected."
}

json_escape() {
  sed 's/\\/\\\\/g; s/"/\\"/g' <<<"$1" | tr -d '\n'
}

remote_evidence_path="$evidence_dir/remote-public-smoke.json"
backup_restore_evidence_path="$evidence_dir/backup-restore.json"
release_evidence_path="$evidence_dir/release-evidence.json"

api_health_status="$(wait_for_status "API health" "200" "$api_origin/health" 30 2)"

web_root_status="$(wait_for_status "Web root" "200" "$web_origin/" 15 2)"

unauthenticated_session_status="$(curl -sS -o "$work_dir/unauthenticated-session.body" -w '%{http_code}' -H "Origin: $web_origin" -H "X-Tenant-Id: $tenant_id" "$api_origin/auth/session")"
require_status "Unauthenticated session" "401" "$unauthenticated_session_status"
rm -f "$work_dir/unauthenticated-session.body"

cors_headers="$work_dir/auth-session-cors.headers"
auth_session_cors_preflight_status="$(curl -sS -D "$cors_headers" -o /dev/null -w '%{http_code}' -X OPTIONS "$api_origin/auth/session" -H "Origin: $web_origin" -H "Access-Control-Request-Method: GET")"
require_status "Auth session CORS preflight" "204" "$auth_session_cors_preflight_status"
if ! tr -d '\r' < "$cors_headers" | grep -qi "^access-control-allow-origin: $web_origin"; then
  fail "Auth session CORS preflight did not allow the staging web origin."
fi

login_headers="$work_dir/auth-login.headers"
auth_login_redirect_status="$(curl -sS -D "$login_headers" -o /dev/null -w '%{http_code}' "$api_origin/auth/login?returnUrl=%2Fapp&tenantId=$tenant_id")"
if [[ "$auth_login_redirect_status" != "302" && "$auth_login_redirect_status" != "303" ]]; then
  fail "Auth login returned HTTP $auth_login_redirect_status; expected 302 or 303."
fi
if ! tr -d '\r' < "$login_headers" | grep -qi '^location: '; then
  fail "Auth login did not return a Location header."
fi

authenticated_remote_smoke_proven=false
authenticated_remote_smoke_status="skipped"
if [[ -n "$session_cookie" ]]; then
  authenticated_session_body="$work_dir/authenticated-session.body"
  authenticated_session_status="$(curl -sS -o "$authenticated_session_body" -w '%{http_code}' -H "Origin: $web_origin" -H "X-Tenant-Id: $tenant_id" -H "Cookie: $session_cookie" "$api_origin/auth/session")"
  require_status "Authenticated session" "200" "$authenticated_session_status"
  if ! grep -q "setup.manage" "$authenticated_session_body"; then
    fail "Authenticated session did not include setup.manage permission."
  fi
  rm -f "$authenticated_session_body"
  authenticated_remote_smoke_proven=true
  authenticated_remote_smoke_status="passed"
fi

api_origin_json="$(json_escape "$api_origin")"
web_origin_json="$(json_escape "$web_origin")"

cat >"$remote_evidence_path" <<JSON
{
  "schemaVersion": 1,
  "generatedAt": "$generated_at",
  "runner": "deploy/staging/run-vps-release-checks.sh",
  "status": "passed",
  "remote": {
    "apiOrigin": "$api_origin_json",
    "webOrigin": "$web_origin_json",
    "apiHealthStatus": $api_health_status,
    "webRootStatus": $web_root_status,
    "unauthenticatedSessionStatus": $unauthenticated_session_status,
    "authSessionCorsPreflightStatus": $auth_session_cors_preflight_status,
    "authLoginRedirectStatus": $auth_login_redirect_status,
    "authenticatedSession": {
      "status": "$authenticated_remote_smoke_status",
      "proven": $authenticated_remote_smoke_proven
    }
  },
  "limitations": [
    "Q-053 blocks real-person production legal/GDPR/DPA claims; this remote smoke evidence is owner-controlled staging engineering proof only.",
    "Q-054 blocks outbound operational-notification email routing and any claim that operational events are emailed.",
    "Evidence omits cookies, session bodies, response bodies, raw headers, email addresses, tenant ids, credential values, and connection strings."
  ]
}
JSON

bash deploy/staging/backup-restore-vps-smoke.sh --evidence-path "$backup_restore_evidence_path"

cat >"$release_evidence_path" <<JSON
{
  "schemaVersion": 1,
  "generatedAt": "$generated_at",
  "runner": "deploy/staging/run-vps-release-checks.sh",
  "status": "passed",
  "gates": [
    {
      "name": "remote-public-smoke",
      "status": "passed",
      "evidenceFile": "remote-public-smoke.json"
    },
    {
      "name": "vps-backup-restore-smoke",
      "status": "passed",
      "evidenceFile": "backup-restore.json"
    },
    {
      "name": "authenticated-remote-smoke",
      "status": "$authenticated_remote_smoke_status",
      "evidenceFile": "remote-public-smoke.json"
    }
  ],
  "proofScope": {
    "remotePublicSmokeProven": true,
    "vpsBackupRestoreProven": true,
    "authenticatedRemoteSmokeProven": $authenticated_remote_smoke_proven,
    "legalGdprReady": false,
    "operationalNotificationEmailReady": false
  },
  "evidenceFiles": {
    "remotePublicSmoke": "remote-public-smoke.json",
    "backupRestore": "backup-restore.json",
    "releaseEvidence": "release-evidence.json"
  },
  "limitations": [
    "Q-053 blocks real-person production legal/GDPR/DPA claims; this is staging engineering release evidence only.",
    "Q-054 blocks outbound operational-notification email routing and related product claims.",
    "Authenticated remote smoke remains false unless an owner supplies a current browser session cookie through --session-cookie-file or STAGING_SESSION_COOKIE."
  ]
}
JSON

echo "VPS release checks passed."
echo "Evidence directory: $evidence_dir"
echo "Remote public smoke evidence: remote-public-smoke.json"
echo "Backup/restore evidence: backup-restore.json"
echo "Release evidence: release-evidence.json"
echo "Authenticated remote smoke: $authenticated_remote_smoke_status"
