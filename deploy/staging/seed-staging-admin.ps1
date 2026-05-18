[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[^@\s]+@[^@\s]+\.[^@\s]+$')]
    [string]$Email,

    [string]$RemoteHost = 'instruments-vps-codex',

    [string]$RemotePath = '/opt/instruments-platform',

    [ValidatePattern('^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$')]
    [string]$TenantId = '11111111-1111-4111-8111-111111111111',

    [ValidatePattern('^[a-z0-9_.-]+$')]
    [string]$RoleCode = 'staging-admin',

    [string]$RoleName = 'Staging Admin'
)

$ErrorActionPreference = 'Stop'

function ConvertTo-SqlStringLiteral {
    param([Parameter(Mandatory = $true)][string]$Value)

    return "'" + $Value.Replace("'", "''") + "'"
}

function ConvertTo-ShellSingleQuoted {
    param([Parameter(Mandatory = $true)][string]$Value)

    return "'" + $Value.Replace("'", "'""'""'") + "'"
}

if ([string]::IsNullOrWhiteSpace($RemoteHost)) {
    throw 'RemoteHost is required.'
}

if ([string]::IsNullOrWhiteSpace($RemotePath)) {
    throw 'RemotePath is required.'
}

$emailLiteral = ConvertTo-SqlStringLiteral $Email.Trim().ToLowerInvariant()
$tenantIdLiteral = ConvertTo-SqlStringLiteral $TenantId
$roleCodeLiteral = ConvertTo-SqlStringLiteral $RoleCode
$roleNameLiteral = ConvertTo-SqlStringLiteral $RoleName

$sql = @"
BEGIN;

DO `$`$
DECLARE
    v_tenant_id uuid := $tenantIdLiteral;
    v_email citext := $emailLiteral;
    v_user_id uuid;
    v_role_id uuid;
    v_setup_permission_id uuid;
    v_team_permission_id uuid;
BEGIN
    INSERT INTO permission (id, code)
    VALUES
        (gen_random_uuid(), 'setup.manage'),
        (gen_random_uuid(), 'team.manage')
    ON CONFLICT (code) DO NOTHING;

    SELECT id INTO v_setup_permission_id FROM permission WHERE code = 'setup.manage';
    SELECT id INTO v_team_permission_id FROM permission WHERE code = 'team.manage';

    INSERT INTO role (id, tenant_id, code, name)
    VALUES (gen_random_uuid(), v_tenant_id, $roleCodeLiteral, $roleNameLiteral)
    ON CONFLICT (tenant_id, code) WHERE tenant_id IS NOT NULL DO UPDATE
    SET name = EXCLUDED.name
    RETURNING id INTO v_role_id;

    IF v_role_id IS NULL THEN
        SELECT id INTO v_role_id
        FROM role
        WHERE tenant_id = v_tenant_id AND code = $roleCodeLiteral;
    END IF;

    INSERT INTO role_permission (role_id, permission_id)
    VALUES
        (v_role_id, v_setup_permission_id),
        (v_role_id, v_team_permission_id)
    ON CONFLICT DO NOTHING;

    INSERT INTO user_account (
        id,
        tenant_id,
        email,
        password_hash,
        mfa_secret,
        locale,
        email_verified_at,
        last_login_at,
        failed_login_attempts,
        locked_until,
        created_at,
        updated_at,
        deleted_at)
    VALUES (
        gen_random_uuid(),
        v_tenant_id,
        v_email,
        NULL,
        NULL,
        'en',
        now(),
        NULL,
        0,
        NULL,
        now(),
        now(),
        NULL)
    ON CONFLICT (tenant_id, email) DO UPDATE
    SET deleted_at = NULL,
        email_verified_at = COALESCE(user_account.email_verified_at, now()),
        updated_at = now()
    RETURNING id INTO v_user_id;

    INSERT INTO role_assignment (
        id,
        tenant_id,
        user_id,
        role_id,
        scope_type,
        scope_id,
        granted_at,
        granted_by)
    SELECT
        gen_random_uuid(),
        v_tenant_id,
        v_user_id,
        v_role_id,
        'tenant',
        NULL,
        now(),
        NULL
    WHERE NOT EXISTS (
        SELECT 1
        FROM role_assignment
        WHERE tenant_id = v_tenant_id
          AND user_id = v_user_id
          AND role_id = v_role_id
          AND scope_type = 'tenant'
          AND scope_id IS NULL);
END `$`$;

COMMIT;

SELECT
    (SELECT count(*) FROM user_account WHERE tenant_id = $tenantIdLiteral AND email = $emailLiteral) AS seeded_users,
    (SELECT count(*) FROM role WHERE tenant_id = $tenantIdLiteral AND code = $roleCodeLiteral) AS seeded_roles,
    (SELECT count(*) FROM permission WHERE code IN ('setup.manage', 'team.manage')) AS seeded_permissions,
    (SELECT count(*)
     FROM role_assignment ra
     JOIN user_account ua ON ua.id = ra.user_id
     JOIN role r ON r.id = ra.role_id
     WHERE ua.tenant_id = $tenantIdLiteral
       AND ua.email = $emailLiteral
       AND r.code = $roleCodeLiteral
       AND ra.scope_type = 'tenant'
       AND ra.scope_id IS NULL) AS seeded_assignments;
"@

$tempSql = Join-Path $env:TEMP ("instruments-staging-admin-{0}.sql" -f ([guid]::NewGuid().ToString('N')))
$remoteSql = "/tmp/instruments-staging-admin-$([guid]::NewGuid().ToString('N')).sql"

try {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($tempSql, $sql.Replace("`r`n", "`n"), $utf8NoBom)

    & scp $tempSql "$RemoteHost`:$remoteSql"
    if ($LASTEXITCODE -ne 0) {
        throw "scp failed with exit code $LASTEXITCODE"
    }

    $remotePathQuoted = ConvertTo-ShellSingleQuoted $RemotePath
    $remoteSqlQuoted = ConvertTo-ShellSingleQuoted $remoteSql
    $remoteScript = @"
set -euo pipefail
cd $remotePathQuoted
docker compose --env-file deploy/staging/.env -f deploy/staging/docker-compose.yml -f deploy/staging/docker-compose.vps.yml exec -T postgres sh -lc 'psql -v ON_ERROR_STOP=1 -U "`$POSTGRES_USER" -d "`$POSTGRES_DB"' < $remoteSqlQuoted
rm -f $remoteSqlQuoted
"@

    $remoteScript | ssh $RemoteHost bash -s
    if ($LASTEXITCODE -ne 0) {
        throw "remote seed failed with exit code $LASTEXITCODE"
    }
} finally {
    Remove-Item -LiteralPath $tempSql -Force -ErrorAction SilentlyContinue
}
