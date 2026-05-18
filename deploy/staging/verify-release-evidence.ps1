param(
    [Parameter(Mandatory = $true)]
    [string]$EvidencePath,
    [switch]$RequireProductSpineEvidence,
    [switch]$RequireBackupRestoreEvidence,
    [switch]$RequireRemotePreflight
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ForbiddenMarkers = @(
    'rawToken',
    'wdr_',
    'storageKey',
    'ConnectionStrings',
    'Password',
    'Secret',
    'participantCode',
    'rawAnswers',
    'rawParticipant'
)

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-EvidenceJson {
    param([string]$Path)

    Assert-True (Test-Path -LiteralPath $Path) "Evidence file was not found: $Path"
    $raw = Get-Content -LiteralPath $Path -Raw
    Assert-True (-not [string]::IsNullOrWhiteSpace($raw)) "Evidence file is empty: $Path"

    return [pscustomobject]@{
        Raw = $raw
        Json = $raw | ConvertFrom-Json
    }
}

function Assert-JsonContains {
    param(
        [string]$Raw,
        [string]$Needle,
        [string]$Context
    )

    Assert-True ($Raw.Contains($Needle)) "$Context did not include required marker '$Needle'."
}

function Assert-NoForbiddenMarkers {
    param(
        [string]$Raw,
        [string]$Context
    )

    foreach ($marker in $ForbiddenMarkers) {
        Assert-True (-not $Raw.Contains($marker)) "$Context contains forbidden marker '$marker'."
    }
}

function Get-SafeFileHash {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Resolve-SidecarPath {
    param(
        [string]$ReleaseEvidencePath,
        [string]$SidecarPath
    )

    if ([string]::IsNullOrWhiteSpace($SidecarPath)) {
        return ''
    }

    if ([IO.Path]::IsPathRooted($SidecarPath)) {
        return $SidecarPath
    }

    $releaseDirectory = Split-Path -Path $ReleaseEvidencePath -Parent
    if ([string]::IsNullOrWhiteSpace($releaseDirectory)) {
        return $SidecarPath
    }

    return Join-Path $releaseDirectory $SidecarPath
}

function Assert-EvidenceArtifactHash {
    param(
        [object]$ReleaseEvidence,
        [string]$ArtifactName,
        [string]$ResolvedSidecarPath,
        [bool]$Required
    )

    Assert-True ($null -ne $ReleaseEvidence.evidenceArtifacts) 'Release evidence must include evidenceArtifacts.'

    $artifactProperty = $ReleaseEvidence.evidenceArtifacts.PSObject.Properties[$ArtifactName]
    Assert-True ($null -ne $artifactProperty) "Release evidence evidenceArtifacts must include $ArtifactName."
    $artifact = $artifactProperty.Value

    Assert-True ($artifact.name -eq $ArtifactName) "Release evidence $ArtifactName artifact name mismatch."
    Assert-True ($null -ne $artifact.path) "Release evidence $ArtifactName artifact must include path."
    Assert-True ($null -ne $artifact.exists) "Release evidence $ArtifactName artifact must include exists."
    Assert-True ($null -ne $artifact.sha256) "Release evidence $ArtifactName artifact must include sha256."

    $exists = -not [string]::IsNullOrWhiteSpace($ResolvedSidecarPath) -and (Test-Path -LiteralPath $ResolvedSidecarPath)
    Assert-True ($artifact.exists -eq $exists) "Release evidence $ArtifactName artifact exists flag does not match the sidecar file."

    if ($exists) {
        $actualHash = Get-SafeFileHash -Path $ResolvedSidecarPath
        Assert-True (-not [string]::IsNullOrWhiteSpace($artifact.sha256)) "Release evidence $ArtifactName artifact must include sha256 when the sidecar exists."
        Assert-True ($artifact.sha256 -eq $actualHash) "Release evidence $ArtifactName sidecar hash mismatch."
        return $true
    }

    Assert-True ([string]::IsNullOrWhiteSpace($artifact.sha256)) "Release evidence $ArtifactName artifact must not include sha256 when the sidecar is absent."
    if ($Required) {
        throw "Release evidence $ArtifactName sidecar is required but was not found."
    }

    return $false
}

function Assert-ReleaseEvidence {
    param(
        [object]$Evidence,
        [string]$Raw
    )

    Assert-True ($Evidence.schemaVersion -eq 1) 'Release evidence schemaVersion must be 1.'
    Assert-True ($Evidence.status -eq 'passed') "Release evidence status must be passed, got '$($Evidence.status)'."
    Assert-True ($Evidence.runner -eq 'deploy/staging/run-release-checks.ps1') "Unexpected release evidence runner '$($Evidence.runner)'."
    Assert-True (@($Evidence.passedGates).Count -gt 0) 'Release evidence must include passedGates.'
    Assert-True ($null -ne $Evidence.skippedGates) 'Release evidence must include skippedGates.'
    Assert-True ($null -ne $Evidence.inputs.remoteApiOriginConfigured) 'Release evidence must include remoteApiOriginConfigured.'
    Assert-True ($null -ne $Evidence.inputs.remoteWebOriginConfigured) 'Release evidence must include remoteWebOriginConfigured.'
    Assert-True ($null -ne $Evidence.proofScope) 'Release evidence must include proofScope.'
    Assert-True ($null -ne $Evidence.claimBoundary) 'Release evidence must include claimBoundary.'
    Assert-True ($null -ne $Evidence.evidenceArtifacts) 'Release evidence must include evidenceArtifacts.'

    $localDefaultStagingPassed = @($Evidence.passedGates | Where-Object { $_.name -eq 'Run local staging smoke' }).Count -gt 0
    $productSpinePassed = @($Evidence.passedGates | Where-Object { $_.name -eq 'Run product-spine smoke' }).Count -gt 0
    $backupRestorePassed = @($Evidence.passedGates | Where-Object { $_.name -eq 'Run backup/restore smoke' }).Count -gt 0
    $campaignEmailRecoveryRegressionPassed = @($Evidence.passedGates | Where-Object { $_.name -eq 'Run campaign email failed-delivery recovery regression' }).Count -gt 0
    $retentionDueBatchAutomationRegressionPassed = @($Evidence.passedGates | Where-Object { $_.name -eq 'Run retention due-batch automation regression' }).Count -gt 0
    $remotePreflightPassed = @($Evidence.passedGates | Where-Object { $_.name -eq 'Run remote validation preflight' }).Count -gt 0

    Assert-True ($Evidence.proofScope.localDefaultStagingProven -eq $localDefaultStagingPassed) 'Release evidence proofScope.localDefaultStagingProven must match the local staging smoke gate.'
    Assert-True ($Evidence.proofScope.productSpineProven -eq $productSpinePassed) 'Release evidence proofScope.productSpineProven must match the product-spine smoke gate.'
    Assert-True ($Evidence.proofScope.backupRestoreProven -eq $backupRestorePassed) 'Release evidence proofScope.backupRestoreProven must match the backup/restore smoke gate.'
    Assert-True ($Evidence.proofScope.campaignFailedDeliveryRecoveryRegressionProven -eq $campaignEmailRecoveryRegressionPassed) 'Release evidence proofScope.campaignFailedDeliveryRecoveryRegressionProven must match the campaign email failed-delivery recovery regression gate.'
    Assert-True ($Evidence.proofScope.retentionDueBatchAutomationRegressionProven -eq $retentionDueBatchAutomationRegressionPassed) 'Release evidence proofScope.retentionDueBatchAutomationRegressionProven must match the retention due-batch automation regression gate.'
    Assert-True ($Evidence.proofScope.remotePreflightProven -eq $remotePreflightPassed) 'Release evidence proofScope.remotePreflightProven must match the remote validation preflight gate.'
    Assert-True ($Evidence.proofScope.remoteVpsDeploymentProven -eq $false) 'Release evidence must not claim remote VPS deployment proof.'
    Assert-True ($Evidence.proofScope.realPersonLegalUseApproved -eq $false) 'Release evidence must not claim real-person legal/GDPR approval.'
    Assert-True ($Evidence.proofScope.outboundOperationalNotificationEmailProven -eq $false) 'Release evidence must not claim outbound operational-notification email proof.'
    Assert-True ($Evidence.claimBoundary.engineeringEvidenceOnly -eq $true) 'Release evidence claimBoundary.engineeringEvidenceOnly must be true.'
    Assert-True ($Evidence.claimBoundary.remoteProofRequiresOwnerOrigins -eq $true) 'Release evidence claimBoundary.remoteProofRequiresOwnerOrigins must be true.'
    Assert-True ($Evidence.claimBoundary.q053BlocksRealPersonLegalClaims -eq $true) 'Release evidence claimBoundary.q053BlocksRealPersonLegalClaims must be true.'
    Assert-True ($Evidence.claimBoundary.q054BlocksOperationalNotificationEmailClaims -eq $true) 'Release evidence claimBoundary.q054BlocksOperationalNotificationEmailClaims must be true.'
    Assert-JsonContains $Raw 'Q-053' 'Release evidence'
    Assert-JsonContains $Raw 'Q-054' 'Release evidence'
    Assert-NoForbiddenMarkers $Raw 'Release evidence'

    if ($RequireRemotePreflight) {
        $remotePassed = @($Evidence.passedGates | Where-Object { $_.name -eq 'Run remote validation preflight' }).Count -gt 0
        $remoteSkipped = @($Evidence.skippedGates | Where-Object { $_.name -eq 'Run remote validation preflight' }).Count -gt 0
        Assert-True $remotePassed 'Release evidence does not prove the remote validation preflight gate passed.'
        Assert-True (-not $remoteSkipped) 'Release evidence still records remote validation preflight as skipped.'
    }
}

function Assert-ProductSpineEvidence {
    param(
        [object]$Evidence,
        [string]$Raw
    )

    Assert-True ($Evidence.schemaVersion -eq 1) 'Product-spine evidence schemaVersion must be 1.'
    Assert-True ($Evidence.status -eq 'passed') "Product-spine evidence status must be passed, got '$($Evidence.status)'."
    Assert-True ($Evidence.runner -eq 'deploy/staging/smoke-product-spine.ps1') "Unexpected product-spine evidence runner '$($Evidence.runner)'."
    Assert-True (@($Evidence.ownerInspectionRoutes).Count -ge 5) 'Product-spine evidence must include ownerInspectionRoutes.'
    Assert-True ($Evidence.productMilestones.closedWaveFinalityProven -eq $true) 'Product-spine evidence must prove closed-wave finality.'
    Assert-True ($Evidence.productMilestones.postWithdrawalReportPdfRegenerationProven -eq $true) 'Product-spine evidence must prove post-withdrawal report PDF regeneration.'
    Assert-True ($Evidence.productMilestones.campaignInvitationDeliveryProven -eq $true) 'Product-spine evidence must prove campaign invitation delivery.'
    Assert-True (-not [string]::IsNullOrWhiteSpace($Evidence.artifactProofs.postWithdrawalReportPdfArtifact.id)) 'Product-spine evidence must include postWithdrawalReportPdfArtifact proof.'
    Assert-True ($Evidence.withdrawalProof.reviewVisibilityProven -eq $true) 'Product-spine evidence must prove withdrawal review visibility.'
    Assert-True ($Evidence.withdrawalProof.terminalNotificationProven -eq $true) 'Product-spine evidence must prove withdrawal terminal notification.'
    Assert-True ($null -ne $Evidence.operationalNotificationProof) 'Product-spine evidence must include operationalNotificationProof.'
    Assert-True ($Evidence.operationalNotificationProof.terminalNotificationProven -eq $true) 'Product-spine evidence must prove terminal operational notification.'
    Assert-True ($Evidence.operationalNotificationProof.summaryProven -eq $true) 'Product-spine evidence must prove operational notification summary.'
    Assert-True ($Evidence.operationalNotificationProof.markReadProven -eq $true) 'Product-spine evidence must prove operational notification mark-read.'
    Assert-True ($Evidence.operationalNotificationProof.markAllReadProven -eq $true) 'Product-spine evidence must prove operational notification mark-all-read.'
    Assert-True ($Evidence.operationalNotificationProof.inAppOnly -eq $true) 'Product-spine evidence must state operational notifications are in-app only.'
    Assert-True ($Evidence.operationalNotificationProof.emailRoutingProven -eq $false) 'Product-spine evidence must not claim operational notification email routing.'
    Assert-True ([int]$Evidence.operationalNotificationProof.unreadAfterMarkAllRead -eq 0) 'Product-spine evidence must prove mark-all-read clears unread notifications.'
    Assert-True ([int]$Evidence.operationalNotificationProof.markAllReadMarkedReadCount -ge 0) 'Product-spine evidence must include markAllReadMarkedReadCount.'
    Assert-True ($null -ne $Evidence.reportExportProof) 'Product-spine evidence must include reportExportProof.'
    Assert-True ($Evidence.reportExportProof.scoreMetadataProven -eq $true) 'Product-spine evidence must prove score metadata.'
    Assert-True ($Evidence.reportExportProof.reportExportCodebookMetadataProven -eq $true) 'Product-spine evidence must prove report export codebook metadata.'
    Assert-True ($Evidence.reportExportProof.responseExportScoreMetadataProven -eq $true) 'Product-spine evidence must prove response export score metadata.'
    Assert-True ($Evidence.reportExportProof.waveComparisonScoreMetadataProven -eq $true) 'Product-spine evidence must prove wave comparison score metadata.'
    Assert-True ($Evidence.reportExportProof.reportPdfDeliveryChecked -eq $true) 'Product-spine evidence must prove report PDF delivery check.'
    Assert-True ($Evidence.reportExportProof.signedDownloadUrlChecked -eq $true) 'Product-spine evidence must prove signed download URL check.'
    Assert-True ($Evidence.reportExportProof.postWithdrawalExportRegenerationProven -eq $true) 'Product-spine evidence must prove post-withdrawal export regeneration.'
    Assert-True ($Evidence.reportExportProof.postWithdrawalReportPdfRegenerationProven -eq $true) 'Product-spine evidence must prove post-withdrawal report PDF regeneration.'
    Assert-True ($Evidence.reportExportProof.artifactLeakChecksProven -eq $true) 'Product-spine evidence must prove artifact leak checks.'
    Assert-True ($null -ne $Evidence.campaignEmailDeliveryProof) 'Product-spine evidence must include campaignEmailDeliveryProof.'
    Assert-True ($Evidence.campaignEmailDeliveryProof.invitationBatchProven -eq $true) 'Product-spine evidence must prove campaign email invitation batching.'
    Assert-True ($Evidence.campaignEmailDeliveryProof.deliveryProcessingProven -eq $true) 'Product-spine evidence must prove campaign email delivery processing.'
    Assert-True ($Evidence.campaignEmailDeliveryProof.provider -eq 'local-dev') 'Product-spine evidence must prove local-dev campaign email provider.'
    Assert-True ($Evidence.campaignEmailDeliveryProof.localDevProviderProven -eq $true) 'Product-spine evidence must prove local-dev campaign email provider.'
    Assert-True ([int]$Evidence.campaignEmailDeliveryProof.createdInvitationCount -eq 1) 'Product-spine evidence must prove one campaign email invitation was created.'
    Assert-True ([int]$Evidence.campaignEmailDeliveryProof.processedCount -eq 1) 'Product-spine evidence must prove one campaign email delivery was processed.'
    Assert-True ([int]$Evidence.campaignEmailDeliveryProof.sentCount -eq 1) 'Product-spine evidence must prove one campaign email delivery was sent.'
    Assert-True ([int]$Evidence.campaignEmailDeliveryProof.failedCount -eq 0) 'Product-spine evidence must prove zero failed campaign email deliveries.'
    Assert-True ($Evidence.campaignEmailDeliveryProof.failedRequeueNoopProven -eq $true) 'Product-spine evidence must prove failed campaign email requeue no-op.'
    Assert-True ([int]$Evidence.campaignEmailDeliveryProof.failedRequeueNoopRequeuedCount -eq 0) 'Product-spine evidence must prove failed campaign email requeue no-op returned zero.'
    Assert-True ($Evidence.campaignEmailDeliveryProof.smtpDeliveryProven -eq $false) 'Product-spine evidence must not claim SMTP campaign email delivery.'
    Assert-True ($Evidence.campaignEmailDeliveryProof.failedRequeueRecoveryProven -eq $false) 'Product-spine evidence must not claim failed campaign email requeue recovery.'
    Assert-JsonContains $Raw 'Q-053' 'Product-spine evidence'
    Assert-JsonContains $Raw 'Q-054' 'Product-spine evidence'
    Assert-NoForbiddenMarkers $Raw 'Product-spine evidence'
}

function Assert-BackupRestoreEvidence {
    param(
        [object]$Evidence,
        [string]$Raw
    )

    Assert-True ($Evidence.schemaVersion -eq 1) 'Backup/restore evidence schemaVersion must be 1.'
    Assert-True ($Evidence.status -eq 'passed') "Backup/restore evidence status must be passed, got '$($Evidence.status)'."
    Assert-True ($Evidence.runner -eq 'deploy/staging/backup-restore-smoke.ps1') "Unexpected backup/restore evidence runner '$($Evidence.runner)'."
    Assert-True ([int64]$Evidence.backup.backupBytes -gt 0) 'Backup/restore evidence must include positive backupBytes.'
    Assert-True ($Evidence.backup.backupSha256 -match '^[a-f0-9]{64}$') 'Backup/restore evidence must include backupSha256.'
    Assert-True ([int]$Evidence.restore.restorePublicTableCount -gt 0) 'Backup/restore evidence must include positive restorePublicTableCount.'
    Assert-True ([int]$Evidence.restore.restorePublicRelationCount -gt 0) 'Backup/restore evidence must include positive restorePublicRelationCount.'
    Assert-True ($Evidence.restore.requiredPlatformTablesPresent -eq $true) 'Backup/restore evidence must prove requiredPlatformTablesPresent.'
    Assert-True ($null -ne $Evidence.restore.restoreVolumeKept) 'Backup/restore evidence must include restoreVolumeKept.'
    Assert-JsonContains $Raw 'Q-053' 'Backup/restore evidence'
    Assert-NoForbiddenMarkers $Raw 'Backup/restore evidence'
}

function Assert-RemotePreflightEvidence {
    param(
        [object]$Evidence,
        [string]$Raw
    )

    Assert-True ($Evidence.schemaVersion -eq 1) 'Remote preflight evidence schemaVersion must be 1.'
    Assert-True ($Evidence.status -eq 'passed') "Remote preflight evidence status must be passed, got '$($Evidence.status)'."
    Assert-True ($Evidence.runner -eq 'deploy/staging/smoke-validation-demo-preflight.ps1') "Unexpected remote preflight evidence runner '$($Evidence.runner)'."
    Assert-True (-not [string]::IsNullOrWhiteSpace($Evidence.validationTenantSlug)) 'Remote preflight evidence must include validationTenantSlug.'
    Assert-True ($null -ne $Evidence.inputs.apiOriginConfigured) 'Remote preflight evidence must include apiOriginConfigured.'
    Assert-True ($null -ne $Evidence.inputs.webOriginConfigured) 'Remote preflight evidence must include webOriginConfigured.'
    Assert-True ($Evidence.remotePreflightChecks.apiHealth -eq 'passed') 'Remote preflight evidence must prove apiHealth.'
    Assert-True ($Evidence.remotePreflightChecks.webApp -eq 'passed') 'Remote preflight evidence must prove webApp.'
    Assert-True ($Evidence.remotePreflightChecks.authSessionCors -eq 'passed') 'Remote preflight evidence must prove authSessionCors.'
    Assert-True ($Evidence.remotePreflightChecks.loginRedirect -eq 'passed') 'Remote preflight evidence must prove loginRedirect.'
    Assert-JsonContains $Raw 'Q-053' 'Remote preflight evidence'
    Assert-JsonContains $Raw 'Q-054' 'Remote preflight evidence'
    Assert-NoForbiddenMarkers $Raw 'Remote preflight evidence'
}

$releaseEvidencePath = Resolve-Path -LiteralPath $EvidencePath
$releaseEvidence = Read-EvidenceJson -Path $releaseEvidencePath
Assert-ReleaseEvidence -Evidence $releaseEvidence.Json -Raw $releaseEvidence.Raw

$productSpineEvidencePath = Resolve-SidecarPath `
    -ReleaseEvidencePath $releaseEvidencePath `
    -SidecarPath ([string]$releaseEvidence.Json.productSpineEvidencePath)

$productSpineHashValidated = Assert-EvidenceArtifactHash `
    -ReleaseEvidence $releaseEvidence.Json `
    -ArtifactName 'productSpine' `
    -ResolvedSidecarPath $productSpineEvidencePath `
    -Required ([bool]$RequireProductSpineEvidence)

$productSpineEvidenceValidated = $false
if (-not [string]::IsNullOrWhiteSpace($productSpineEvidencePath) -and (Test-Path -LiteralPath $productSpineEvidencePath)) {
    $productSpineEvidence = Read-EvidenceJson -Path $productSpineEvidencePath
    Assert-ProductSpineEvidence -Evidence $productSpineEvidence.Json -Raw $productSpineEvidence.Raw
    $productSpineEvidenceValidated = $true
}
elseif ($RequireProductSpineEvidence) {
    throw "Product-spine evidence sidecar is required but was not found: $productSpineEvidencePath"
}

$backupRestoreEvidencePath = Resolve-SidecarPath `
    -ReleaseEvidencePath $releaseEvidencePath `
    -SidecarPath ([string]$releaseEvidence.Json.backupRestoreEvidencePath)

$backupRestoreHashValidated = Assert-EvidenceArtifactHash `
    -ReleaseEvidence $releaseEvidence.Json `
    -ArtifactName 'backupRestore' `
    -ResolvedSidecarPath $backupRestoreEvidencePath `
    -Required ([bool]$RequireBackupRestoreEvidence)

$backupRestoreEvidenceValidated = $false
if (-not [string]::IsNullOrWhiteSpace($backupRestoreEvidencePath) -and (Test-Path -LiteralPath $backupRestoreEvidencePath)) {
    $backupRestoreEvidence = Read-EvidenceJson -Path $backupRestoreEvidencePath
    Assert-BackupRestoreEvidence -Evidence $backupRestoreEvidence.Json -Raw $backupRestoreEvidence.Raw
    $backupRestoreEvidenceValidated = $true
}
elseif ($RequireBackupRestoreEvidence) {
    throw "Backup/restore evidence sidecar is required but was not found: $backupRestoreEvidencePath"
}

$remotePreflightEvidencePath = Resolve-SidecarPath `
    -ReleaseEvidencePath $releaseEvidencePath `
    -SidecarPath ([string]$releaseEvidence.Json.remotePreflightEvidencePath)

$remotePreflightHashValidated = Assert-EvidenceArtifactHash `
    -ReleaseEvidence $releaseEvidence.Json `
    -ArtifactName 'remotePreflight' `
    -ResolvedSidecarPath $remotePreflightEvidencePath `
    -Required ([bool]$RequireRemotePreflight)

$remotePreflightEvidenceValidated = $false
if (-not [string]::IsNullOrWhiteSpace($remotePreflightEvidencePath) -and (Test-Path -LiteralPath $remotePreflightEvidencePath)) {
    $remotePreflightEvidence = Read-EvidenceJson -Path $remotePreflightEvidencePath
    Assert-RemotePreflightEvidence -Evidence $remotePreflightEvidence.Json -Raw $remotePreflightEvidence.Raw
    $remotePreflightEvidenceValidated = $true
}
elseif ($RequireRemotePreflight) {
    throw "Remote preflight evidence sidecar is required but was not found: $remotePreflightEvidencePath"
}

Write-Host "Release evidence verification passed. Passed gates: $(@($releaseEvidence.Json.passedGates).Count). Skipped gates: $(@($releaseEvidence.Json.skippedGates).Count). Product-spine evidence validated: $productSpineEvidenceValidated. Backup/restore evidence validated: $backupRestoreEvidenceValidated. Remote preflight evidence validated: $remotePreflightEvidenceValidated. Product-spine hash validated: $productSpineHashValidated. Backup/restore hash validated: $backupRestoreHashValidated. Remote preflight hash validated: $remotePreflightHashValidated. Campaign email failed-delivery recovery regression proven: $($releaseEvidence.Json.proofScope.campaignFailedDeliveryRecoveryRegressionProven). Retention due-batch automation regression proven: $($releaseEvidence.Json.proofScope.retentionDueBatchAutomationRegressionProven)."
