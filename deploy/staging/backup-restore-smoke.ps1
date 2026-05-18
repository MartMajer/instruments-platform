param(
    [string]$EnvFile,
    [string]$BackupDirectory,
    [string]$BackupFile,
    [string]$RestoreProjectName,
    [string]$EvidencePath,
    [switch]$SkipBackup,
    [switch]$KeepRestoreVolume
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$composeFile = Join-Path $repoRoot 'deploy\staging\docker-compose.yml'

if (-not $EnvFile) {
    $EnvFile = Join-Path $repoRoot 'deploy\staging\.env'
}

if (-not $BackupDirectory) {
    $BackupDirectory = Join-Path $repoRoot 'artifacts/deployment-dr/backups'
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
        throw 'docker.exe was not found. Install Docker or add Docker to PATH before running backup/restore smoke.'
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

function Get-RequiredEnvValue {
    param(
        [hashtable]$Values,
        [string]$Name
    )

    if (-not $Values.ContainsKey($Name) -or [string]::IsNullOrWhiteSpace($Values[$Name])) {
        throw "$Name is required in the env file for backup/restore smoke."
    }

    return $Values[$Name]
}

function ConvertTo-DockerMountPath {
    param([string]$Path)

    $resolved = Resolve-Path -LiteralPath $Path
    return $resolved.Path.Replace('\', '/')
}

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $repoRoot $Path
}

function Get-SafeFileHash {
    param([string]$Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Invoke-Native {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$Operation
    )

    $nativeErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $FilePath @Arguments 2>&1
        $nativeExitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $nativeErrorActionPreference
    }

    if ($nativeExitCode -ne 0) {
        throw "$Operation failed. Inspect Docker logs for the staging stack; command output was intentionally not echoed to avoid leaking sensitive values."
    }

    return $output
}

function Invoke-DockerCompose {
    param(
        [string]$Docker,
        [string[]]$Arguments,
        [string]$Operation
    )

    $dockerArguments = @('compose') + $Arguments
    return Invoke-Native -FilePath $Docker -Arguments $dockerArguments -Operation $Operation
}

function Wait-RestorePostgres {
    param(
        [string]$Docker,
        [string]$ProjectName,
        [string]$ComposeFile,
        [string]$EnvFilePath,
        [string]$Db,
        [string]$User
    )

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        try {
            Invoke-DockerCompose -Docker $Docker -Operation 'restore readiness check' -Arguments @(
                '--env-file', $EnvFilePath,
                '-p', $ProjectName,
                '-f', $ComposeFile,
                'exec', '-T', 'restore-postgres',
                'pg_isready', '-U', $User, '-d', $Db
            ) | Out-Null
            return
        } catch {
            Start-Sleep -Seconds 2
        }
    }

    throw 'Timed out waiting for disposable restore Postgres to become ready.'
}

function Write-BackupRestoreEvidence {
    param([object]$Evidence)

    if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
        return
    }

    $directory = Split-Path -Path $EvidencePath -Parent
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $json = $Evidence | ConvertTo-Json -Depth 12
    Set-Content -Path $EvidencePath -Value $json -Encoding utf8
    Write-Host ''
    Write-Host "Backup/restore evidence written to $EvidencePath"
}

Set-Location $repoRoot

if (-not (Test-Path -LiteralPath $EnvFile)) {
    throw 'deploy/staging/.env does not exist. Start the staging stack or pass -EnvFile.'
}

$docker = Get-DockerCommand
$envValues = Read-EnvFile -Path $EnvFile
$composeProjectName = Get-RequiredEnvValue -Values $envValues -Name 'COMPOSE_PROJECT_NAME'
$postgresDb = Get-RequiredEnvValue -Values $envValues -Name 'POSTGRES_DB'
$postgresUser = Get-RequiredEnvValue -Values $envValues -Name 'POSTGRES_USER'
$postgresPassword = Get-RequiredEnvValue -Values $envValues -Name 'POSTGRES_PASSWORD'
$postgresWorkerUser = 'platform_worker'
if ($envValues.ContainsKey('POSTGRES_WORKER_USER') -and -not [string]::IsNullOrWhiteSpace($envValues['POSTGRES_WORKER_USER'])) {
    $postgresWorkerUser = $envValues['POSTGRES_WORKER_USER']
}

if ($postgresWorkerUser -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
    throw 'POSTGRES_WORKER_USER must be a valid unquoted PostgreSQL role identifier for backup/restore smoke.'
}

if (-not $RestoreProjectName) {
    $RestoreProjectName = "$composeProjectName-restore-smoke"
}

$backupDirectoryPath = Resolve-RepoPath -Path $BackupDirectory
New-Item -ItemType Directory -Force -Path $backupDirectoryPath | Out-Null

if ($BackupFile) {
    $backupFilePath = Resolve-RepoPath -Path $BackupFile
} else {
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backupFilePath = Join-Path $backupDirectoryPath "platform-staging-$timestamp.dump"
}

$backupFileName = Split-Path -Path $backupFilePath -Leaf

if ($SkipBackup) {
    if (-not (Test-Path -LiteralPath $backupFilePath)) {
        throw 'SkipBackup requires an existing -BackupFile.'
    }
} else {
    Invoke-DockerCompose -Docker $docker -Operation 'source Postgres readiness check' -Arguments @(
        '--env-file', $EnvFile,
        '-f', $composeFile,
        'exec', '-T', 'postgres',
        'pg_isready', '-U', $postgresUser, '-d', $postgresDb
    ) | Out-Null

    $backupMountPath = ConvertTo-DockerMountPath -Path $backupDirectoryPath
    $networkName = "$composeProjectName`_default"
    $pgDumpCommand = "PGPASSWORD=`"`$POSTGRES_PASSWORD`" pg_dump -h postgres -Fc --no-owner --no-acl -U `"`$POSTGRES_USER`" -d `"`$POSTGRES_DB`" -f `"/backup/$backupFileName`""

    Invoke-Native -FilePath $docker -Operation 'pg_dump backup' -Arguments @(
        'run', '--rm',
        '--network', $networkName,
        '--env-file', $EnvFile,
        '-v', "$backupMountPath`:/backup",
        'postgres:17-alpine',
        'sh', '-c',
        $pgDumpCommand
    ) | Out-Null
}

if (-not (Test-Path -LiteralPath $backupFilePath)) {
    throw 'Backup file was not created.'
}

$backupItem = Get-Item -LiteralPath $backupFilePath
if ($backupItem.Length -le 0) {
    throw 'Backup file is empty.'
}

$backupSha256 = Get-SafeFileHash -Path $backupFilePath

$restoreRoot = Join-Path $repoRoot 'artifacts\deployment-dr\restore-smoke'
New-Item -ItemType Directory -Force -Path $restoreRoot | Out-Null

$restoreComposeFile = Join-Path $restoreRoot 'restore-compose.yml'
$backupMountForRestore = ConvertTo-DockerMountPath -Path (Split-Path -Path $backupFilePath -Parent)
$restoreComposeTemplate = @'
services:
  restore-postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U $${POSTGRES_USER} -d $${POSTGRES_DB}"]
      interval: 2s
      timeout: 3s
      retries: 30
    volumes:
      - restore_postgres_data:/var/lib/postgresql/data
      - "__BACKUP_MOUNT__:/backup:ro"

volumes:
  restore_postgres_data:
'@

$restoreCompose = $restoreComposeTemplate.Replace('__BACKUP_MOUNT__', $backupMountForRestore)
Set-Content -LiteralPath $restoreComposeFile -Value $restoreCompose -Encoding utf8

try {
    Invoke-DockerCompose -Docker $docker -Operation 'restore Postgres startup' -Arguments @(
        '--env-file', $EnvFile,
        '-p', $RestoreProjectName,
        '-f', $restoreComposeFile,
        'up', '-d', 'restore-postgres'
    ) | Out-Null

    Wait-RestorePostgres -Docker $docker -ProjectName $RestoreProjectName -ComposeFile $restoreComposeFile -EnvFilePath $EnvFile -Db $postgresDb -User $postgresUser

    Invoke-DockerCompose -Docker $docker -Operation 'restore worker role bootstrap' -Arguments @(
        '--env-file', $EnvFile,
        '-p', $RestoreProjectName,
        '-f', $restoreComposeFile,
        'exec', '-T',
        '-e', "PGPASSWORD=$postgresPassword",
        'restore-postgres',
        'createuser',
        '-h', 'localhost',
        '-U', $postgresUser,
        $postgresWorkerUser
    ) | Out-Null

    $pgRestoreCommand = "PGPASSWORD=`"`$POSTGRES_PASSWORD`" pg_restore --clean --if-exists --no-owner --no-acl -h localhost -U `"`$POSTGRES_USER`" -d `"`$POSTGRES_DB`" `"/backup/$backupFileName`""

    Invoke-DockerCompose -Docker $docker -Operation 'pg_restore restore' -Arguments @(
        '--env-file', $EnvFile,
        '-p', $RestoreProjectName,
        '-f', $restoreComposeFile,
        'exec', '-T', 'restore-postgres',
        'sh', '-c',
        $pgRestoreCommand
    ) | Out-Null

    $tableCount = Invoke-DockerCompose -Docker $docker -Operation 'restore table-count verification' -Arguments @(
        '--env-file', $EnvFile,
        '-p', $RestoreProjectName,
        '-f', $restoreComposeFile,
        'exec', '-T',
        '-e', "PGPASSWORD=$postgresPassword",
        'restore-postgres',
        'psql',
        '-X', '-A', '-t',
        '-h', 'localhost',
        '-U', $postgresUser,
        '-d', $postgresDb,
        '-c', "SET session_replication_role = DEFAULT; SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';"
    )

    $relationCount = Invoke-DockerCompose -Docker $docker -Operation 'restore relation-count verification' -Arguments @(
        '--env-file', $EnvFile,
        '-p', $RestoreProjectName,
        '-f', $restoreComposeFile,
        'exec', '-T',
        '-e', "PGPASSWORD=$postgresPassword",
        'restore-postgres',
        'psql',
        '-X', '-A', '-t',
        '-h', 'localhost',
        '-U', $postgresUser,
        '-d', $postgresDb,
        '-c', "SELECT COUNT(*) FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' AND c.relkind = 'r';"
    )

    $requiredTables = Invoke-DockerCompose -Docker $docker -Operation 'restore required-table verification' -Arguments @(
        '--env-file', $EnvFile,
        '-p', $RestoreProjectName,
        '-f', $restoreComposeFile,
        'exec', '-T',
        '-e', "PGPASSWORD=$postgresPassword",
        'restore-postgres',
        'psql',
        '-X', '-A', '-t',
        '-h', 'localhost',
        '-U', $postgresUser,
        '-d', $postgresDb,
        '-c', "SELECT to_regclass('public.tenant') IS NOT NULL AND to_regclass('public.audit_event') IS NOT NULL;"
    )

    if (($requiredTables -join "`n") -notmatch 't') {
        throw 'Restored database is missing required platform tables.'
    }

    $restorePublicTableCountRaw = $tableCount | Where-Object { $_ -match '^\d+$' } | Select-Object -Last 1
    $restorePublicRelationCountRaw = $relationCount | Where-Object { $_ -match '^\d+$' } | Select-Object -Last 1

    if ($null -eq $restorePublicTableCountRaw) {
        throw 'Restore public table count was not returned.'
    }

    if ($null -eq $restorePublicRelationCountRaw) {
        throw 'Restore public relation count was not returned.'
    }

    $restorePublicTableCount = [int]$restorePublicTableCountRaw
    $restorePublicRelationCount = [int]$restorePublicRelationCountRaw

    $backupRestoreEvidence = [ordered]@{
        schemaVersion = 1
        generatedAt = [DateTimeOffset]::UtcNow.ToString('o')
        runner = 'deploy/staging/backup-restore-smoke.ps1'
        status = 'passed'
        backup = [ordered]@{
            backupBytes = [int64]$backupItem.Length
            backupSha256 = $backupSha256
            skipBackup = [bool]$SkipBackup
        }
        restore = [ordered]@{
            restorePublicTableCount = $restorePublicTableCount
            restorePublicRelationCount = $restorePublicRelationCount
            requiredPlatformTablesPresent = $true
            restoreVolumeKept = [bool]$KeepRestoreVolume
        }
        limitations = @(
            'Q-053 blocks real-person production legal/GDPR/DPA claims; this backup/restore evidence is engineering proof only.',
            'Backup/restore evidence omits database names, users, passwords, connection strings, env file values, credential values, and host paths.'
        )
    }

    Write-BackupRestoreEvidence -Evidence $backupRestoreEvidence

    Write-Host "Backup file created: $backupFilePath"
    Write-Host "Backup bytes: $($backupItem.Length)"
    Write-Host "Restore public table count: $restorePublicTableCount"
    Write-Host "Restore public relation count: $restorePublicRelationCount"
    Write-Host 'Backup/restore smoke passed.'
} finally {
    if ($KeepRestoreVolume) {
        Write-Host "Keeping restore project '$RestoreProjectName'. Remove it manually with: docker compose -p $RestoreProjectName -f <restore-compose.yml> down --volumes"
    } else {
        try {
            Invoke-DockerCompose -Docker $docker -Operation 'restore cleanup' -Arguments @(
                '--env-file', $EnvFile,
                '-p', $RestoreProjectName,
                '-f', $restoreComposeFile,
                'down', '--volumes'
            ) | Out-Null
        } catch {
            Write-Host 'Restore cleanup failed; inspect Docker state manually.'
        }
    }
}
