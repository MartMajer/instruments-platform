$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$composeFile = Join-Path $repoRoot 'deploy\staging\docker-compose.yml'
$envFile = Join-Path $repoRoot 'deploy\staging\.env'

if (-not (Test-Path $envFile)) {
    throw 'deploy/staging/.env does not exist. Run deploy/staging/start-local-staging.ps1 first.'
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
            $values[$parts[0]] = $parts[1]
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

    if ($Values.ContainsKey($Name) -and $Values[$Name].Length -gt 0) {
        return $Values[$Name]
    }

    return $Default
}

function Wait-HttpOk {
    param(
        [string]$Url,
        [int]$Attempts = 40
    )

    for ($i = 0; $i -lt $Attempts; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                return $response
            }
        } catch {
            Start-Sleep -Seconds 2
            continue
        }

        Start-Sleep -Seconds 2
    }

    throw "Timed out waiting for $Url."
}

function Get-HttpStatus {
    param([string]$Url)

    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        return [int]$response.StatusCode
    } catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            return [int]$_.Exception.Response.StatusCode
        }

        throw
    }
}

function Assert-ComposeServiceRunning {
    param([string]$Service)

    $docker = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $docker) {
        $dockerBin = 'C:\Program Files\Docker\Docker\resources\bin'
        if (Test-Path (Join-Path $dockerBin 'docker.exe')) {
            $env:PATH = "$dockerBin;$env:PATH"
        }

        $docker = Get-Command docker -ErrorAction SilentlyContinue
    }

    if (-not $docker) {
        throw 'docker.exe was not found. Cannot verify local staging Compose services.'
    }

    $runningServices = docker compose --env-file $envFile -f $composeFile ps --status running --services
    if (-not ($runningServices | Where-Object { $_ -eq $Service })) {
        throw "Expected local staging Compose service '$Service' to be running."
    }
}

function Invoke-AuthenticatedDevSession {
    try {
        return Invoke-RestMethod -Uri "$apiBaseUrl/auth/session" -Headers $headers -TimeoutSec 10
    } catch {
        $statusCode = $null
        if ($_.Exception.Response -is [System.Net.HttpWebResponse]) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        if ($statusCode -in @(401, 403)) {
            throw "Authenticated local staging smoke failed because development authentication is disabled. Recreate local staging with Authentication__Dev__Enabled=true and PUBLIC_DEV_AUTH_ENABLED=true before running this smoke."
        }

        throw
    }
}

function Wait-WorkerHeartbeat {
    param(
        [string]$WorkerName,
        [string]$PostgresDatabase,
        [string]$PostgresUser,
        [string]$PostgresPassword,
        [int]$Attempts = 20
    )

    for ($i = 0; $i -lt $Attempts; $i++) {
        try {
            $safeWorkerName = $WorkerName.Replace("'", "''")
            $count = docker compose --env-file $envFile -f $composeFile exec -T -e PGPASSWORD=$PostgresPassword postgres psql `
                -v ON_ERROR_STOP=1 `
                -U $PostgresUser `
                -d $PostgresDatabase `
                -tA `
                -c "SELECT COUNT(*) FROM worker_heartbeat WHERE worker_name = '$safeWorkerName';"

            if ([int]$count.Trim() -gt 0) {
                return
            }
        } catch {
            Start-Sleep -Seconds 2
            continue
        }

        Start-Sleep -Seconds 2
    }

    throw "Timed out waiting for worker heartbeat for '$WorkerName'."
}

$envValues = Read-EnvFile -Path $envFile
$postgresDatabase = Get-EnvValue $envValues 'POSTGRES_DB' 'instruments_platform_staging'
$postgresUser = Get-EnvValue $envValues 'POSTGRES_USER' 'platform_app'
$postgresPassword = Get-EnvValue $envValues 'POSTGRES_PASSWORD' 'platform_app_staging'
$apiPort = Get-EnvValue $envValues 'API_HTTP_PORT' '5055'
$webPort = Get-EnvValue $envValues 'WEB_HTTP_PORT' '5174'
$tenantId = Get-EnvValue $envValues 'PUBLIC_DEV_TENANT_ID' '11111111-1111-4111-8111-111111111111'
$userId = Get-EnvValue $envValues 'PUBLIC_DEV_USER_ID' '22222222-2222-4222-8222-222222222222'
$workerHeartbeatName = Get-EnvValue $envValues 'WorkerHeartbeat__WorkerName' 'platform-workers'

$apiBaseUrl = "http://127.0.0.1:$apiPort"
$webBaseUrl = "http://127.0.0.1:$webPort"

Assert-ComposeServiceRunning -Service 'worker'
Wait-WorkerHeartbeat -WorkerName $workerHeartbeatName `
    -PostgresDatabase $postgresDatabase `
    -PostgresUser $postgresUser `
    -PostgresPassword $postgresPassword

$health = Wait-HttpOk -Url "$apiBaseUrl/health"
$healthJson = $health.Content | ConvertFrom-Json
if ($healthJson.status -ne 'ok') {
    throw "Unexpected health status: $($health.Content)"
}

$unauthenticatedStatus = Get-HttpStatus -Url "$apiBaseUrl/auth/session"
if ($unauthenticatedStatus -ne 401) {
    throw "Expected unauthenticated /auth/session to return 401, got $unauthenticatedStatus."
}

$headers = @{
    'X-Tenant-Id' = $tenantId
    'X-Dev-User-Id' = $userId
    'X-Dev-Tenant-Memberships' = $tenantId
    'X-Dev-Permissions' = 'setup.manage'
}

$session = Invoke-AuthenticatedDevSession
if ($session.tenantId -ne $tenantId) {
    throw "Expected dev auth tenant $tenantId, got $($session.tenantId)."
}

$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 8)
$instrumentCode = "docker-smoke-$suffix"
$instrumentRequest = @{
    code = $instrumentCode
    version = '1.0.0'
    fullName = "Docker smoke $suffix"
    domain = 'psychometric'
    provenanceNote = 'Synthetic DEP01-A smoke instrument.'
    rightsStatus = 'attested_by_tenant'
    validityLabel = 'tenant_provided'
    licenseType = 'unknown'
} | ConvertTo-Json

$created = Invoke-RestMethod `
    -Uri "$apiBaseUrl/instruments/private-imports" `
    -Method Post `
    -Headers $headers `
    -ContentType 'application/json' `
    -Body $instrumentRequest `
    -TimeoutSec 20

if ($created.code -ne $instrumentCode) {
    throw "Expected created instrument $instrumentCode, got $($created.code)."
}

$instruments = Invoke-RestMethod -Uri "$apiBaseUrl/instruments" -Headers $headers -TimeoutSec 20
if (-not ($instruments | Where-Object { $_.code -eq $instrumentCode })) {
    throw "Created instrument $instrumentCode was not returned by /instruments."
}

$web = Wait-HttpOk -Url "$webBaseUrl/"
if ($web.Content -notmatch 'Instruments Platform') {
    throw 'Frontend did not return the product entry shell.'
}

Write-Host 'Local staging smoke passed.'
