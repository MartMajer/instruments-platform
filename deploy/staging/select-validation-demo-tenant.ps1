param(
    [Parameter(Mandatory = $true)]
    [string]$TenantSlug,
    [string]$EnvFile,
    [string]$ApiOrigin,
    [string]$WebOrigin,
    [switch]$ValidateOnly,
    [switch]$NoPromptLogin,
    [switch]$Restart
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$composeFile = Join-Path $repoRoot 'deploy\staging\docker-compose.yml'
$vpsComposeFile = Join-Path $repoRoot 'deploy\staging\docker-compose.vps.yml'

if (-not $EnvFile) {
    $EnvFile = Join-Path $repoRoot 'deploy\staging\.env'
}

$tenantDefinitions = @{
    'validation-oh-research' = @{
        Id = '33333333-3333-4333-8333-333333333333'
        Label = 'Occupational Health Research'
        PrimaryRoles = @('tenant_owner', 'researcher')
        SmokeRoles = @('analyst', 'viewer')
    }
    'validation-se-education' = @{
        Id = '44444444-4444-4444-8444-444444444444'
        Label = 'Software Engineering Education'
        PrimaryRoles = @('tenant_owner', 'researcher')
        SmokeRoles = @('analyst', 'viewer')
    }
    'validation-osh-consulting' = @{
        Id = '55555555-5555-4555-8555-555555555555'
        Label = 'Workplace Safety Consulting'
        PrimaryRoles = @('tenant_owner', 'researcher')
        SmokeRoles = @('analyst', 'viewer')
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

function Read-EnvFile {
    param([string]$Path)

    $values = @{}
    if (-not (Test-Path -LiteralPath $Path)) {
        return $values
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#')) {
            continue
        }

        $parts = $trimmed.Split('=', 2)
        if ($parts.Length -eq 2) {
            $values[$parts[0].Trim()] = $parts[1]
        }
    }

    return $values
}

function Get-FirstValue {
    param(
        [AllowEmptyString()]
        [string[]]$Values,
        [string]$Fallback
    )

    foreach ($value in $Values) {
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value.Trim()
        }
    }

    return $Fallback
}

function Normalize-Origin {
    param(
        [string]$Value,
        [string]$Name
    )

    $normalized = $Value.Trim().TrimEnd('/')
    $uri = $null
    if (-not [System.Uri]::TryCreate($normalized, [System.UriKind]::Absolute, [ref]$uri)) {
        throw "$Name must be an absolute http(s) origin."
    }

    if ($uri.Scheme -ne 'http' -and $uri.Scheme -ne 'https') {
        throw "$Name must use http or https."
    }

    if (-not [string]::IsNullOrWhiteSpace($uri.AbsolutePath) -and $uri.AbsolutePath -ne '/') {
        throw "$Name must be an origin only, without a path."
    }

    return $normalized
}

function Join-OriginPath {
    param(
        [string]$Origin,
        [string]$Path
    )

    return $Origin.TrimEnd('/') + '/' + $Path.TrimStart('/')
}

function Encode-ReturnUrl {
    param([string]$Url)

    return [System.Uri]::EscapeDataString($Url)
}

function Update-EnvLines {
    param(
        [string[]]$Lines,
        [System.Collections.Specialized.OrderedDictionary]$Updates
    )

    # Update known keys in place and preserve unrelated comments/settings.
    $seen = @{}
    $updated = New-Object System.Collections.Generic.List[string]

    foreach ($line in $Lines) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#') -or -not $trimmed.Contains('=')) {
            $updated.Add($line)
            continue
        }

        $parts = $trimmed.Split('=', 2)
        $key = $parts[0].Trim()
        if ($Updates.Contains($key)) {
            $updated.Add("$key=$($Updates[$key])")
            $seen[$key] = $true
        } else {
            $updated.Add($line)
        }
    }

    foreach ($key in $Updates.Keys) {
        if (-not $seen.ContainsKey($key)) {
            $updated.Add("$key=$($Updates[$key])")
        }
    }

    return $updated.ToArray()
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
        throw 'docker.exe was not found. Install Docker or add Docker to PATH before restarting local staging.'
    }

    return $docker.Source
}

function Write-SelectionSummary {
    param(
        [string]$Slug,
        [hashtable]$Definition,
        [string]$ResolvedWebOrigin,
        [string]$ResolvedEnvFile,
        [bool]$WasValidateOnly
    )

    Write-Host "Validation tenant: $Slug"
    Write-Host "Tenant id: $($Definition.Id)"
    Write-Host "Primary walkthrough role slots: $($Definition.PrimaryRoles -join ', ')"
    Write-Host "Membership smoke role slots: $($Definition.SmokeRoles -join ', ')"
    Write-Host "App URL after restart: $(Join-OriginPath $ResolvedWebOrigin '/app')"

    if ($WasValidateOnly) {
        Write-Host "ValidateOnly: no file was written."
    } else {
        Write-Host "Updated ignored local env file: $ResolvedEnvFile"
    }

    Write-Host 'Next: restart the local staging stack if you did not pass -Restart, then sign in with a matching role slot.'
}

Assert-True $tenantDefinitions.ContainsKey($TenantSlug) "Unknown validation tenant '$TenantSlug'. Use one of: $($tenantDefinitions.Keys -join ', ')."
Assert-True (Test-Path -LiteralPath $EnvFile) "Missing env file '$EnvFile'. Create or copy deploy/staging/.env first; this helper will not create placeholder secret files."

$definition = $tenantDefinitions[$TenantSlug]
$envValues = Read-EnvFile $EnvFile

$resolvedApiOrigin = Normalize-Origin `
    (Get-FirstValue @($ApiOrigin, $envValues['STAGING_API_ORIGIN'], $envValues['PUBLIC_API_BASE_URL']) 'http://127.0.0.1:5055') `
    'ApiOrigin'
$resolvedWebOrigin = Normalize-Origin `
    (Get-FirstValue @($WebOrigin, $envValues['STAGING_WEB_ORIGIN'], $envValues['Cors__AllowedOrigins__0']) 'http://127.0.0.1:5174') `
    'WebOrigin'

$appReturnUrl = Encode-ReturnUrl (Join-OriginPath $resolvedWebOrigin '/app')
$rootReturnUrl = Encode-ReturnUrl (Join-OriginPath $resolvedWebOrigin '/')
$loginUrl = "$(Join-OriginPath $resolvedApiOrigin '/auth/login')?returnUrl=$appReturnUrl"
if (-not $NoPromptLogin) {
    $loginUrl = "$loginUrl&prompt=login"
}

$updates = [ordered]@{
    'PUBLIC_TENANT_ID' = $definition.Id
    'PUBLIC_API_BASE_URL' = $resolvedApiOrigin
    'Cors__AllowedOrigins__0' = $resolvedWebOrigin
    'PUBLIC_AUTH_LOGIN_URL' = $loginUrl
    'PUBLIC_AUTH_LOGOUT_URL' = "$(Join-OriginPath $resolvedApiOrigin '/auth/logout')?returnUrl=$rootReturnUrl"
}

$lines = Get-Content -LiteralPath $EnvFile
$updatedLines = Update-EnvLines -Lines $lines -Updates $updates

if ($ValidateOnly) {
    Write-SelectionSummary `
        -Slug $TenantSlug `
        -Definition $definition `
        -ResolvedWebOrigin $resolvedWebOrigin `
        -ResolvedEnvFile $EnvFile `
        -WasValidateOnly $true
    Write-Host 'Planned keys: PUBLIC_TENANT_ID, PUBLIC_API_BASE_URL, Cors__AllowedOrigins__0, PUBLIC_AUTH_LOGIN_URL, PUBLIC_AUTH_LOGOUT_URL'
    return
}

[System.IO.File]::WriteAllLines((Resolve-Path -LiteralPath $EnvFile), $updatedLines)

Write-SelectionSummary `
    -Slug $TenantSlug `
    -Definition $definition `
    -ResolvedWebOrigin $resolvedWebOrigin `
    -ResolvedEnvFile $EnvFile `
    -WasValidateOnly $false

if ($Restart) {
    $docker = Get-DockerCommand
    Push-Location $repoRoot
    try {
        Write-Host 'Restarting local staging stack with docker compose.'
        & $docker compose --env-file $EnvFile -f $composeFile -f $vpsComposeFile up -d --build
    } finally {
        Pop-Location
    }
}
