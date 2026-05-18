param(
    [string]$UsersFile,
    [string]$EnvFile,
    [switch]$ValidateOnly,
    [switch]$SkipTenantBootstrap,
    [switch]$AllowPlaceholderEmails
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$composeFile = Join-Path $repoRoot 'deploy\staging\docker-compose.yml'
$fixtureDir = Join-Path $PSScriptRoot 'validation-demo-fixtures'
$tenantBootstrapFile = Join-Path $fixtureDir 'tenant-bootstrap.sql'
$exampleUsersFile = Join-Path $fixtureDir 'validation-demo-auth-users.example.json'
$localUsersFile = Join-Path $fixtureDir 'validation-demo-auth-users.local.json'

if (-not $UsersFile) {
    $UsersFile = $localUsersFile
}

if (-not $EnvFile) {
    $EnvFile = Join-Path $repoRoot 'deploy\staging\.env'
}

$expectedRoles = @('tenant_owner', 'researcher', 'analyst', 'viewer')
$expectedTenantSlugs = @(
    'validation-oh-research',
    'validation-se-education',
    'validation-osh-consulting'
)

$tenantDefinitions = @{
    'validation-oh-research' = @{
        Id = '33333333-3333-4333-8333-333333333333'
        Roles = @{
            'tenant_owner' = @{
                RoleId = '71000000-0000-7000-8000-000000000001'
                UserId = '72000000-0000-7000-8000-000000000001'
                AssignmentId = '73000000-0000-7000-8000-000000000001'
                Name = 'Tenant Owner'
                Permissions = @('setup.manage', 'team.manage')
            }
            'researcher' = @{
                RoleId = '71000000-0000-7000-8000-000000000002'
                UserId = '72000000-0000-7000-8000-000000000002'
                AssignmentId = '73000000-0000-7000-8000-000000000002'
                Name = 'Researcher'
                Permissions = @('setup.manage')
            }
            'analyst' = @{
                RoleId = '71000000-0000-7000-8000-000000000003'
                UserId = '72000000-0000-7000-8000-000000000003'
                AssignmentId = '73000000-0000-7000-8000-000000000003'
                Name = 'Analyst'
                Permissions = @()
            }
            'viewer' = @{
                RoleId = '71000000-0000-7000-8000-000000000004'
                UserId = '72000000-0000-7000-8000-000000000004'
                AssignmentId = '73000000-0000-7000-8000-000000000004'
                Name = 'Viewer'
                Permissions = @()
            }
        }
    }
    'validation-se-education' = @{
        Id = '44444444-4444-4444-8444-444444444444'
        Roles = @{
            'tenant_owner' = @{
                RoleId = '74000000-0000-7000-8000-000000000001'
                UserId = '75000000-0000-7000-8000-000000000001'
                AssignmentId = '76000000-0000-7000-8000-000000000001'
                Name = 'Tenant Owner'
                Permissions = @('setup.manage', 'team.manage')
            }
            'researcher' = @{
                RoleId = '74000000-0000-7000-8000-000000000002'
                UserId = '75000000-0000-7000-8000-000000000002'
                AssignmentId = '76000000-0000-7000-8000-000000000002'
                Name = 'Researcher'
                Permissions = @('setup.manage')
            }
            'analyst' = @{
                RoleId = '74000000-0000-7000-8000-000000000003'
                UserId = '75000000-0000-7000-8000-000000000003'
                AssignmentId = '76000000-0000-7000-8000-000000000003'
                Name = 'Analyst'
                Permissions = @()
            }
            'viewer' = @{
                RoleId = '74000000-0000-7000-8000-000000000004'
                UserId = '75000000-0000-7000-8000-000000000004'
                AssignmentId = '76000000-0000-7000-8000-000000000004'
                Name = 'Viewer'
                Permissions = @()
            }
        }
    }
    'validation-osh-consulting' = @{
        Id = '55555555-5555-4555-8555-555555555555'
        Roles = @{
            'tenant_owner' = @{
                RoleId = '77000000-0000-7000-8000-000000000001'
                UserId = '78000000-0000-7000-8000-000000000001'
                AssignmentId = '79000000-0000-7000-8000-000000000001'
                Name = 'Tenant Owner'
                Permissions = @('setup.manage', 'team.manage')
            }
            'researcher' = @{
                RoleId = '77000000-0000-7000-8000-000000000002'
                UserId = '78000000-0000-7000-8000-000000000002'
                AssignmentId = '79000000-0000-7000-8000-000000000002'
                Name = 'Researcher'
                Permissions = @('setup.manage')
            }
            'analyst' = @{
                RoleId = '77000000-0000-7000-8000-000000000003'
                UserId = '78000000-0000-7000-8000-000000000003'
                AssignmentId = '79000000-0000-7000-8000-000000000003'
                Name = 'Analyst'
                Permissions = @()
            }
            'viewer' = @{
                RoleId = '77000000-0000-7000-8000-000000000004'
                UserId = '78000000-0000-7000-8000-000000000004'
                AssignmentId = '79000000-0000-7000-8000-000000000004'
                Name = 'Viewer'
                Permissions = @()
            }
        }
    }
}

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-DockerCommand {
    $docker = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $docker) {
        $dockerBin = 'C:\Program Files\Docker\Docker\resources\bin'
        if (Test-Path (Join-Path $dockerBin 'docker.exe')) {
            $env:PATH = "$dockerBin;$env:PATH"
        }

        $docker = Get-Command docker -ErrorAction SilentlyContinue
    }

    if (-not $docker) {
        throw 'docker.exe was not found. Install Docker or add Docker to PATH before running validation auth bootstrap.'
    }

    return $docker.Source
}

function Read-EnvFile {
    param([string]$Path)

    $values = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#')) {
            continue
        }

        $parts = $trimmed.Split('=', 2)
        if ($parts.Length -eq 2) {
            $values[$parts[0]] = $parts[1].Trim('"')
        }
    }

    return $values
}

function Get-EnvValue {
    param(
        [hashtable]$Values,
        [string]$Name,
        [string]$Default
    )

    if ($Values.ContainsKey($Name) -and -not [string]::IsNullOrWhiteSpace($Values[$Name])) {
        return $Values[$Name]
    }

    return $Default
}

function Quote-SqlLiteral {
    param([string]$Value)

    if ($null -eq $Value) {
        return 'NULL'
    }

    return "'" + $Value.Replace("'", "''") + "'"
}

function Get-ObjectPropertyValue {
    param(
        [object]$Object,
        [string]$Name
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Assert-NoForbiddenProperties {
    param(
        [object]$Value,
        [string]$Path = '$'
    )

    if ($null -eq $Value) {
        return
    }

    if ($Value -is [string]) {
        return
    }

    if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [System.Management.Automation.PSCustomObject])) {
        $index = 0
        foreach ($item in $Value) {
            Assert-NoForbiddenProperties -Value $item -Path "$Path[$index]"
            $index++
        }
        return
    }

    foreach ($property in $Value.PSObject.Properties) {
        if ($property.Name -match '(?i)(secret|token|password|provider[_-]?subject|connection[_-]?string|client[_-]?secret)') {
            throw "Unsupported sensitive auth field found at $Path."
        }

        Assert-NoForbiddenProperties -Value $property.Value -Path "$Path.$($property.Name)"
    }
}

function Assert-UsableAuthEmail {
    param(
        [string]$Value,
        [string]$TenantSlug,
        [string]$Role,
        [switch]$AllowPlaceholders
    )

    Assert-True (-not [string]::IsNullOrWhiteSpace($Value)) "Missing auth identity for $TenantSlug/$Role."
    Assert-True ($Value -match '^[^@\s]+@[^@\s]+\.[^@\s]+$') "Invalid auth identity format for $TenantSlug/$Role."

    $lower = $Value.ToLowerInvariant()
    $placeholderDomains = @('@demo.test', '@example.com', '@example.invalid', '@localhost')
    foreach ($domain in $placeholderDomains) {
        if ($lower.EndsWith($domain) -and -not $AllowPlaceholders) {
            throw "Placeholder auth identity for $TenantSlug/$Role. Copy validation-demo-auth-users.example.json to validation-demo-auth-users.local.json and replace placeholders, or pass -AllowPlaceholderEmails for dry validation only."
        }
    }
}

function Read-AuthUsersFile {
    param([string]$Path)

    Assert-True (Test-Path -LiteralPath $Path) "Missing auth user file: $Path. Copy $exampleUsersFile to $localUsersFile and replace placeholders with owner-controlled Auth0 test identities."

    try {
        return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    } catch {
        throw "Auth user file is not valid JSON: $Path"
    }
}

function Get-RoleUser {
    param(
        [object]$Tenant,
        [string]$Role
    )

    $matches = @($Tenant.users | Where-Object { [string]$_.role -eq $Role })
    Assert-True ($matches.Count -eq 1) "Tenant $($Tenant.slug) must define role $Role exactly once."
    return $matches[0]
}

function ConvertTo-MembershipRows {
    param([object]$Config)

    Assert-NoForbiddenProperties -Value $Config
    Assert-True ([string]$Config.environmentLabel -eq 'proof_demo') 'Auth user file environmentLabel must be proof_demo.'
    Assert-True ($Config.productionData -eq $false) 'Auth user file must not represent production data.'

    $tenants = @($Config.tenants)
    Assert-True ($tenants.Count -eq $expectedTenantSlugs.Count) 'Auth user file must define exactly the validation demo tenants.'

    $tenantMap = @{}
    foreach ($tenant in $tenants) {
        $slug = [string]$tenant.slug
        Assert-True ($expectedTenantSlugs -contains $slug) "Unexpected validation tenant slug: $slug."
        Assert-True (-not $tenantMap.ContainsKey($slug)) "Duplicate validation tenant slug: $slug."
        $tenantMap[$slug] = $tenant
    }

    $rows = @()
    foreach ($slug in $expectedTenantSlugs) {
        Assert-True ($tenantMap.ContainsKey($slug)) "Auth user file is missing tenant $slug."
        $tenant = $tenantMap[$slug]
        $definition = $tenantDefinitions[$slug]
        $tenantId = [string](Get-ObjectPropertyValue -Object $tenant -Name 'tenantId')
        if (-not [string]::IsNullOrWhiteSpace($tenantId)) {
            Assert-True ($tenantId -eq $definition.Id) "Tenant id mismatch for $slug."
        }

        $users = @($tenant.users)
        Assert-True ($users.Count -eq $expectedRoles.Count) "Tenant $slug must define one auth identity per role slot."

        $seenRoles = @{}
        $seenEmails = @{}
        foreach ($role in $expectedRoles) {
            $user = Get-RoleUser -Tenant $tenant -Role $role
            Assert-True (-not $seenRoles.ContainsKey($role)) "Duplicate role slot $role for tenant $slug."
            $seenRoles[$role] = $true

            $value = [string]$user.email
            Assert-UsableAuthEmail -Value $value -TenantSlug $slug -Role $role -AllowPlaceholders:$AllowPlaceholderEmails

            $normalized = $value.Trim().ToLowerInvariant()
            Assert-True (-not $seenEmails.ContainsKey($normalized)) "Tenant $slug has duplicate auth identities."
            $seenEmails[$normalized] = $true

            $roleDefinition = $definition.Roles[$role]
            $rows += [pscustomobject]@{
                TenantSlug = $slug
                TenantId = $definition.Id
                Role = $role
                RoleId = $roleDefinition.RoleId
                RoleName = $roleDefinition.Name
                UserId = $roleDefinition.UserId
                AssignmentId = $roleDefinition.AssignmentId
                Email = $normalized
                Permissions = @($roleDefinition.Permissions)
            }
        }
    }

    return $rows
}

function New-AuthBootstrapSql {
    param([object[]]$Rows)

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('BEGIN;')
    $lines.Add('')
    $lines.Add("INSERT INTO permission (id, code)")
    $lines.Add("VALUES ('70000000-0000-7000-8000-000000000001', 'setup.manage'),")
    $lines.Add("       ('70000000-0000-7000-8000-000000000002', 'team.manage')")
    $lines.Add('ON CONFLICT (code) DO UPDATE')
    $lines.Add('SET code = EXCLUDED.code;')

    foreach ($row in $Rows) {
        $lines.Add('')
        $lines.Add('INSERT INTO "role" (id, tenant_id, code, name)')
        $lines.Add("VALUES ($(Quote-SqlLiteral $row.RoleId)::uuid, $(Quote-SqlLiteral $row.TenantId)::uuid, $(Quote-SqlLiteral $row.Role), $(Quote-SqlLiteral $row.RoleName))")
        $lines.Add('ON CONFLICT (id) DO UPDATE')
        $lines.Add('SET tenant_id = EXCLUDED.tenant_id,')
        $lines.Add('    code = EXCLUDED.code,')
        $lines.Add('    name = EXCLUDED.name;')

        foreach ($permission in @($row.Permissions)) {
            $lines.Add('')
            $lines.Add('INSERT INTO role_permission (role_id, permission_id)')
            $lines.Add("SELECT $(Quote-SqlLiteral $row.RoleId)::uuid, permission.id")
            $lines.Add('FROM permission')
            $lines.Add("WHERE permission.code = $(Quote-SqlLiteral $permission)")
            $lines.Add('ON CONFLICT (role_id, permission_id) DO NOTHING;')
        }

        $lines.Add('')
        $lines.Add('INSERT INTO user_account (id, tenant_id, email, password_hash, mfa_secret, locale, email_verified_at, last_login_at, failed_login_attempts, locked_until, created_at, updated_at, deleted_at)')
        $lines.Add("VALUES ($(Quote-SqlLiteral $row.UserId)::uuid, $(Quote-SqlLiteral $row.TenantId)::uuid, $(Quote-SqlLiteral $row.Email), NULL, NULL, 'en', NULL, NULL, 0, NULL, now(), now(), NULL)")
        $lines.Add('ON CONFLICT (id) DO UPDATE')
        $lines.Add('SET tenant_id = EXCLUDED.tenant_id,')
        $lines.Add('    email = EXCLUDED.email,')
        $lines.Add('    locale = EXCLUDED.locale,')
        $lines.Add('    failed_login_attempts = 0,')
        $lines.Add('    locked_until = NULL,')
        $lines.Add('    updated_at = now(),')
        $lines.Add('    deleted_at = NULL;')

        $lines.Add('')
        $lines.Add('INSERT INTO role_assignment (id, tenant_id, user_id, role_id, scope_type, scope_id, granted_at, granted_by)')
        $lines.Add("VALUES ($(Quote-SqlLiteral $row.AssignmentId)::uuid, $(Quote-SqlLiteral $row.TenantId)::uuid, $(Quote-SqlLiteral $row.UserId)::uuid, $(Quote-SqlLiteral $row.RoleId)::uuid, 'tenant', NULL, now(), NULL)")
        $lines.Add('ON CONFLICT (id) DO UPDATE')
        $lines.Add('SET tenant_id = EXCLUDED.tenant_id,')
        $lines.Add('    user_id = EXCLUDED.user_id,')
        $lines.Add('    role_id = EXCLUDED.role_id,')
        $lines.Add('    scope_type = EXCLUDED.scope_type,')
        $lines.Add('    scope_id = NULL;')
    }

    $lines.Add('')
    $lines.Add('COMMIT;')
    return ($lines -join [Environment]::NewLine)
}

function Invoke-Psql {
    param(
        [string]$Docker,
        [string]$Sql,
        [hashtable]$EnvValues
    )

    $dbUser = Get-EnvValue -Values $EnvValues -Name 'POSTGRES_USER' -Default 'platform'
    $dbName = Get-EnvValue -Values $EnvValues -Name 'POSTGRES_DB' -Default 'platform'

    Push-Location $repoRoot
    try {
        $arguments = @(
            'compose',
            '--env-file', $EnvFile,
            '-f', $composeFile,
            'exec', '-T', 'postgres',
            'psql', '-v', 'ON_ERROR_STOP=1',
            '-U', $dbUser,
            '-d', $dbName
        )

        $commandOutput = $Sql | & $Docker @arguments 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw 'Validation auth bootstrap failed. Inspect Postgres and Docker logs; command output was intentionally not echoed.'
        }
    } finally {
        Pop-Location
    }
}

function Invoke-TenantBootstrap {
    param(
        [string]$Docker,
        [hashtable]$EnvValues
    )

    if ($SkipTenantBootstrap) {
        Write-Host 'Skipping tenant row bootstrap; assuming validation tenants already exist.'
        return
    }

    Assert-True (Test-Path -LiteralPath $tenantBootstrapFile) "Missing tenant-bootstrap.sql: $tenantBootstrapFile"
    $sql = Get-Content -LiteralPath $tenantBootstrapFile -Raw
    Invoke-Psql -Docker $Docker -Sql $sql -EnvValues $EnvValues
}

$config = Read-AuthUsersFile -Path $UsersFile
$memberships = @(ConvertTo-MembershipRows -Config $config)
$sql = New-AuthBootstrapSql -Rows $memberships

Assert-True ($sql -match 'ON CONFLICT') 'Generated bootstrap SQL must be idempotent.'
Assert-True ($sql -notmatch '(?i)INSERT\s+INTO\s+external_auth_identity') 'Generated bootstrap SQL must not create external identity bindings.'
Assert-True ($sql -notmatch '(?i)INSERT\s+INTO\s+auth_session') 'Generated bootstrap SQL must not create auth sessions.'

if ($ValidateOnly) {
    Write-Host "Validated validation demo auth membership input for $($memberships.Count) users across $($expectedTenantSlugs.Count) tenants."
    return
}

Assert-True (Test-Path -LiteralPath $EnvFile) 'Missing staging env file. Start the staging stack or pass -EnvFile.'

$docker = Get-DockerCommand
$envValues = Read-EnvFile -Path $EnvFile

Invoke-TenantBootstrap -Docker $docker -EnvValues $envValues
Invoke-Psql -Docker $docker -Sql $sql -EnvValues $envValues

Write-Host "Bootstrapped validation demo auth memberships for $($memberships.Count) users across $($expectedTenantSlugs.Count) tenants."
Write-Host 'Next: run deploy/staging/seed-validation-demo.ps1, then smoke Auth0 login through /auth/login for each validation tenant.'
