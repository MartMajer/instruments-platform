param(
    [switch]$SkipLiveSmoke,
    [switch]$SkipDockerConfig,
    [switch]$SkipNodePackageTests,
    [switch]$SkipEmailRecoveryRegression,
    [switch]$SkipRetentionAutomationRegression,
    [switch]$SkipWebBuild,
    [switch]$SkipWebAudit,
    [switch]$SkipWebBundleCheck,
    [switch]$SkipLocalStagingRefresh,
    [switch]$SkipBackupRestoreSmoke,
    [string]$RemoteValidationTenantSlug = 'validation-oh-research',
    [string]$RemoteApiOrigin = '',
    [string]$RemoteWebOrigin = '',
    [switch]$SkipRemotePreflight,
    [string]$EvidencePath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$stepNumber = 0
$releaseChecksEvidence = [ordered]@{
    passedGates = [System.Collections.Generic.List[object]]::new()
    skippedGates = [System.Collections.Generic.List[object]]::new()
}

function Invoke-ReleaseStep {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    $script:stepNumber++
    Write-Host ''
    Write-Host "[$script:stepNumber] $Name"
    & $Action
    $script:releaseChecksEvidence['passedGates'].Add([ordered]@{
        number = $script:stepNumber
        name = $Name
    }) | Out-Null
}

function Add-ReleaseSkip {
    param(
        [string]$Name,
        [string]$Reason,
        [string]$Flag = ''
    )

    Write-Host ''
    Write-Host "[skip] $Reason"
    $script:releaseChecksEvidence['skippedGates'].Add([ordered]@{
        name = $Name
        reason = $Reason
        flag = $Flag
    }) | Out-Null
}

function Get-ProductSpineEvidencePath {
    if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
        return ''
    }

    $directory = Split-Path -Path $EvidencePath -Parent
    $fileName = [IO.Path]::GetFileNameWithoutExtension($EvidencePath)
    $productSpineFileName = "$fileName.product-spine.json"
    if ([string]::IsNullOrWhiteSpace($directory)) {
        return $productSpineFileName
    }

    return Join-Path $directory $productSpineFileName
}

function Get-BackupRestoreEvidencePath {
    if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
        return ''
    }

    $directory = Split-Path -Path $EvidencePath -Parent
    $fileName = [IO.Path]::GetFileNameWithoutExtension($EvidencePath)
    $backupRestoreFileName = "$fileName.backup-restore.json"
    if ([string]::IsNullOrWhiteSpace($directory)) {
        return $backupRestoreFileName
    }

    return Join-Path $directory $backupRestoreFileName
}

function Get-RemotePreflightEvidencePath {
    if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
        return ''
    }

    $directory = Split-Path -Path $EvidencePath -Parent
    $fileName = [IO.Path]::GetFileNameWithoutExtension($EvidencePath)
    $remotePreflightFileName = "$fileName.remote-preflight.json"
    if ([string]::IsNullOrWhiteSpace($directory)) {
        return $remotePreflightFileName
    }

    return Join-Path $directory $remotePreflightFileName
}

function Test-RemotePreflightExpected {
    return (-not $SkipRemotePreflight) `
        -and (-not [string]::IsNullOrWhiteSpace($RemoteApiOrigin)) `
        -and (-not [string]::IsNullOrWhiteSpace($RemoteWebOrigin))
}

function Get-ProofScope {
    $liveSmokeProven = -not $SkipLiveSmoke
    $backupRestoreProven = $liveSmokeProven -and (-not $SkipBackupRestoreSmoke)
    $remotePreflightProven = Test-RemotePreflightExpected
    $campaignEmailRecoveryRegressionProven = @($script:releaseChecksEvidence['passedGates'] | Where-Object {
        $_.name -eq 'Run campaign email failed-delivery recovery regression'
    }).Count -gt 0
    $retentionDueBatchAutomationRegressionProven = @($script:releaseChecksEvidence['passedGates'] | Where-Object {
        $_.name -eq 'Run retention due-batch automation regression'
    }).Count -gt 0

    return [ordered]@{
        localDefaultStagingProven = [bool]$liveSmokeProven
        productSpineProven = [bool]$liveSmokeProven
        backupRestoreProven = [bool]$backupRestoreProven
        campaignFailedDeliveryRecoveryRegressionProven = [bool]$campaignEmailRecoveryRegressionProven
        retentionDueBatchAutomationRegressionProven = [bool]$retentionDueBatchAutomationRegressionProven
        remotePreflightProven = [bool]$remotePreflightProven
        remoteVpsDeploymentProven = $false
        realPersonLegalUseApproved = $false
        outboundOperationalNotificationEmailProven = $false
    }
}

function Get-ClaimBoundary {
    return [ordered]@{
        engineeringEvidenceOnly = $true
        remoteProofRequiresOwnerOrigins = $true
        q053BlocksRealPersonLegalClaims = $true
        q054BlocksOperationalNotificationEmailClaims = $true
    }
}

function Get-SafeFileHash {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-EvidenceArtifact {
    param(
        [string]$Name,
        [string]$Path
    )

    $exists = -not [string]::IsNullOrWhiteSpace($Path) -and (Test-Path -LiteralPath $Path)
    return [ordered]@{
        name = $Name
        path = $Path
        exists = [bool]$exists
        sha256 = if ($exists) { Get-SafeFileHash -Path $Path } else { '' }
    }
}

function Get-EvidenceArtifacts {
    return [ordered]@{
        productSpine = Get-EvidenceArtifact -Name 'productSpine' -Path (Get-ProductSpineEvidencePath)
        backupRestore = Get-EvidenceArtifact -Name 'backupRestore' -Path (Get-BackupRestoreEvidencePath)
        remotePreflight = Get-EvidenceArtifact -Name 'remotePreflight' -Path (Get-RemotePreflightEvidencePath)
    }
}

function Write-ReleaseEvidence {
    if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
        return
    }

    $directory = Split-Path -Path $EvidencePath -Parent
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $evidence = [ordered]@{
        schemaVersion = 1
        generatedAt = [DateTimeOffset]::UtcNow.ToString('o')
        runner = 'deploy/staging/run-release-checks.ps1'
        status = 'passed'
        proofScope = Get-ProofScope
        claimBoundary = Get-ClaimBoundary
        evidenceArtifacts = Get-EvidenceArtifacts
        productSpineEvidencePath = Get-ProductSpineEvidencePath
        backupRestoreEvidencePath = Get-BackupRestoreEvidencePath
        remotePreflightEvidencePath = Get-RemotePreflightEvidencePath
        passedGates = $script:releaseChecksEvidence['passedGates'].ToArray()
        skippedGates = $script:releaseChecksEvidence['skippedGates'].ToArray()
        inputs = [ordered]@{
            skipLiveSmoke = [bool]$SkipLiveSmoke
            skipDockerConfig = [bool]$SkipDockerConfig
            skipNodePackageTests = [bool]$SkipNodePackageTests
            skipEmailRecoveryRegression = [bool]$SkipEmailRecoveryRegression
            skipRetentionAutomationRegression = [bool]$SkipRetentionAutomationRegression
            skipWebBuild = [bool]$SkipWebBuild
            skipWebAudit = [bool]$SkipWebAudit
            skipWebBundleCheck = [bool]$SkipWebBundleCheck
            skipLocalStagingRefresh = [bool]$SkipLocalStagingRefresh
            skipBackupRestoreSmoke = [bool]$SkipBackupRestoreSmoke
            skipRemotePreflight = [bool]$SkipRemotePreflight
            remoteApiOriginConfigured = -not [string]::IsNullOrWhiteSpace($RemoteApiOrigin)
            remoteWebOriginConfigured = -not [string]::IsNullOrWhiteSpace($RemoteWebOrigin)
            remoteValidationTenantSlugConfigured = -not [string]::IsNullOrWhiteSpace($RemoteValidationTenantSlug)
        }
        limitations = @(
            'Q-053 blocks real-person production legal/GDPR/DPA claims; this evidence is engineering proof only.',
            'Q-054 blocks outbound operational-notification email routing and claims that operational events are emailed.',
            'Remote origins are recorded as configured/not configured only; raw remote origins are intentionally omitted.',
            'Remote VPS deployment proof requires owner-supplied origins and a remote preflight run.'
        )
    }

    $json = $evidence | ConvertTo-Json -Depth 8
    Set-Content -Path $EvidencePath -Value $json -Encoding utf8
    Write-Host ''
    Write-Host "Release evidence written to $EvidencePath"
}

function Invoke-ReleaseEvidenceVerifier {
    if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
        return
    }

    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        'deploy/staging/verify-release-evidence.ps1',
        '-EvidencePath',
        $EvidencePath)

    $liveProductSpineEvidenceExpected = -not $SkipLiveSmoke
    if ($liveProductSpineEvidenceExpected) {
        $arguments += '-RequireProductSpineEvidence'
    }

    $backupRestoreEvidenceExpected = (-not $SkipLiveSmoke) -and (-not $SkipBackupRestoreSmoke)
    if ($backupRestoreEvidenceExpected) {
        $arguments += '-RequireBackupRestoreEvidence'
    }

    $remotePreflightExpected = Test-RemotePreflightExpected
    if ($remotePreflightExpected) {
        $arguments += '-RequireRemotePreflight'
    }

    Invoke-CommandLine `
        -Display 'powershell -NoProfile -ExecutionPolicy Bypass -File deploy/staging/verify-release-evidence.ps1 -EvidencePath <release-evidence>' `
        -FilePath 'powershell' `
        -Arguments $arguments
}

function Invoke-CommandLine {
    param(
        [string]$Display,
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$WorkingDirectory = ''
    )

    if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        Push-Location $WorkingDirectory
    }

    try {
        Write-Host "> $Display"
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code ${LASTEXITCODE}: $Display"
        }
    }
    finally {
        if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) {
            Pop-Location
        }
    }
}

function Invoke-WebNpmCommandLine {
    param(
        [string]$Display,
        [string[]]$Arguments
    )

    $originalPath = $env:Path
    $nodeDirectory = Split-Path (Get-Command 'node' -ErrorAction Stop).Source -Parent
    $npmDirectory = Split-Path (Get-Command 'npm' -ErrorAction Stop).Source -Parent
    $pathParts = @(
        $nodeDirectory,
        $npmDirectory,
        (Join-Path $env:SystemRoot 'system32'),
        $env:SystemRoot,
        (Join-Path $env:SystemRoot 'System32\Wbem'),
        (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\')
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    try {
        $env:Path = $pathParts -join [IO.Path]::PathSeparator
        Invoke-CommandLine `
            -Display $Display `
            -FilePath 'npm' `
            -Arguments $Arguments `
            -WorkingDirectory 'apps/web'
    }
    finally {
        $env:Path = $originalPath
    }
}

function Invoke-WithLocalDevAuthEnvironment {
    param([scriptblock]$Action)

    $previousDevAuth = $env:Authentication__Dev__Enabled
    $previousPublicDevAuth = $env:PUBLIC_DEV_AUTH_ENABLED
    $previousOidcInteractive = $env:Authentication__Oidc__InteractiveEnabled

    try {
        $env:Authentication__Dev__Enabled = 'true'
        $env:PUBLIC_DEV_AUTH_ENABLED = 'true'
        $env:Authentication__Oidc__InteractiveEnabled = 'false'
        & $Action
    }
    finally {
        if ($null -eq $previousDevAuth) {
            Remove-Item Env:\Authentication__Dev__Enabled -ErrorAction SilentlyContinue
        }
        else {
            $env:Authentication__Dev__Enabled = $previousDevAuth
        }

        if ($null -eq $previousPublicDevAuth) {
            Remove-Item Env:\PUBLIC_DEV_AUTH_ENABLED -ErrorAction SilentlyContinue
        }
        else {
            $env:PUBLIC_DEV_AUTH_ENABLED = $previousPublicDevAuth
        }

        if ($null -eq $previousOidcInteractive) {
            Remove-Item Env:\Authentication__Oidc__InteractiveEnabled -ErrorAction SilentlyContinue
        }
        else {
            $env:Authentication__Oidc__InteractiveEnabled = $previousOidcInteractive
        }
    }
}

function Invoke-WithPostgresIntegrationTestEnvironment {
    param([scriptblock]$Action)

    $previousRunPostgresIntegrationTests = $env:RUN_POSTGRES_INTEGRATION_TESTS

    try {
        $env:RUN_POSTGRES_INTEGRATION_TESTS = '1'
        & $Action
    }
    finally {
        if ($null -eq $previousRunPostgresIntegrationTests) {
            Remove-Item Env:\RUN_POSTGRES_INTEGRATION_TESTS -ErrorAction SilentlyContinue
        }
        else {
            $env:RUN_POSTGRES_INTEGRATION_TESTS = $previousRunPostgresIntegrationTests
        }
    }
}

Push-Location $repoRoot
try {
    Invoke-ReleaseStep 'Build solution' {
        Invoke-CommandLine `
            -Display 'dotnet build --no-restore' `
            -FilePath 'dotnet' `
            -Arguments @('build', '--no-restore')
    }

    Invoke-ReleaseStep 'Run deployment package static tests' {
        Invoke-CommandLine `
            -Display 'dotnet test tests/Platform.UnitTests/Platform.UnitTests.csproj --no-restore --filter FullyQualifiedName~StagingWorkerDeploymentPackageTests' `
            -FilePath 'dotnet' `
            -Arguments @(
                'test',
                'tests/Platform.UnitTests/Platform.UnitTests.csproj',
                '--no-restore',
                '--filter',
                'FullyQualifiedName~StagingWorkerDeploymentPackageTests')
    }

    if (-not $SkipEmailRecoveryRegression) {
        Invoke-ReleaseStep 'Run campaign email failed-delivery recovery regression' {
            Invoke-WithPostgresIntegrationTestEnvironment {
                Invoke-CommandLine `
                    -Display 'RUN_POSTGRES_INTEGRATION_TESTS=1 dotnet test tests/Platform.IntegrationTests/Platform.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~Email_delivery_requeues_failed_invitations_for_retry_without_retrying_withdrawal_scrubbed' `
                    -FilePath 'dotnet' `
                    -Arguments @(
                        'test',
                        'tests/Platform.IntegrationTests/Platform.IntegrationTests.csproj',
                        '--no-restore',
                        '--filter',
                        'FullyQualifiedName~Email_delivery_requeues_failed_invitations_for_retry_without_retrying_withdrawal_scrubbed')
            }
        }
    }
    else {
        Add-ReleaseSkip `
            -Name 'Run campaign email failed-delivery recovery regression' `
            -Reason 'Campaign email failed-delivery recovery regression skipped by -SkipEmailRecoveryRegression.' `
            -Flag 'SkipEmailRecoveryRegression'
    }

    if (-not $SkipRetentionAutomationRegression) {
        Invoke-ReleaseStep 'Run retention due-batch automation regression' {
            Invoke-WithPostgresIntegrationTestEnvironment {
                Invoke-CommandLine `
                    -Display 'RUN_POSTGRES_INTEGRATION_TESTS=1 dotnet test tests/Platform.IntegrationTests/Platform.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~Due_batch_automation' `
                    -FilePath 'dotnet' `
                    -Arguments @(
                        'test',
                        'tests/Platform.IntegrationTests/Platform.IntegrationTests.csproj',
                        '--no-restore',
                        '--filter',
                        'FullyQualifiedName~Due_batch_automation')
            }
        }
    }
    else {
        Add-ReleaseSkip `
            -Name 'Run retention due-batch automation regression' `
            -Reason 'Retention due-batch automation regression skipped by -SkipRetentionAutomationRegression.' `
            -Flag 'SkipRetentionAutomationRegression'
    }

    if (-not $SkipNodePackageTests) {
        Invoke-ReleaseStep 'Run Node deployment package tests' {
            Invoke-CommandLine `
                -Display 'node --test tests/deployment-package/*.test.mjs' `
                -FilePath 'node' `
                -Arguments @('--test', 'tests/deployment-package/*.test.mjs')
        }
    }
    else {
        Add-ReleaseSkip `
            -Name 'Run Node deployment package tests' `
            -Reason 'Node deployment package tests skipped by -SkipNodePackageTests.' `
            -Flag 'SkipNodePackageTests'
    }

    if (-not $SkipWebBuild) {
        Invoke-ReleaseStep 'Install web app dependencies' {
            Invoke-WebNpmCommandLine `
                -Display 'npm ci' `
                -Arguments @('ci')
        }

        if (-not $SkipWebAudit) {
            Invoke-ReleaseStep 'Audit web production dependencies' {
                Invoke-WebNpmCommandLine `
                    -Display 'npm audit --omit=dev --audit-level=high' `
                    -Arguments @('audit', '--omit=dev', '--audit-level=high')
            }
        }
        else {
            Add-ReleaseSkip `
                -Name 'Audit web production dependencies' `
                -Reason 'Web production dependency audit skipped by -SkipWebAudit.' `
                -Flag 'SkipWebAudit'
        }

        Invoke-ReleaseStep 'Check web app' {
            Invoke-WebNpmCommandLine `
                -Display 'npm run check' `
                -Arguments @('run', 'check')
        }

        Invoke-ReleaseStep 'Build web app' {
            Invoke-WebNpmCommandLine `
                -Display 'npm run build' `
                -Arguments @('run', 'build')
        }

        if (-not $SkipWebBundleCheck) {
            Invoke-ReleaseStep 'Check web bundle budgets' {
                Invoke-WebNpmCommandLine `
                    -Display 'npm run check:bundles' `
                    -Arguments @('run', 'check:bundles')
            }
        }
        else {
            Add-ReleaseSkip `
                -Name 'Check web bundle budgets' `
                -Reason 'Web bundle budget check skipped by -SkipWebBundleCheck.' `
                -Flag 'SkipWebBundleCheck'
        }
    }
    else {
        Add-ReleaseSkip `
            -Name 'Web app check/build' `
            -Reason 'Web app check/build skipped by -SkipWebBuild.' `
            -Flag 'SkipWebBuild'
    }

    if (-not $SkipDockerConfig) {
        Invoke-ReleaseStep 'Render local staging Compose config' {
            Invoke-CommandLine `
                -Display 'docker compose --env-file deploy/staging/env.example -f deploy/staging/docker-compose.yml config' `
                -FilePath 'docker' `
                -Arguments @(
                    'compose',
                    '--env-file',
                    'deploy/staging/env.example',
                    '-f',
                    'deploy/staging/docker-compose.yml',
                    'config')
        }

        if (Test-Path 'deploy/staging/docker-compose.vps.yml') {
            Invoke-ReleaseStep 'Render VPS staging Compose config' {
                Invoke-CommandLine `
                    -Display 'docker compose --env-file deploy/staging/vps.env.example -f deploy/staging/docker-compose.yml -f deploy/staging/docker-compose.vps.yml config' `
                    -FilePath 'docker' `
                    -Arguments @(
                        'compose',
                        '--env-file',
                        'deploy/staging/vps.env.example',
                        '-f',
                        'deploy/staging/docker-compose.yml',
                        '-f',
                        'deploy/staging/docker-compose.vps.yml',
                        'config')
            }
        }
    }
    else {
        Add-ReleaseSkip `
            -Name 'Docker Compose config rendering' `
            -Reason 'Docker Compose config rendering skipped by -SkipDockerConfig.' `
            -Flag 'SkipDockerConfig'
    }

    if (-not $SkipLiveSmoke) {
        if (-not $SkipLocalStagingRefresh) {
            Invoke-ReleaseStep 'Refresh local staging services' {
                Invoke-WithLocalDevAuthEnvironment {
                    Invoke-CommandLine `
                        -Display 'docker compose --env-file deploy/staging/.env -f deploy/staging/docker-compose.yml up -d --build api worker web' `
                        -FilePath 'docker' `
                        -Arguments @(
                            'compose',
                            '--env-file',
                            'deploy/staging/.env',
                            '-f',
                            'deploy/staging/docker-compose.yml',
                            'up',
                            '-d',
                            '--build',
                            'api',
                            'worker',
                            'web')
                }
            }
        }
        else {
            Add-ReleaseSkip `
                -Name 'Refresh local staging services' `
                -Reason 'Local staging service refresh skipped by -SkipLocalStagingRefresh.' `
                -Flag 'SkipLocalStagingRefresh'
        }

        Invoke-ReleaseStep 'Run local staging smoke' {
            Invoke-CommandLine `
                -Display 'powershell -NoProfile -ExecutionPolicy Bypass -File deploy/staging/smoke-local-staging.ps1' `
                -FilePath 'powershell' `
                -Arguments @(
                    '-NoProfile',
                    '-ExecutionPolicy',
                    'Bypass',
                    '-File',
                    'deploy/staging/smoke-local-staging.ps1')
        }

        Invoke-ReleaseStep 'Run product-spine smoke' {
            $productSpineSmokeArguments = @(
                '-NoProfile',
                '-ExecutionPolicy',
                'Bypass',
                '-File',
                'deploy/staging/smoke-product-spine.ps1')
            $productSpineEvidencePath = Get-ProductSpineEvidencePath
            if (-not [string]::IsNullOrWhiteSpace($productSpineEvidencePath)) {
                $productSpineSmokeArguments += @(
                    '-EvidencePath',
                    $productSpineEvidencePath)
            }

            Invoke-CommandLine `
                -Display 'powershell -NoProfile -ExecutionPolicy Bypass -File deploy/staging/smoke-product-spine.ps1' `
                -FilePath 'powershell' `
                -Arguments $productSpineSmokeArguments
        }

        if (-not $SkipBackupRestoreSmoke) {
            Invoke-ReleaseStep 'Run backup/restore smoke' {
                $backupRestoreSmokeArguments = @(
                    '-NoProfile',
                    '-ExecutionPolicy',
                    'Bypass',
                    '-File',
                    'deploy/staging/backup-restore-smoke.ps1')
                $backupRestoreEvidencePath = Get-BackupRestoreEvidencePath
                if (-not [string]::IsNullOrWhiteSpace($backupRestoreEvidencePath)) {
                    $backupRestoreSmokeArguments += @(
                        '-EvidencePath',
                        $backupRestoreEvidencePath)
                }

                Invoke-CommandLine `
                    -Display 'powershell -NoProfile -ExecutionPolicy Bypass -File deploy/staging/backup-restore-smoke.ps1' `
                    -FilePath 'powershell' `
                    -Arguments $backupRestoreSmokeArguments
            }
        }
        else {
            Add-ReleaseSkip `
                -Name 'Run backup/restore smoke' `
                -Reason 'Backup/restore smoke skipped by -SkipBackupRestoreSmoke.' `
                -Flag 'SkipBackupRestoreSmoke'
        }
    }
    else {
        Add-ReleaseSkip `
            -Name 'Live staging smokes' `
            -Reason 'Live staging smokes skipped by -SkipLiveSmoke.' `
            -Flag 'SkipLiveSmoke'
    }

    if ($SkipRemotePreflight) {
        Add-ReleaseSkip `
            -Name 'Run remote validation preflight' `
            -Reason 'Remote validation preflight skipped by -SkipRemotePreflight.' `
            -Flag 'SkipRemotePreflight'
    }
    elseif ([string]::IsNullOrWhiteSpace($RemoteApiOrigin) -and [string]::IsNullOrWhiteSpace($RemoteWebOrigin)) {
        Add-ReleaseSkip `
            -Name 'Run remote validation preflight' `
            -Reason 'Remote validation preflight not configured. Pass -RemoteApiOrigin and -RemoteWebOrigin to run it.'
    }
    elseif ([string]::IsNullOrWhiteSpace($RemoteApiOrigin) -or [string]::IsNullOrWhiteSpace($RemoteWebOrigin)) {
        throw 'Remote validation preflight requires both -RemoteApiOrigin and -RemoteWebOrigin.'
    }
    else {
        Invoke-ReleaseStep 'Run remote validation preflight' {
            $remotePreflightArguments = @(
                '-NoProfile',
                '-ExecutionPolicy',
                'Bypass',
                '-File',
                'deploy/staging/smoke-validation-demo-preflight.ps1',
                $RemoteValidationTenantSlug,
                '-RemoteOnly',
                '-ApiOrigin',
                $RemoteApiOrigin,
                '-WebOrigin',
                $RemoteWebOrigin)
            $remotePreflightEvidencePath = Get-RemotePreflightEvidencePath
            if (-not [string]::IsNullOrWhiteSpace($remotePreflightEvidencePath)) {
                $remotePreflightArguments += @(
                    '-EvidencePath',
                    $remotePreflightEvidencePath)
            }

            Invoke-CommandLine `
                -Display "powershell -NoProfile -ExecutionPolicy Bypass -File deploy/staging/smoke-validation-demo-preflight.ps1 $RemoteValidationTenantSlug -RemoteOnly -ApiOrigin $RemoteApiOrigin -WebOrigin $RemoteWebOrigin" `
                -FilePath 'powershell' `
                -Arguments $remotePreflightArguments
        }
    }

    Write-ReleaseEvidence
    Invoke-ReleaseEvidenceVerifier

    Write-Host ''
    Write-Host 'Staging release checks passed.'
}
finally {
    Pop-Location
}
