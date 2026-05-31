namespace Platform.UnitTests.Deployment;

public sealed class StagingWorkerDeploymentPackageTests
{
    [Fact]
    public void Worker_dockerfile_publishes_platform_worker_host()
    {
        var dockerfile = ReadRepoFile("deploy/staging/worker.Dockerfile");

        Assert.Contains("dotnet publish src/Platform.Workers/Platform.Workers.csproj", dockerfile);
        Assert.Contains("FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime", dockerfile);
        Assert.Contains("ENTRYPOINT [\"dotnet\", \"Platform.Workers.dll\"]", dockerfile);
    }

    [Fact]
    public void Compose_defines_worker_service_without_public_ports()
    {
        var compose = ReadRepoFile("deploy/staging/docker-compose.yml");
        var worker = ExtractServiceSection(compose, "worker");

        Assert.Contains("dockerfile: deploy/staging/worker.Dockerfile", worker);
        Assert.Contains("DOTNET_ENVIRONMENT: Development", worker);
        Assert.Contains("ConnectionStrings__PlatformDb: Host=postgres;Port=5432;Database=${POSTGRES_DB:-instruments_platform_staging};Username=${POSTGRES_WORKER_USER:-platform_worker};Password=${POSTGRES_WORKER_PASSWORD:-platform_worker_staging}", worker);
        Assert.Contains("OutboxRelay__Enabled: ${OutboxRelay__Enabled:-true}", worker);
        Assert.Contains("WorkerHeartbeat__Enabled: ${WorkerHeartbeat__Enabled:-true}", worker);
        Assert.Contains("RetentionAutomation__Enabled: ${RetentionAutomation__Enabled:-false}", worker);
        Assert.Contains("ReportPdfArtifacts__Enabled: ${ReportPdfArtifacts__Enabled:-false}", worker);
        Assert.Contains("condition: service_completed_successfully", worker);
        Assert.DoesNotContain("ports:", worker);
    }

    [Fact]
    public void Vps_override_sets_worker_to_production_without_ports()
    {
        var compose = ReadRepoFile("deploy/staging/docker-compose.vps.yml");
        var worker = ExtractServiceSection(compose, "worker");

        Assert.Contains("DOTNET_ENVIRONMENT: Production", worker);
        Assert.DoesNotContain("ports:", worker);
    }

    [Fact]
    public void Env_examples_expose_safe_worker_defaults()
    {
        foreach (var path in new[] { "deploy/staging/env.example", "deploy/staging/vps.env.example" })
        {
            var env = ReadRepoFile(path);

            Assert.Contains("POSTGRES_WORKER_USER=platform_worker", env);
            Assert.Contains("POSTGRES_WORKER_PASSWORD=", env);
            Assert.Contains("OutboxRelay__Enabled=true", env);
            Assert.Contains("WorkerHeartbeat__Enabled=true", env);
            Assert.Contains("WorkerHeartbeat__WorkerName=platform-workers", env);
            Assert.Contains("RetentionAutomation__Enabled=false", env);
            Assert.Contains("ReportPdfArtifacts__Enabled=false", env);
        }
    }

    [Fact]
    public void Runtime_role_script_creates_runtime_and_worker_roles_with_operational_grants()
    {
        var script = ReadRepoFile("deploy/staging/runtime-role.sql");

        Assert.Contains("worker_user", script);
        Assert.Contains("worker_password", script);
        Assert.Contains("ALTER ROLE %I LOGIN PASSWORD %L', :'worker_user'", script);
        Assert.Contains("worker_heartbeat", script);
        Assert.Contains("operational_notification", script);
        Assert.Contains("email_template", ExtractReadWriteGrantBlock(script, "runtime_user"));
        Assert.Contains("email_template", ExtractReadWriteGrantBlock(script, "worker_user"));
        Assert.Contains("withdrawal_event", script);
        Assert.Contains("withdrawal_request_token", script);
        Assert.Contains("retention_due_batch", script);
        Assert.Contains("GRANT SELECT, INSERT, UPDATE ON TABLE", script);
        Assert.Contains("TO :\"worker_user\"", script);
    }

    [Fact]
    public void Local_staging_smoke_checks_worker_service_running()
    {
        var script = ReadRepoFile("deploy/staging/smoke-local-staging.ps1");

        Assert.Contains("Assert-ComposeServiceRunning -Service 'worker'", script);
        Assert.Contains("docker compose --env-file $envFile -f $composeFile ps --status running --services", script);
    }

    [Fact]
    public void Local_staging_smokes_explain_dev_auth_requirement()
    {
        var localSmoke = ReadRepoFile("deploy/staging/smoke-local-staging.ps1");
        var productSpineSmoke = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Invoke-AuthenticatedDevSession", localSmoke);
        Assert.Contains("development authentication is disabled", localSmoke);
        Assert.Contains("Authentication__Dev__Enabled=true", localSmoke);
        Assert.Contains("Invoke-AuthenticatedDevSession", productSpineSmoke);
        Assert.Contains("development authentication is disabled", productSpineSmoke);
        Assert.Contains("Authentication__Dev__Enabled=true", productSpineSmoke);
    }

    [Fact]
    public void Local_staging_smoke_waits_for_worker_heartbeat()
    {
        var script = ReadRepoFile("deploy/staging/smoke-local-staging.ps1");

        Assert.Contains("Wait-WorkerHeartbeat -WorkerName $workerHeartbeatName", script);
        Assert.Contains("FROM worker_heartbeat", script);
        Assert.Contains("$safeWorkerName = $WorkerName.Replace(\"'\", \"''\")", script);
        Assert.Contains("WHERE worker_name = '$safeWorkerName'", script);
    }

    [Fact]
    public void Staging_release_check_runner_codifies_release_gates()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("dotnet build --no-restore", script);
        Assert.Contains("StagingWorkerDeploymentPackageTests", script);
        Assert.Contains("node --test tests/deployment-package/*.test.mjs", script);
        Assert.Contains("docker compose --env-file deploy/staging/env.example -f deploy/staging/docker-compose.yml config", script);
        Assert.Contains("smoke-local-staging.ps1", script);
        Assert.Contains("smoke-product-spine.ps1", script);
        Assert.Contains("SkipLiveSmoke", script);
    }

    [Fact]
    public void Staging_release_check_runner_runs_campaign_email_recovery_regression()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("SkipEmailRecoveryRegression", script);
        Assert.Contains("Run campaign email failed-delivery recovery regression", script);
        Assert.Contains("RUN_POSTGRES_INTEGRATION_TESTS", script);
        Assert.Contains("Email_delivery_requeues_failed_invitations_for_retry_without_retrying_withdrawal_scrubbed", script);
        Assert.Contains("tests/Platform.IntegrationTests/Platform.IntegrationTests.csproj", script);
    }

    [Fact]
    public void Staging_release_check_runner_runs_retention_automation_regression()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("SkipRetentionAutomationRegression", script);
        Assert.Contains("Run retention due-batch automation regression", script);
        Assert.Contains("RUN_POSTGRES_INTEGRATION_TESTS", script);
        Assert.Contains("Due_batch_automation", script);
        Assert.Contains("tests/Platform.IntegrationTests/Platform.IntegrationTests.csproj", script);
    }

    [Fact]
    public void Staging_release_check_runner_refreshes_local_staging_before_live_smokes()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("SkipLocalStagingRefresh", script);
        Assert.Contains("Refresh local staging services", script);
        Assert.Contains("Authentication__Dev__Enabled", script);
        Assert.Contains("PUBLIC_DEV_AUTH_ENABLED", script);
        Assert.Contains("Authentication__Oidc__InteractiveEnabled", script);
        Assert.Contains("docker compose --env-file deploy/staging/.env -f deploy/staging/docker-compose.yml up -d --build api worker web", script);
        Assert.Contains("Run local staging smoke", script);
    }

    [Fact]
    public void Staging_release_check_runner_builds_web_app()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("apps/web", script);
        Assert.Contains("npm ci", script);
        Assert.Contains("npm run check", script);
        Assert.Contains("npm run build", script);
        Assert.Contains("SkipWebBuild", script);
    }

    [Fact]
    public void Staging_release_check_runner_audits_web_production_dependencies()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("npm audit --omit=dev --audit-level=high", script);
        Assert.Contains("SkipWebAudit", script);
    }

    [Fact]
    public void Staging_release_check_runner_checks_web_bundle_budgets()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("npm run check:bundles", script);
        Assert.Contains("SkipWebBundleCheck", script);
    }

    [Fact]
    public void Graph_directory_import_smoke_uses_safe_inputs_and_does_not_leak_graph_data()
    {
        var script = ReadRepoFile("deploy/staging/smoke-graph-directory-import.ps1");
        var lower = script.ToLowerInvariant();

        Assert.Contains("GRAPH_DIRECTORY_IMPORT_API_ORIGIN", script);
        Assert.Contains("GRAPH_TENANT_ID", script);
        Assert.Contains("GRAPH_DIRECTORY_PRIMARY_DOMAIN", script);
        Assert.Contains("/directory-imports/workspace", script);
        Assert.Contains("/directory-connections", script);
        Assert.Contains("/directory-import-rules", script);
        Assert.Contains("/subjects", script);
        Assert.Contains("/preview", script);
        Assert.Contains("/apply", script);
        Assert.Contains("safePreviewSummary", script);
        Assert.Contains("safeApplySummary", script);
        Assert.Contains("safeBeforeDirectorySummary", script);
        Assert.Contains("safeAfterDirectorySummary", script);
        Assert.Contains("subjectCountIncrease", script);
        Assert.DoesNotContain("client_secret", lower);
        Assert.DoesNotContain("access_token", lower);
        Assert.DoesNotContain("authorization", lower);
        Assert.DoesNotContain("bearer", lower);
        Assert.DoesNotContain("Write-Host $SessionCookie", script);
        Assert.DoesNotContain("Write-Host \"$SessionCookie", script);
        Assert.DoesNotContain("ConvertTo-Json $preview", script);
        Assert.DoesNotContain("ConvertTo-Json $apply", script);
        Assert.DoesNotContain("ConvertTo-Json $beforeDirectory", script);
        Assert.DoesNotContain("ConvertTo-Json $afterDirectory", script);
        Assert.DoesNotContain("csvContent", script);
        Assert.DoesNotContain("participantCode", script);
    }

    [Fact]
    public void Compose_and_env_examples_expose_microsoft_graph_admin_consent_config()
    {
        var compose = ReadRepoFile("deploy/staging/docker-compose.yml");

        Assert.Contains("DirectoryImports__MicrosoftGraph__ClientId", compose);
        Assert.Contains("DirectoryImports__MicrosoftGraph__ClientSecret", compose);
        Assert.Contains("DirectoryImports__MicrosoftGraph__AdminConsentRedirectUri", compose);
        Assert.Contains("DirectoryImports__MicrosoftGraph__PostConsentRedirectUrl", compose);
        Assert.Contains("DirectoryImports__MicrosoftGraph__AdminConsentTenant", compose);

        foreach (var path in new[] { "deploy/staging/env.example", "deploy/staging/vps.env.example" })
        {
            var env = ReadRepoFile(path);

            Assert.Contains("DirectoryImports__MicrosoftGraph__ClientId=", env);
            Assert.Contains("DirectoryImports__MicrosoftGraph__ClientSecret=", env);
            Assert.Contains("DirectoryImports__MicrosoftGraph__AdminConsentRedirectUri=", env);
            Assert.Contains("DirectoryImports__MicrosoftGraph__PostConsentRedirectUrl=", env);
            Assert.Contains("DirectoryImports__MicrosoftGraph__AdminConsentTenant=organizations", env);
        }
    }

    [Fact]
    public void Staging_release_check_runner_runs_backup_restore_smoke_with_live_gates()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("backup-restore-smoke.ps1", script);
        Assert.Contains("SkipBackupRestoreSmoke", script);
        Assert.Contains("Run backup/restore smoke", script);
    }

    [Fact]
    public void Staging_release_check_runner_exposes_remote_validation_preflight()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("RemoteValidationTenantSlug", script);
        Assert.Contains("RemoteApiOrigin", script);
        Assert.Contains("RemoteWebOrigin", script);
        Assert.Contains("SkipRemotePreflight", script);
        Assert.Contains("smoke-validation-demo-preflight.ps1", script);
        Assert.Contains("-RemoteOnly", script);
        Assert.Contains("-ApiOrigin", script);
        Assert.Contains("-WebOrigin", script);
    }

    [Fact]
    public void Remote_staging_smoke_checks_public_auth_cors_and_optional_authenticated_session()
    {
        var script = ReadRepoFile("deploy/staging/smoke-remote-staging.ps1");

        Assert.Contains("[string]$ApiOrigin", script);
        Assert.Contains("[string]$WebOrigin", script);
        Assert.Contains("[string]$TenantId", script);
        Assert.Contains("[string]$SessionCookie", script);
        Assert.Contains("/health", script);
        Assert.Contains("/auth/session", script);
        Assert.Contains("Access-Control-Request-Method", script);
        Assert.Contains("Access-Control-Allow-Origin", script);
        Assert.Contains("/auth/login", script);
        Assert.Contains("redirect_uri", script);
        Assert.Contains("/auth/callback", script);
        Assert.Contains("setup.manage", script);
        Assert.Contains("No SessionCookie supplied", script);
        Assert.DoesNotContain("servok01+oh-owner@gmail.com", script);
    }

    [Fact]
    public void Remote_staging_smoke_supports_safe_authenticated_cookie_sources()
    {
        var script = ReadRepoFile("deploy/staging/smoke-remote-staging.ps1");

        Assert.Contains("[string]$SessionCookiePath", script);
        Assert.Contains("[switch]$RequireAuthenticatedSession", script);
        Assert.Contains("$env:STAGING_SESSION_COOKIE", script);
        Assert.Contains("Resolve-SessionCookie", script);
        Assert.Contains("Do not commit cookie files", script);
        Assert.Contains("Authenticated session proof required", script);
        Assert.Contains("SessionCookie and SessionCookiePath cannot both be supplied", script);
        Assert.Contains("Get-Content -Raw -LiteralPath $SessionCookiePath", script);
        Assert.Contains("Authenticated session cookie source resolved.", script);
        Assert.DoesNotContain("Write-Host $SessionCookie", script);
        Assert.DoesNotContain("Write-Host \"$SessionCookie", script);
    }

    [Fact]
    public void Staging_release_check_runner_writes_release_evidence_artifact()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("[string]$EvidencePath", script);
        Assert.Contains("Write-ReleaseEvidence", script);
        Assert.Contains("releaseChecksEvidence", script);
        Assert.Contains("ConvertTo-Json -Depth", script);
        Assert.Contains("schemaVersion", script);
        Assert.Contains("passedGates", script);
        Assert.Contains("skippedGates", script);
        Assert.Contains("remoteApiOriginConfigured", script);
        Assert.Contains("remoteWebOriginConfigured", script);
        Assert.Contains("Q-053", script);
        Assert.Contains("Q-054", script);
        Assert.DoesNotContain("remoteApiOrigin = $RemoteApiOrigin", script);
        Assert.DoesNotContain("remoteWebOrigin = $RemoteWebOrigin", script);
    }

    [Fact]
    public void Staging_release_check_runner_writes_proof_scope_claim_boundary()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("proofScope", script);
        Assert.Contains("localDefaultStagingProven", script);
        Assert.Contains("productSpineProven", script);
        Assert.Contains("backupRestoreProven", script);
        Assert.Contains("campaignFailedDeliveryRecoveryRegressionProven", script);
        Assert.Contains("retentionDueBatchAutomationRegressionProven", script);
        Assert.Contains("remotePreflightProven", script);
        Assert.Contains("remoteVpsDeploymentProven", script);
        Assert.Contains("realPersonLegalUseApproved", script);
        Assert.Contains("outboundOperationalNotificationEmailProven", script);
        Assert.Contains("claimBoundary", script);
        Assert.Contains("engineeringEvidenceOnly", script);
        Assert.Contains("remoteProofRequiresOwnerOrigins", script);
        Assert.Contains("q053BlocksRealPersonLegalClaims", script);
        Assert.Contains("q054BlocksOperationalNotificationEmailClaims", script);
    }

    [Fact]
    public void Staging_release_check_runner_writes_sidecar_integrity_metadata()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("evidenceArtifacts", script);
        Assert.Contains("Get-EvidenceArtifacts", script);
        Assert.Contains("Get-EvidenceArtifact", script);
        Assert.Contains("Get-SafeFileHash", script);
        Assert.Contains("Get-FileHash", script);
        Assert.Contains("sha256", script);
        Assert.Contains("productSpine", script);
        Assert.Contains("backupRestore", script);
        Assert.Contains("remotePreflight", script);
    }

    [Fact]
    public void Staging_release_check_runner_writes_product_spine_evidence_sidecar()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("Get-ProductSpineEvidencePath", script);
        Assert.Contains("productSpineEvidencePath", script);
        Assert.Contains(".product-spine.json", script);
        Assert.Contains("smoke-product-spine.ps1", script);
        Assert.Contains("'-EvidencePath'", script);
        Assert.Contains("$productSpineEvidencePath", script);
    }

    [Fact]
    public void Staging_release_check_runner_writes_backup_restore_evidence_sidecar()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("Get-BackupRestoreEvidencePath", script);
        Assert.Contains("backupRestoreEvidencePath", script);
        Assert.Contains(".backup-restore.json", script);
        Assert.Contains("backup-restore-smoke.ps1", script);
        Assert.Contains("$backupRestoreEvidencePath", script);
        Assert.Contains("RequireBackupRestoreEvidence", script);
    }

    [Fact]
    public void Staging_release_check_runner_writes_remote_preflight_evidence_sidecar()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("Get-RemotePreflightEvidencePath", script);
        Assert.Contains("remotePreflightEvidencePath", script);
        Assert.Contains(".remote-preflight.json", script);
        Assert.Contains("smoke-validation-demo-preflight.ps1", script);
        Assert.Contains("$remotePreflightEvidencePath", script);
        Assert.Contains("RequireRemotePreflight", script);
    }

    [Fact]
    public void Staging_release_evidence_verifier_validates_release_and_product_spine_sidecar()
    {
        var script = ReadRepoFile("deploy/staging/verify-release-evidence.ps1");

        Assert.Contains("[string]$EvidencePath", script);
        Assert.Contains("RequireProductSpineEvidence", script);
        Assert.Contains("RequireRemotePreflight", script);
        Assert.Contains("schemaVersion", script);
        Assert.Contains("passedGates", script);
        Assert.Contains("skippedGates", script);
        Assert.Contains("Q-053", script);
        Assert.Contains("Q-054", script);
        Assert.Contains("productSpineEvidencePath", script);
        Assert.Contains("ownerInspectionRoutes", script);
        Assert.Contains("artifactProofs", script);
        Assert.Contains("withdrawalProof", script);
        Assert.Contains("ForbiddenMarkers", script);
        Assert.Contains("rawToken", script);
        Assert.Contains("participantCode", script);
        Assert.Contains("storageKey", script);
        Assert.Contains("ConnectionStrings", script);
        Assert.Contains("Password", script);
        Assert.Contains("Secret", script);
    }

    [Fact]
    public void Staging_release_evidence_verifier_validates_backup_restore_evidence_sidecar()
    {
        var script = ReadRepoFile("deploy/staging/verify-release-evidence.ps1");

        Assert.Contains("RequireBackupRestoreEvidence", script);
        Assert.Contains("backupRestoreEvidencePath", script);
        Assert.Contains("Assert-BackupRestoreEvidence", script);
        Assert.Contains("backupBytes", script);
        Assert.Contains("restorePublicTableCount", script);
        Assert.Contains("restorePublicRelationCount", script);
        Assert.Contains("requiredPlatformTablesPresent", script);
        Assert.Contains("Q-053", script);
    }

    [Fact]
    public void Backup_restore_evidence_includes_backup_sha256()
    {
        var script = ReadRepoFile("deploy/staging/backup-restore-smoke.ps1");

        Assert.Contains("backupSha256", script);
        Assert.Contains("Get-SafeFileHash", script);
        Assert.Contains("Get-FileHash", script);
        Assert.Contains("SHA256", script);
    }

    [Fact]
    public void Staging_release_evidence_verifier_validates_backup_sha256()
    {
        var script = ReadRepoFile("deploy/staging/verify-release-evidence.ps1");

        Assert.Contains("backupSha256", script);
        Assert.Contains("^[a-f0-9]{64}$", script);
        Assert.Contains("Backup/restore evidence must include backupSha256.", script);
    }

    [Fact]
    public void Staging_release_evidence_verifier_validates_remote_preflight_evidence_sidecar()
    {
        var script = ReadRepoFile("deploy/staging/verify-release-evidence.ps1");

        Assert.Contains("RequireRemotePreflight", script);
        Assert.Contains("remotePreflightEvidencePath", script);
        Assert.Contains("Assert-RemotePreflightEvidence", script);
        Assert.Contains("remotePreflightChecks", script);
        Assert.Contains("apiHealth", script);
        Assert.Contains("webApp", script);
        Assert.Contains("authSessionCors", script);
        Assert.Contains("loginRedirect", script);
        Assert.Contains("Q-053", script);
        Assert.Contains("Q-054", script);
    }

    [Fact]
    public void Staging_release_evidence_verifier_validates_operational_notification_admin_evidence()
    {
        var script = ReadRepoFile("deploy/staging/verify-release-evidence.ps1");

        Assert.Contains("operationalNotificationProof", script);
        Assert.Contains("terminalNotificationProven", script);
        Assert.Contains("summaryProven", script);
        Assert.Contains("markReadProven", script);
        Assert.Contains("markAllReadProven", script);
        Assert.Contains("inAppOnly", script);
        Assert.Contains("emailRoutingProven", script);
        Assert.Contains("unreadAfterMarkAllRead", script);
        Assert.Contains("Q-054", script);
    }

    [Fact]
    public void Staging_release_evidence_verifier_validates_report_export_evidence_detail()
    {
        var script = ReadRepoFile("deploy/staging/verify-release-evidence.ps1");

        Assert.Contains("reportExportProof", script);
        Assert.Contains("scoreMetadataProven", script);
        Assert.Contains("reportExportCodebookMetadataProven", script);
        Assert.Contains("responseExportScoreMetadataProven", script);
        Assert.Contains("waveComparisonScoreMetadataProven", script);
        Assert.Contains("reportPdfDeliveryChecked", script);
        Assert.Contains("signedDownloadUrlChecked", script);
        Assert.Contains("postWithdrawalExportRegenerationProven", script);
        Assert.Contains("postWithdrawalReportPdfRegenerationProven", script);
        Assert.Contains("artifactLeakChecksProven", script);
    }

    [Fact]
    public void Staging_release_evidence_verifier_validates_campaign_email_delivery_evidence()
    {
        var script = ReadRepoFile("deploy/staging/verify-release-evidence.ps1");

        Assert.Contains("campaignEmailDeliveryProof", script);
        Assert.Contains("invitationBatchProven", script);
        Assert.Contains("deliveryProcessingProven", script);
        Assert.Contains("localDevProviderProven", script);
        Assert.Contains("createdInvitationCount", script);
        Assert.Contains("processedCount", script);
        Assert.Contains("sentCount", script);
        Assert.Contains("failedCount", script);
        Assert.Contains("smtpDeliveryProven", script);
        Assert.Contains("failedRequeueRecoveryProven", script);
    }

    [Fact]
    public void Staging_release_evidence_verifier_validates_campaign_email_requeue_noop()
    {
        var script = ReadRepoFile("deploy/staging/verify-release-evidence.ps1");

        Assert.Contains("failedRequeueNoopProven", script);
        Assert.Contains("failedRequeueNoopRequeuedCount", script);
        Assert.Contains("Product-spine evidence must prove failed campaign email requeue no-op.", script);
    }

    [Fact]
    public void Staging_release_evidence_verifier_validates_proof_scope_claim_boundary()
    {
        var script = ReadRepoFile("deploy/staging/verify-release-evidence.ps1");

        Assert.Contains("proofScope", script);
        Assert.Contains("claimBoundary", script);
        Assert.Contains("localDefaultStagingProven", script);
        Assert.Contains("productSpineProven", script);
        Assert.Contains("backupRestoreProven", script);
        Assert.Contains("campaignFailedDeliveryRecoveryRegressionProven", script);
        Assert.Contains("Release evidence proofScope.campaignFailedDeliveryRecoveryRegressionProven must match the campaign email failed-delivery recovery regression gate.", script);
        Assert.Contains("retentionDueBatchAutomationRegressionProven", script);
        Assert.Contains("Release evidence proofScope.retentionDueBatchAutomationRegressionProven must match the retention due-batch automation regression gate.", script);
        Assert.Contains("remotePreflightProven", script);
        Assert.Contains("remoteVpsDeploymentProven", script);
        Assert.Contains("realPersonLegalUseApproved", script);
        Assert.Contains("outboundOperationalNotificationEmailProven", script);
        Assert.Contains("engineeringEvidenceOnly", script);
        Assert.Contains("remoteProofRequiresOwnerOrigins", script);
        Assert.Contains("q053BlocksRealPersonLegalClaims", script);
        Assert.Contains("q054BlocksOperationalNotificationEmailClaims", script);
    }

    [Fact]
    public void Staging_release_evidence_verifier_rejects_forged_campaign_recovery_proof_without_gate()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var evidencePath = WriteMinimalReleaseEvidenceFixture(
                tempDirectory,
                "forged-campaign-recovery-proof.json",
                campaignRecoveryProven: true,
                retentionAutomationProven: false);

            var result = RunReleaseEvidenceVerifier(evidencePath);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("proofScope.campaignFailedDeliveryRecoveryRegressionProven", result.Output);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Staging_release_evidence_verifier_rejects_forged_retention_automation_proof_without_gate()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var evidencePath = WriteMinimalReleaseEvidenceFixture(
                tempDirectory,
                "forged-retention-automation-proof.json",
                campaignRecoveryProven: false,
                retentionAutomationProven: true);

            var result = RunReleaseEvidenceVerifier(evidencePath);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("proofScope.retentionDueBatchAutomationRegressionProven", result.Output);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Staging_release_evidence_verifier_validates_sidecar_integrity_metadata()
    {
        var script = ReadRepoFile("deploy/staging/verify-release-evidence.ps1");

        Assert.Contains("evidenceArtifacts", script);
        Assert.Contains("Assert-EvidenceArtifactHash", script);
        Assert.Contains("Get-SafeFileHash", script);
        Assert.Contains("Get-FileHash", script);
        Assert.Contains("sha256", script);
        Assert.Contains("productSpine", script);
        Assert.Contains("backupRestore", script);
        Assert.Contains("remotePreflight", script);
        Assert.Contains("hash mismatch", script);
    }

    [Fact]
    public void Staging_release_evidence_verifier_reports_hash_validation_summary()
    {
        var script = ReadRepoFile("deploy/staging/verify-release-evidence.ps1");

        Assert.Contains("productSpineHashValidated", script);
        Assert.Contains("backupRestoreHashValidated", script);
        Assert.Contains("remotePreflightHashValidated", script);
        Assert.Contains("Product-spine hash validated", script);
        Assert.Contains("Backup/restore hash validated", script);
        Assert.Contains("Remote preflight hash validated", script);
    }

    [Fact]
    public void Staging_release_check_runner_evidence_self_verification_invokes_verifier()
    {
        var script = ReadRepoFile("deploy/staging/run-release-checks.ps1");

        Assert.Contains("Invoke-ReleaseEvidenceVerifier", script);
        Assert.Contains("verify-release-evidence.ps1", script);
        Assert.Contains("RequireProductSpineEvidence", script);
        Assert.Contains("RequireRemotePreflight", script);
        Assert.Contains("liveProductSpineEvidenceExpected", script);
        Assert.Contains("remotePreflightExpected", script);
    }

    [Fact]
    public void Vps_staging_runbook_makes_release_runner_the_default_gate()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("deploy/staging/run-release-checks.ps1", runbook);
        Assert.Contains("staging release checks", runbook.ToLowerInvariant());
        Assert.Contains("-SkipLiveSmoke", runbook);
        Assert.Contains("-SkipDockerConfig", runbook);
        Assert.Contains("-SkipWebBuild", runbook);
    }

    [Fact]
    public void Vps_staging_runbook_documents_backup_restore_release_gate()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("backup/restore smoke", runbook.ToLowerInvariant());
        Assert.Contains("backup-restore-smoke.ps1", runbook);
        Assert.Contains("-SkipBackupRestoreSmoke", runbook);
    }

    [Fact]
    public void Vps_staging_runbook_documents_remote_validation_preflight_release_gate()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("remote validation preflight", runbook.ToLowerInvariant());
        Assert.Contains("-RemoteApiOrigin", runbook);
        Assert.Contains("-RemoteWebOrigin", runbook);
        Assert.Contains("-SkipRemotePreflight", runbook);
        Assert.Contains("smoke-validation-demo-preflight.ps1", runbook);
    }

    [Fact]
    public void Vps_staging_runbook_documents_release_evidence_artifact()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("-EvidencePath", runbook);
        Assert.Contains("release evidence JSON", runbook);
        Assert.Contains("skipped gates", runbook);
        Assert.Contains("raw remote origins", runbook);
        Assert.Contains("Q-053", runbook);
        Assert.Contains("Q-054", runbook);
    }

    [Fact]
    public void Vps_staging_runbook_documents_release_evidence_proof_scope_claim_boundary()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("proofScope", runbook);
        Assert.Contains("claimBoundary", runbook);
        Assert.Contains("local/default staging proof", runbook);
        Assert.Contains("remote VPS deployment proof", runbook);
        Assert.Contains("real-person legal/GDPR approval", runbook);
        Assert.Contains("outbound operational-notification email proof", runbook);
    }

    [Fact]
    public void Vps_staging_runbook_documents_release_evidence_sidecar_integrity()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("evidenceArtifacts", runbook);
        Assert.Contains("SHA-256", runbook);
        Assert.Contains("sidecar hashes", runbook);
        Assert.Contains("recomputes hashes", runbook);
        Assert.Contains("product-spine, backup/restore, and remote-preflight sidecars", runbook);
    }

    [Fact]
    public void Vps_staging_runbook_documents_release_evidence_hash_validation_summary()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("hash-validation summary", runbook);
        Assert.Contains("Product-spine hash validated", runbook);
        Assert.Contains("Backup/restore hash validated", runbook);
        Assert.Contains("Remote preflight hash validated", runbook);
    }

    [Fact]
    public void Vps_staging_runbook_documents_release_evidence_verifier()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("verify-release-evidence.ps1", runbook);
        Assert.Contains("-RequireProductSpineEvidence", runbook);
        Assert.Contains("-RequireRemotePreflight", runbook);
        Assert.Contains("offline verifier", runbook);
        Assert.Contains("forbidden markers", runbook);
    }

    [Fact]
    public void Vps_staging_runbook_documents_release_evidence_self_verification()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("automatically verifies", runbook);
        Assert.Contains("evidence bundle", runbook);
        Assert.Contains("offline verifier", runbook);
    }

    [Fact]
    public void Vps_staging_runbook_documents_backup_restore_evidence_sidecar()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("backup/restore evidence", runbook.ToLowerInvariant());
        Assert.Contains(".backup-restore.json", runbook);
        Assert.Contains("RequireBackupRestoreEvidence", runbook);
    }

    [Fact]
    public void Vps_staging_runbook_documents_backup_sha256()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("backupSha256", runbook);
        Assert.Contains("SHA-256 backup dump digest", runbook);
        Assert.Contains("omits backup paths", runbook);
    }

    [Fact]
    public void Vps_staging_runbook_documents_remote_preflight_evidence_sidecar()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("remote preflight evidence", runbook.ToLowerInvariant());
        Assert.Contains(".remote-preflight.json", runbook);
        Assert.Contains("RequireRemotePreflight", runbook);
    }

    [Fact]
    public void Backup_restore_smoke_passes_container_commands_as_single_shell_arguments()
    {
        var script = ReadRepoFile("deploy/staging/backup-restore-smoke.ps1");

        Assert.Contains("$nativeErrorActionPreference = $ErrorActionPreference", script);
        Assert.Contains("$ErrorActionPreference = 'Continue'", script);
        Assert.Contains("$nativeExitCode = $LASTEXITCODE", script);
        Assert.Contains("$postgresPassword = Get-RequiredEnvValue", script);
        Assert.Contains("PGPASSWORD=$postgresPassword", script);
        Assert.Contains("$pgDumpCommand =", script);
        Assert.Contains("$pgRestoreCommand =", script);
        Assert.Contains("$postgresWorkerUser = 'platform_worker'", script);
        Assert.Contains("$envValues.ContainsKey('POSTGRES_WORKER_USER')", script);
        Assert.Contains("platform_worker", script);
        Assert.Contains("restore worker role bootstrap", script);
        Assert.Contains("createuser", script);
        Assert.Contains("'sh', '-c',", script);
        Assert.Contains("$pgDumpCommand", script);
        Assert.Contains("$pgRestoreCommand", script);
        Assert.DoesNotContain("' + $backupFileName + '", script);
    }

    [Fact]
    public void Backup_restore_smoke_writes_safe_structured_backup_restore_evidence()
    {
        var script = ReadRepoFile("deploy/staging/backup-restore-smoke.ps1");

        Assert.Contains("[string]$EvidencePath", script);
        Assert.Contains("Write-BackupRestoreEvidence", script);
        Assert.Contains("backupRestoreEvidence", script);
        Assert.Contains("backupBytes", script);
        Assert.Contains("restorePublicTableCount", script);
        Assert.Contains("restorePublicRelationCount", script);
        Assert.Contains("requiredPlatformTablesPresent", script);
        Assert.Contains("restoreVolumeKept", script);
        Assert.Contains("ConvertTo-Json -Depth", script);
        Assert.Contains("Q-053", script);
        Assert.Contains("credential values", script);
    }

    [Fact]
    public void Vps_backup_restore_smoke_runs_safe_linux_target_rehearsal()
    {
        var script = ReadRepoFile("deploy/staging/backup-restore-vps-smoke.sh");

        Assert.Contains("set -euo pipefail", script);
        Assert.Contains("deploy/staging/.env", script);
        Assert.Contains("deploy/staging/docker-compose.yml", script);
        Assert.Contains("deploy/staging/docker-compose.vps.yml", script);
        Assert.Contains("mktemp -d", script);
        Assert.Contains("trap cleanup EXIT", script);
        Assert.Contains("pg_dump", script);
        Assert.Contains("createdb", script);
        Assert.Contains("pg_restore", script);
        Assert.Contains("dropdb", script);
        Assert.Contains("restore_db=", script);
        Assert.Contains("public.tenant", script);
        Assert.Contains("public.audit_event", script);
        Assert.Contains("Q-053", script);
        Assert.DoesNotContain("cat deploy/staging/.env", script);
        Assert.DoesNotContain("echo $POSTGRES_PASSWORD", script);
        Assert.DoesNotContain("echo \"$POSTGRES_PASSWORD", script);
    }

    [Fact]
    public void Vps_release_check_runner_collects_safe_target_evidence()
    {
        var script = ReadRepoFile("deploy/staging/run-vps-release-checks.sh");

        Assert.Contains("set -euo pipefail", script);
        Assert.Contains("api-staging.validatedscale.com", script);
        Assert.Contains("staging.validatedscale.com", script);
        Assert.Contains("curl", script);
        Assert.Contains("/health", script);
        Assert.Contains("/auth/session", script);
        Assert.Contains("Access-Control-Request-Method", script);
        Assert.Contains("/auth/login", script);
        Assert.Contains("tenant_id=", script);
        Assert.Contains("returnUrl=%2Fapp", script);
        Assert.Contains("tenantId=$tenant_id", script);
        Assert.Contains("X-Tenant-Id: $tenant_id", script);
        Assert.Contains("backup-restore-vps-smoke.sh", script);
        Assert.Contains("backup-restore.json", script);
        Assert.Contains("release-evidence.json", script);
        Assert.Contains("STAGING_SESSION_COOKIE", script);
        Assert.Contains("--session-cookie-file", script);
        Assert.Contains("--require-authenticated-session", script);
        Assert.Contains("Authenticated session proof required", script);
        Assert.Contains("remotePublicSmokeProven", script);
        Assert.Contains("vpsBackupRestoreProven", script);
        Assert.Contains("authenticatedRemoteSmokeProven", script);
        Assert.Contains("legalGdprReady", script);
        Assert.Contains("operationalNotificationEmailReady", script);
        Assert.Contains("Q-053", script);
        Assert.Contains("Q-054", script);
        Assert.DoesNotContain("cat deploy/staging/.env", script);
        Assert.DoesNotContain("echo $STAGING_SESSION_COOKIE", script);
        Assert.DoesNotContain("echo \"$STAGING_SESSION_COOKIE", script);
        Assert.DoesNotContain("echo $session_cookie", script);
        Assert.DoesNotContain("echo \"$session_cookie", script);
    }

    [Fact]
    public void Vps_release_check_runner_can_verify_legacy_web_redirects()
    {
        var script = ReadRepoFile("deploy/staging/run-vps-release-checks.sh");

        Assert.Contains("--legacy-web-origin", script);
        Assert.Contains("legacy_web_origin", script);
        Assert.Contains("legacy_web_redirect_status", script);
        Assert.Contains("legacy_web_redirect_location", script);
        Assert.Contains("legacyWebRedirectStatus", script);
        Assert.Contains("legacyWebRedirectLocation", script);
        Assert.Contains("curl -sS -D \"$legacy_web_headers\"", script);
        Assert.Contains("expected $web_origin", script);
    }

    [Fact]
    public void Vps_redeploy_smoke_records_revision_and_runs_release_checks()
    {
        var script = ReadRepoFile("deploy/staging/redeploy-vps-stack.sh");

        Assert.Contains("set -euo pipefail", script);
        Assert.Contains("deploy/staging/.env", script);
        Assert.Contains("deploy/staging/docker-compose.yml", script);
        Assert.Contains("deploy/staging/docker-compose.vps.yml", script);
        Assert.Contains("git rev-parse HEAD", script);
        Assert.Contains("git rev-parse --abbrev-ref HEAD", script);
        Assert.Contains("docker compose", script);
        Assert.Contains("up -d --build", script);
        Assert.Contains("run-vps-release-checks.sh", script);
        Assert.Contains("api_origin=\"$(read_env_value STAGING_API_ORIGIN || read_env_value PUBLIC_API_BASE_URL || true)\"", script);
        Assert.Contains("web_origin=\"$(read_env_value STAGING_WEB_ORIGIN || read_env_value Cors__AllowedOrigins__0 || true)\"", script);
        Assert.Contains("legacy_web_origin=\"$(read_env_value STAGING_LEGACY_WEB_ORIGIN || true)\"", script);
        Assert.Contains("--api-origin \"$api_origin\"", script);
        Assert.Contains("--web-origin \"$web_origin\"", script);
        Assert.Contains("--legacy-web-origin \"$legacy_web_origin\"", script);
        Assert.Contains("redeploy-evidence.json", script);
        Assert.Contains("release-evidence", script);
        Assert.Contains("redeployProven", script);
        Assert.Contains("releaseChecksProven", script);
        Assert.Contains("rollbackProven", script);
        Assert.Contains("legalGdprReady", script);
        Assert.Contains("operationalNotificationEmailReady", script);
        Assert.Contains("Q-053", script);
        Assert.Contains("Q-054", script);
        Assert.DoesNotContain("cat deploy/staging/.env", script);
        Assert.DoesNotContain("POSTGRES_PASSWORD", script);
        Assert.DoesNotContain("Authentication__Oidc__ClientSecret", script);
    }

    [Fact]
    public void Vps_rollback_smoke_round_trips_revision_and_runs_release_checks()
    {
        var script = ReadRepoFile("deploy/staging/rollback-vps-stack.sh");

        Assert.Contains("set -euo pipefail", script);
        Assert.Contains("deploy/staging/.env", script);
        Assert.Contains("deploy/staging/docker-compose.yml", script);
        Assert.Contains("deploy/staging/docker-compose.vps.yml", script);
        Assert.Contains("git diff --quiet", script);
        Assert.Contains("git rev-parse HEAD~1", script);
        Assert.Contains("git checkout --detach \"$rollback_revision\"", script);
        Assert.Contains("git checkout \"$restore_checkout_ref\"", script);
        Assert.Contains("docker compose", script);
        Assert.Contains("up -d --build", script);
        Assert.Contains("run-vps-release-checks.sh", script);
        Assert.Contains("api_origin=\"$(read_env_value STAGING_API_ORIGIN || read_env_value PUBLIC_API_BASE_URL || true)\"", script);
        Assert.Contains("web_origin=\"$(read_env_value STAGING_WEB_ORIGIN || read_env_value Cors__AllowedOrigins__0 || true)\"", script);
        Assert.Contains("legacy_web_origin=\"$(read_env_value STAGING_LEGACY_WEB_ORIGIN || true)\"", script);
        Assert.Contains("--api-origin \"$api_origin\"", script);
        Assert.Contains("--web-origin \"$web_origin\"", script);
        Assert.Contains("--legacy-web-origin \"$legacy_web_origin\"", script);
        Assert.Contains("rollback-evidence.json", script);
        Assert.Contains("rollback-release-evidence", script);
        Assert.Contains("restore-release-evidence", script);
        Assert.Contains("rollbackProven", script);
        Assert.Contains("restoreProven", script);
        Assert.Contains("releaseChecksAfterRollbackProven", script);
        Assert.Contains("releaseChecksAfterRestoreProven", script);
        Assert.Contains("legalGdprReady", script);
        Assert.Contains("operationalNotificationEmailReady", script);
        Assert.Contains("Q-053", script);
        Assert.Contains("Q-054", script);
        Assert.DoesNotContain("git reset --hard", script);
        Assert.DoesNotContain("cat deploy/staging/.env", script);
        Assert.DoesNotContain("POSTGRES_PASSWORD", script);
        Assert.DoesNotContain("Authentication__Oidc__ClientSecret", script);
    }

    [Fact]
    public void Validation_demo_preflight_writes_safe_structured_remote_preflight_evidence()
    {
        var script = ReadRepoFile("deploy/staging/smoke-validation-demo-preflight.ps1");

        Assert.Contains("[string]$EvidencePath", script);
        Assert.Contains("Write-RemotePreflightEvidence", script);
        Assert.Contains("remotePreflightEvidence", script);
        Assert.Contains("remotePreflightChecks", script);
        Assert.Contains("apiHealth", script);
        Assert.Contains("webApp", script);
        Assert.Contains("authSessionCors", script);
        Assert.Contains("loginRedirect", script);
        Assert.Contains("ConvertTo-Json -Depth", script);
        Assert.Contains("Q-053", script);
        Assert.Contains("Q-054", script);
        Assert.Contains("raw origins", script);
        Assert.Contains("provider redirect URLs", script);
    }

    [Fact]
    public void Local_staging_smoke_checks_current_frontend_shell_marker()
    {
        var script = ReadRepoFile("deploy/staging/smoke-local-staging.ps1");

        Assert.Contains("Instruments Platform", script);
        Assert.DoesNotContain("Tenant setup workspace", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_report_pdf_artifact_route()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("/campaign-series/$($series.id)/report-pdf-artifacts", script);
        Assert.Contains("campaign_series_report_pdf", script);
        Assert.Contains("Report PDF artifact fetch returned the wrong artifact.", script);
    }

    [Fact]
    public void Product_spine_smoke_writes_safe_structured_product_spine_evidence()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("[string]$EvidencePath", script);
        Assert.Contains("Write-ProductSpineEvidence", script);
        Assert.Contains("productSpineEvidence", script);
        Assert.Contains("ownerInspectionRoutes", script);
        Assert.Contains("artifactProofs", script);
        Assert.Contains("withdrawalProof", script);
        Assert.Contains("postWithdrawalReportPdfArtifact", script);
        Assert.Contains("campaignInvitationDeliveryProven", script);
        Assert.Contains("ConvertTo-Json -Depth", script);
        Assert.Contains("Q-053", script);
        Assert.Contains("Q-054", script);
        Assert.DoesNotContain("rawToken =", script);
        Assert.DoesNotContain("rawParticipantCodes", script);
        Assert.DoesNotContain("rawAnswers", script);
    }

    [Fact]
    public void Product_spine_smoke_supports_authenticated_remote_cookie_sources()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("[string]$SessionCookiePath", script);
        Assert.Contains("[switch]$RequireAuthenticatedSession", script);
        Assert.Contains("$env:STAGING_SESSION_COOKIE", script);
        Assert.Contains("Resolve-SessionCookie", script);
        Assert.Contains("SessionCookie and SessionCookiePath cannot both be supplied", script);
        Assert.Contains("Get-Content -Raw -LiteralPath $SessionCookiePath", script);
        Assert.Contains("Authenticated product-spine session proof required", script);
        Assert.Contains("Authenticated product-spine session cookie source resolved.", script);
        Assert.Contains("/auth/csrf", script);
        Assert.Contains("X-CSRF-TOKEN", script);
        Assert.Contains("remoteCookieAuthenticated", script);
        Assert.Contains("Cookie = $resolvedAuth.CookieHeader", script);
        Assert.DoesNotContain("Write-Host $SessionCookie", script);
        Assert.DoesNotContain("Write-Host \"$SessionCookie", script);
    }

    [Fact]
    public void Product_spine_smoke_writes_operational_notification_admin_evidence()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("$notificationSummaryAndMarkReadProof = Assert-OperationalNotificationSummaryAndMarkRead", script);
        Assert.Contains("$notificationMarkAllReadProof = Assert-OperationalNotificationMarkAllRead", script);
        Assert.Contains("operationalNotificationProof", script);
        Assert.Contains("terminalNotificationProven", script);
        Assert.Contains("summaryProven", script);
        Assert.Contains("markReadProven", script);
        Assert.Contains("markAllReadProven", script);
        Assert.Contains("inAppOnly", script);
        Assert.Contains("emailRoutingProven", script);
        Assert.Contains("unreadBeforeMarkRead", script);
        Assert.Contains("unreadAfterMarkRead", script);
        Assert.Contains("markAllReadMarkedReadCount", script);
        Assert.Contains("unreadAfterMarkAllRead", script);
    }

    [Fact]
    public void Product_spine_smoke_writes_report_export_evidence_detail()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("reportExportProof", script);
        Assert.Contains("scoreMetadataProven", script);
        Assert.Contains("reportExportCodebookMetadataProven", script);
        Assert.Contains("responseExportScoreMetadataProven", script);
        Assert.Contains("waveComparisonScoreMetadataProven", script);
        Assert.Contains("reportPdfDeliveryChecked", script);
        Assert.Contains("signedDownloadUrlChecked", script);
        Assert.Contains("postCloseReportPdfProven", script);
        Assert.Contains("postWithdrawalExportRegenerationProven", script);
        Assert.Contains("postWithdrawalReportPdfRegenerationProven", script);
        Assert.Contains("artifactLeakChecksProven", script);
    }

    [Fact]
    public void Product_spine_smoke_writes_campaign_email_delivery_evidence()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("$campaignEmailDeliveryProof = Assert-CampaignEmailInvitationDelivery", script);
        Assert.Contains("campaignEmailDeliveryProof", script);
        Assert.Contains("invitationBatchProven", script);
        Assert.Contains("deliveryProcessingProven", script);
        Assert.Contains("localDevProviderProven", script);
        Assert.Contains("createdInvitationCount", script);
        Assert.Contains("processedCount", script);
        Assert.Contains("sentCount", script);
        Assert.Contains("failedCount", script);
        Assert.Contains("smtpDeliveryProven", script);
        Assert.Contains("failedRequeueRecoveryProven", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_campaign_email_requeue_noop()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("/campaigns/$($Campaign.id)/notification-deliveries/requeue-failed", script);
        Assert.Contains("Campaign email failed-delivery requeue no-op did not return zero.", script);
        Assert.Contains("failedRequeueNoopProven", script);
        Assert.Contains("failedRequeueNoopRequeuedCount", script);
    }

    [Fact]
    public void Product_spine_smoke_waits_for_report_pdf_operational_notification()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Wait-OperationalNotificationForArtifact -ArtifactId $reportPdfArtifact.id", script);
        Assert.Contains("/operational-notifications?limit=50", script);
        Assert.Contains("sourceAggregateId", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_report_pdf_delivery_or_safe_failure()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Assert-ReportPdfArtifactDeliveryState -Artifact $reportPdfArtifactFetched", script);
        Assert.Contains("/export-artifacts/$($Artifact.id)/download", script);
        Assert.Contains("application/pdf", script);
        Assert.Contains("failureReasonCode", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_report_pdf_signed_download_url_or_safe_unsupported()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Assert-ReportPdfArtifactSignedDownloadUrlState -Artifact $reportPdfArtifactFetched", script);
        Assert.Contains("Add-Type -AssemblyName System.Net.Http", script);
        Assert.Contains("/export-artifacts/$($Artifact.id)/signed-download-url", script);
        Assert.Contains("export_artifact_object.signed_urls_not_supported", script);
        Assert.Contains("storageKey", script);
        Assert.Contains("secret", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_post_close_report_pdf_artifact()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("$closedReportPdfArtifact = Invoke-Json POST \"/campaign-series/$($series.id)/report-pdf-artifacts\"", script);
        Assert.Contains("$closedReportPdfArtifactFetched = Invoke-Json GET \"/export-artifacts/$($closedReportPdfArtifact.id)\"", script);
        Assert.Contains("Assert-ReportPdfArtifactDeliveryState -Artifact $closedReportPdfArtifactFetched", script);
        Assert.Contains("Assert-ReportPdfArtifactSignedDownloadUrlState -Artifact $closedReportPdfArtifactFetched", script);
        Assert.Contains("Wait-OperationalNotificationForArtifact -ArtifactId $closedReportPdfArtifact.id", script);
        Assert.Contains("Post-close report PDF artifact", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_withdrawal_token_issue_and_consume()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Assert-WithdrawalTokenIssueAndConsume", script);
        Assert.Contains("/withdrawal-requests/tokens", script);
        Assert.Contains("/withdrawal-requests/anonymous", script);
        Assert.Contains("responseSessionId = $SubmittedResponse.session.id", script);
        Assert.Contains("Withdrawal token consume response echoed the raw token.", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_withdrawal_approve_and_execute()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Assert-WithdrawalRequestApproveAndExecute", script);
        Assert.Contains("/withdrawal-requests/$($Withdrawal.request.requestId)/approve", script);
        Assert.Contains("/withdrawal-requests/$($Withdrawal.request.requestId)/execute", script);
        Assert.Contains("Withdrawal execution did not complete.", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_withdrawal_request_review_visibility()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Assert-WithdrawalRequestReviewVisibility", script);
        Assert.Contains("/withdrawal-requests", script);
        Assert.Contains("/withdrawal-requests/$($Withdrawal.request.requestId)", script);
        Assert.Contains("Assert-WithdrawalRequestReviewVisibility -Withdrawal $withdrawalSmoke -ExpectedStatus 'requested'", script);
        Assert.Contains("Assert-WithdrawalRequestReviewVisibility -Withdrawal $withdrawalSmoke -ExpectedStatus 'completed'", script);
        Assert.Contains("Withdrawal review list response leaked sensitive data.", script);
        Assert.Contains("Withdrawal review detail response leaked sensitive data.", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_withdrawal_terminal_operational_notification()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Assert-WithdrawalTerminalNotification", script);
        Assert.Contains("withdrawal_request_terminal", script);
        Assert.Contains("WithdrawalRequestTerminal", script);
        Assert.Contains("sourceStatus", script);
        Assert.Contains("Withdrawal terminal notification leaked sensitive data.", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_operational_notification_summary_and_mark_read()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Assert-OperationalNotificationSummaryAndMarkRead", script);
        Assert.Contains("/operational-notifications/summary", script);
        Assert.Contains("/operational-notifications/$($Notification.id)/mark-read", script);
        Assert.Contains("Operational notification summary unread count did not decrement after mark-read.", script);
        Assert.Contains("Operational notification mark-read response leaked sensitive data.", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_operational_notification_mark_all_read()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Assert-OperationalNotificationMarkAllRead", script);
        Assert.Contains("/operational-notifications/mark-all-read", script);
        Assert.Contains("Operational notification mark-all-read did not mark the expected unread notifications.", script);
        Assert.Contains("Operational notification mark-all-read response leaked sensitive data.", script);
        Assert.Contains("Operational notification summary unread count did not reach zero after mark-all-read.", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_withdrawal_derived_artifact_invalidation()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Assert-WithdrawalInvalidatedDerivedArtifacts", script);
        Assert.Contains("deletedAt", script);
        Assert.Contains("canDownload", script);
        Assert.Contains("checksumSha256", script);
        Assert.Contains("csvContent", script);
        Assert.Contains("Withdrawal invalidated artifact still exposed old CSV content.", script);
        Assert.Contains("Wave 1 report export", script);
        Assert.Contains("Pre-withdrawal response export", script);
        Assert.Contains("Post-close response export", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_withdrawal_report_pdf_artifact_invalidation()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("$reportPdfArtifactFetched.status -eq 'succeeded'", script);
        Assert.Contains("Report PDF artifact", script);
        Assert.Contains("$invalidationArtifacts += [pscustomobject]@", script);
        Assert.Contains("Assert-WithdrawalInvalidatedDerivedArtifacts -Artifacts $invalidationArtifacts", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_post_withdrawal_fresh_export_regeneration()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Assert-PostWithdrawalFreshExportArtifactSafety", script);
        Assert.Contains("$postWithdrawalReportExport = Invoke-Json POST \"/campaigns/$($wave1.campaign.id)/report-proof/exports\"", script);
        Assert.Contains("$postWithdrawalResponseExport = Invoke-Json POST \"/campaign-series/$($series.id)/response-exports\"", script);
        Assert.Contains("Assert-ReportExportScoreMetadata $postWithdrawalReportArtifact 'Post-withdrawal report export'", script);
        Assert.Contains("Assert-ResponseExportScoreMetadata $postWithdrawalResponseArtifact 'Post-withdrawal response export'", script);
        Assert.Contains("Post-withdrawal response export artifact", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_post_withdrawal_report_pdf_artifact_regeneration()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Assert-PostWithdrawalReportPdfArtifactSafety", script);
        Assert.Contains("$postWithdrawalReportPdfArtifact = Invoke-Json POST \"/campaign-series/$($series.id)/report-pdf-artifacts\"", script);
        Assert.Contains("$postWithdrawalReportPdfArtifactFetched = Invoke-Json GET \"/export-artifacts/$($postWithdrawalReportPdfArtifact.id)\"", script);
        Assert.Contains("Assert-ReportPdfArtifactDeliveryState -Artifact $postWithdrawalReportPdfArtifactFetched", script);
        Assert.Contains("Assert-ReportPdfArtifactSignedDownloadUrlState -Artifact $postWithdrawalReportPdfArtifactFetched", script);
        Assert.Contains("Wait-OperationalNotificationForArtifact -ArtifactId $postWithdrawalReportPdfArtifact.id", script);
        Assert.Contains("Post-withdrawal report PDF artifact", script);
    }

    [Fact]
    public void Product_spine_smoke_checks_campaign_email_invitation_delivery()
    {
        var script = ReadRepoFile("deploy/staging/smoke-product-spine.ps1");

        Assert.Contains("Assert-CampaignEmailInvitationDelivery", script);
        Assert.Contains("/campaigns/$($Campaign.id)/invitation-batches", script);
        Assert.Contains("/campaigns/$($Campaign.id)/notification-deliveries/process", script);
        Assert.Contains("local-dev", script);
        Assert.Contains("Campaign email delivery response leaked sensitive data.", script);
    }

    [Fact]
    public void Vps_staging_runbook_documents_campaign_email_recovery_regression_gate()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("campaign email failed-delivery recovery regression", runbook);
        Assert.Contains("backend synthetic regression proof", runbook);
        Assert.Contains("not SMTP live delivery proof", runbook);
        Assert.Contains("-SkipEmailRecoveryRegression", runbook);
    }

    [Fact]
    public void Vps_staging_runbook_documents_retention_automation_regression_gate()
    {
        var runbook = ReadRepoFile("docs/v2/40-ops/vps-staging-runbook.md");

        Assert.Contains("retention due-batch automation regression", runbook);
        Assert.Contains("backend synthetic retention enforcement proof", runbook);
        Assert.Contains("does not enable retention automation", runbook);
        Assert.Contains("-SkipRetentionAutomationRegression", runbook);
    }

    [Fact]
    public void Compose_shares_local_export_artifact_store_between_api_and_worker()
    {
        var compose = ReadRepoFile("deploy/staging/docker-compose.yml");
        var api = ExtractServiceSection(compose, "api");
        var worker = ExtractServiceSection(compose, "worker");

        foreach (var service in new[] { api, worker })
        {
            Assert.Contains("ExportArtifacts__ObjectStore__Provider: ${ExportArtifacts__ObjectStore__Provider:-local}", service);
            Assert.Contains("ExportArtifacts__ObjectStore__RootPath: ${ExportArtifacts__ObjectStore__RootPath:-/var/lib/instruments-platform/export-artifacts}", service);
            Assert.Contains("platform_staging_export_artifacts:/var/lib/instruments-platform/export-artifacts", service);
        }

        Assert.Contains("platform_staging_export_artifacts:", compose);
    }

    [Fact]
    public void Compose_persists_api_data_protection_keys()
    {
        var compose = ReadRepoFile("deploy/staging/docker-compose.yml");
        var api = ExtractServiceSection(compose, "api");

        Assert.Contains("platform_staging_data_protection_keys:/root/.aspnet/DataProtection-Keys", api);
        Assert.Contains("platform_staging_data_protection_keys:", compose);
    }

    [Fact]
    public void Staging_admin_seed_script_is_parameterized_and_idempotent()
    {
        var script = ReadRepoFile("deploy/staging/seed-staging-admin.ps1");

        Assert.Contains("[Parameter(Mandatory = $true)]", script);
        Assert.Contains("[string]$Email", script);
        Assert.Contains("staging-admin", script);
        Assert.Contains("setup.manage", script);
        Assert.Contains("team.manage", script);
        Assert.Contains("ON CONFLICT (code) DO NOTHING", script);
        Assert.Contains("ON CONFLICT (tenant_id, email) DO UPDATE", script);
        Assert.Contains("WHERE NOT EXISTS", script);
        Assert.Contains("docker compose --env-file deploy/staging/.env", script);
        Assert.DoesNotContain("servok01+oh-owner@gmail.com", script);
    }

    [Fact]
    public void Env_examples_expose_export_artifact_object_store_defaults()
    {
        foreach (var path in new[] { "deploy/staging/env.example", "deploy/staging/vps.env.example" })
        {
            var env = ReadRepoFile(path);

            Assert.Contains("ExportArtifacts__ObjectStore__Provider=local", env);
            Assert.Contains("ExportArtifacts__ObjectStore__RootPath=/var/lib/instruments-platform/export-artifacts", env);
            Assert.Contains("ExportArtifacts__ObjectStore__S3__Endpoint=", env);
            Assert.Contains("ExportArtifacts__ObjectStore__S3__BucketName=", env);
            Assert.Contains("ExportArtifacts__ObjectStore__S3__Region=us-east-1", env);
            Assert.Contains("ExportArtifacts__ObjectStore__S3__AccessKeyId=", env);
            Assert.Contains("ExportArtifacts__ObjectStore__S3__SecretAccessKey=", env);
        }
    }

    [Fact]
    public void Compose_exposes_report_pdf_renderer_config_to_api_and_worker()
    {
        var compose = ReadRepoFile("deploy/staging/docker-compose.yml");
        var api = ExtractServiceSection(compose, "api");
        var worker = ExtractServiceSection(compose, "worker");

        foreach (var service in new[] { api, worker })
        {
            Assert.Contains("Reports__PdfRenderer__BrowserExecutablePath: ${Reports__PdfRenderer__BrowserExecutablePath:-/usr/bin/chromium}", service);
            Assert.Contains("Reports__PdfRenderer__TimeoutMilliseconds: ${Reports__PdfRenderer__TimeoutMilliseconds:-30000}", service);
            Assert.Contains("Reports__PdfRenderer__DisableSandbox: ${Reports__PdfRenderer__DisableSandbox:-true}", service);
        }
    }

    [Fact]
    public void Env_examples_expose_report_pdf_renderer_defaults()
    {
        foreach (var path in new[] { "deploy/staging/env.example", "deploy/staging/vps.env.example" })
        {
            var env = ReadRepoFile(path);

            Assert.Contains("Reports__PdfRenderer__BrowserExecutablePath=", env);
            Assert.Contains("Reports__PdfRenderer__TimeoutMilliseconds=30000", env);
            Assert.Contains("Reports__PdfRenderer__DisableSandbox=true", env);
        }
    }

    [Fact]
    public void Compose_and_env_examples_expose_email_delivery_config_to_api()
    {
        var compose = ReadRepoFile("deploy/staging/docker-compose.yml");
        var api = ExtractServiceSection(compose, "api");

        Assert.Contains("EmailDelivery__Provider: ${EmailDelivery__Provider:-local-dev}", api);
        Assert.Contains("EmailDelivery__SenderDomainVerified: ${EmailDelivery__SenderDomainVerified:-false}", api);
        Assert.Contains("EmailDelivery__VerifiedSenderDomain: ${EmailDelivery__VerifiedSenderDomain:-}", api);
        Assert.Contains("EmailDelivery__FromAddress: ${EmailDelivery__FromAddress:-}", api);
        Assert.Contains("EmailDelivery__PublicAppBaseUrl: ${EmailDelivery__PublicAppBaseUrl:-https://staging.validatedscale.com}", api);
        Assert.Contains("EmailDelivery__InvitationFooterText: ${EmailDelivery__InvitationFooterText:-}", api);
        Assert.Contains("EmailDelivery__AzureCommunicationServices__ConnectionString: ${EmailDelivery__AzureCommunicationServices__ConnectionString:-}", api);
        Assert.Contains("EmailDelivery__AzureCommunicationServices__Endpoint: ${EmailDelivery__AzureCommunicationServices__Endpoint:-}", api);
        Assert.Contains("EmailDelivery__AzureCommunicationServices__AccessKey: ${EmailDelivery__AzureCommunicationServices__AccessKey:-}", api);
        Assert.Contains("EmailDelivery__AzureCommunicationServices__EventGridWebhookSecret: ${EmailDelivery__AzureCommunicationServices__EventGridWebhookSecret:-}", api);
        Assert.Contains("EmailDelivery__AzureCommunicationServices__DisableUserEngagementTracking: ${EmailDelivery__AzureCommunicationServices__DisableUserEngagementTracking:-true}", api);
        Assert.DoesNotContain("EmailDelivery__ManagedProviderName", api);
        Assert.DoesNotContain("EmailDelivery__AwsSes", api);
        Assert.DoesNotContain("EmailDelivery__Smtp", api);
        Assert.DoesNotContain("EmailDelivery__ProviderWebhookSecret", api);

        foreach (var path in new[] { "deploy/staging/env.example", "deploy/staging/vps.env.example" })
        {
            var env = ReadRepoFile(path);

            Assert.Contains("EmailDelivery__Provider=local-dev", env);
            Assert.Contains("EmailDelivery__SenderDomainVerified=false", env);
            Assert.Contains("EmailDelivery__VerifiedSenderDomain=", env);
            Assert.Contains("EmailDelivery__FromAddress=", env);
            Assert.Contains("EmailDelivery__PublicAppBaseUrl=", env);
            Assert.Contains("EmailDelivery__InvitationFooterText=", env);
            Assert.Contains("EmailDelivery__AzureCommunicationServices__ConnectionString=", env);
            Assert.Contains("EmailDelivery__AzureCommunicationServices__Endpoint=", env);
            Assert.Contains("EmailDelivery__AzureCommunicationServices__AccessKey=", env);
            Assert.Contains("EmailDelivery__AzureCommunicationServices__EventGridWebhookSecret=", env);
            Assert.Contains("EmailDelivery__AzureCommunicationServices__DisableUserEngagementTracking=true", env);
            Assert.DoesNotContain("EmailDelivery__ManagedProviderName=", env);
            Assert.DoesNotContain("EmailDelivery__AwsSes", env);
            Assert.DoesNotContain("EmailDelivery__Smtp", env);
            Assert.DoesNotContain("EmailDelivery__ProviderWebhookSecret", env);
        }
    }

    [Fact]
    public void Api_and_worker_images_package_chromium_for_report_pdf_rendering()
    {
        foreach (var path in new[] { "deploy/staging/api.Dockerfile", "deploy/staging/worker.Dockerfile" })
        {
            var dockerfile = ReadRepoFile(path);

            Assert.Contains("apt-get update", dockerfile);
            Assert.Contains("chromium", dockerfile);
            Assert.Contains("fonts-dejavu-core", dockerfile);
            Assert.Contains("rm -rf /var/lib/apt/lists/*", dockerfile);
        }
    }

    [Fact]
    public void Compose_and_env_examples_default_pdf_renderer_to_packaged_chromium()
    {
        var compose = ReadRepoFile("deploy/staging/docker-compose.yml");
        var api = ExtractServiceSection(compose, "api");
        var worker = ExtractServiceSection(compose, "worker");

        foreach (var service in new[] { api, worker })
        {
            Assert.Contains("Reports__PdfRenderer__BrowserExecutablePath: ${Reports__PdfRenderer__BrowserExecutablePath:-/usr/bin/chromium}", service);
        }

        foreach (var path in new[] { "deploy/staging/env.example", "deploy/staging/vps.env.example" })
        {
            var env = ReadRepoFile(path);

            Assert.Contains("Reports__PdfRenderer__BrowserExecutablePath=/usr/bin/chromium", env);
        }
    }

    private static string CreateTempDirectory()
    {
        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "staging-release-evidence-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }

    private static string WriteMinimalReleaseEvidenceFixture(
        string tempDirectory,
        string fileName,
        bool campaignRecoveryProven,
        bool retentionAutomationProven)
    {
        var evidencePath = Path.Combine(tempDirectory, fileName);
        var evidence = new
        {
            schemaVersion = 1,
            generatedAt = DateTimeOffset.UtcNow.ToString("O"),
            runner = "deploy/staging/run-release-checks.ps1",
            status = "passed",
            proofScope = new
            {
                localDefaultStagingProven = false,
                productSpineProven = false,
                backupRestoreProven = false,
                campaignFailedDeliveryRecoveryRegressionProven = campaignRecoveryProven,
                retentionDueBatchAutomationRegressionProven = retentionAutomationProven,
                remotePreflightProven = false,
                remoteVpsDeploymentProven = false,
                realPersonLegalUseApproved = false,
                outboundOperationalNotificationEmailProven = false
            },
            claimBoundary = new
            {
                engineeringEvidenceOnly = true,
                remoteProofRequiresOwnerOrigins = true,
                q053BlocksRealPersonLegalClaims = true,
                q054BlocksOperationalNotificationEmailClaims = true
            },
            evidenceArtifacts = new
            {
                productSpine = CreateAbsentEvidenceArtifact("productSpine"),
                backupRestore = CreateAbsentEvidenceArtifact("backupRestore"),
                remotePreflight = CreateAbsentEvidenceArtifact("remotePreflight")
            },
            productSpineEvidencePath = "",
            backupRestoreEvidencePath = "",
            remotePreflightEvidencePath = "",
            passedGates = new[]
            {
                new
                {
                    number = 1,
                    name = "Build solution"
                }
            },
            skippedGates = Array.Empty<object>(),
            inputs = new
            {
                remoteApiOriginConfigured = false,
                remoteWebOriginConfigured = false
            },
            limitations = new[]
            {
                "Q-053 blocks real-person production legal/GDPR/DPA claims; this evidence is engineering proof only.",
                "Q-054 blocks outbound operational-notification email routing and claims that operational events are emailed."
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(
            evidence,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(evidencePath, json);
        return evidencePath;
    }

    private static object CreateAbsentEvidenceArtifact(string name)
    {
        return new
        {
            name,
            path = "",
            exists = false,
            sha256 = ""
        };
    }

    private static (int ExitCode, string Output) RunReleaseEvidenceVerifier(string evidencePath)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = GetRepoRoot(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add("deploy/staging/verify-release-evidence.ps1");
        startInfo.ArgumentList.Add("-EvidencePath");
        startInfo.ArgumentList.Add(evidencePath);

        using var process = System.Diagnostics.Process.Start(startInfo);
        Assert.NotNull(process);

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(30000), "Release evidence verifier did not exit within 30 seconds.");

        return (process.ExitCode, standardOutput + standardError);
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "deploy", "staging")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }

    private static string ReadRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "deploy", "staging")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }

    private static string ExtractServiceSection(string compose, string serviceName)
    {
        var normalized = compose.Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = normalized.Split('\n');
        var section = new List<string>();
        var inSection = false;

        foreach (var line in lines)
        {
            if (line == $"  {serviceName}:")
            {
                inSection = true;
            }
            else if (inSection &&
                     line.StartsWith("  ", StringComparison.Ordinal) &&
                     !line.StartsWith("    ", StringComparison.Ordinal))
            {
                break;
            }

            if (inSection)
            {
                section.Add(line);
            }
        }

        Assert.NotEmpty(section);
        return string.Join('\n', section);
    }

    private static string ExtractReadWriteGrantBlock(string script, string grantee)
    {
        var normalized = script.Replace("\r\n", "\n", StringComparison.Ordinal);
        const string startMarker = "GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE";
        var endMarker = $"TO :\"{grantee}\";";

        var searchIndex = 0;
        while (searchIndex < normalized.Length)
        {
            var startIndex = normalized.IndexOf(startMarker, searchIndex, StringComparison.Ordinal);
            if (startIndex < 0)
            {
                break;
            }

            var endIndex = normalized.IndexOf(endMarker, startIndex, StringComparison.Ordinal);
            if (endIndex >= 0)
            {
                return normalized[startIndex..(endIndex + endMarker.Length)];
            }

            searchIndex = startIndex + startMarker.Length;
        }

        Assert.Fail($"Expected read/write grant block for {grantee}.");
        return string.Empty;
    }
}
