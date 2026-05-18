import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { describe, it } from 'node:test';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const read = (relativePath) => readFileSync(path.join(repoRoot, relativePath), 'utf8');
const exists = (relativePath) => existsSync(path.join(repoRoot, relativePath));

describe('local staging deployment package', () => {
  it('contains the required deployment files', () => {
    for (const file of [
      'deploy/staging/docker-compose.yml',
      'deploy/staging/api.Dockerfile',
      'deploy/staging/web.Dockerfile',
      'deploy/staging/migrator.Dockerfile',
      'deploy/staging/runtime-role.sql',
      'deploy/staging/env.example',
      'deploy/staging/start-local-staging.ps1',
      'deploy/staging/smoke-local-staging.ps1',
      'deploy/staging/smoke-product-spine.ps1',
      'deploy/staging/smoke-validation-demo-preflight.ps1',
      'deploy/staging/select-validation-demo-tenant.ps1',
      'deploy/staging/seed-validation-demo.ps1',
      'deploy/staging/bootstrap-validation-demo-auth.ps1',
      'deploy/staging/backup-restore-smoke.ps1',
      'deploy/staging/validation-demo-fixtures/README.md',
      'deploy/staging/validation-demo-fixtures/tenant-bootstrap.sql',
      'deploy/staging/validation-demo-fixtures/validation-demo-catalog.json',
      'deploy/staging/validation-demo-fixtures/validation-demo-auth-users.example.json',
      'deploy/staging/stop-local-staging.ps1',
      '.dockerignore'
    ]) {
      assert.equal(exists(file), true, `${file} should exist`);
    }
  });

  it('keeps the compose stack isolated and ordered', () => {
    const compose = read('deploy/staging/docker-compose.yml');

    for (const service of ['postgres:', 'migrator:', 'runtime-role:', 'seed:', 'api:', 'web:']) {
      assert.match(compose, new RegExp(`\\n  ${service}`));
    }

    assert.match(compose, /condition: service_healthy/);
    assert.match(compose, /condition: service_completed_successfully/);
    assert.match(compose, /platform_staging_postgres_data/);
  });

  it('runs the API through a least-privilege runtime database role', () => {
    const compose = read('deploy/staging/docker-compose.yml');
    const env = read('deploy/staging/env.example');
    const runtimeRole = read('deploy/staging/runtime-role.sql');

    assert.match(env, /POSTGRES_RUNTIME_USER=platform_app_runtime/);
    assert.match(env, /POSTGRES_RUNTIME_PASSWORD=platform_app_runtime/);
    assert.match(compose, /runtime-role:/);
    assert.match(compose, /runtime-role\.sql/);
    assert.match(compose, /Username=\$\{POSTGRES_RUNTIME_USER:-platform_app_runtime\}/);
    assert.match(compose, /Password=\$\{POSTGRES_RUNTIME_PASSWORD:-platform_app_runtime\}/);
    assert.match(compose, /seed:[\s\S]*condition: service_completed_successfully/);
    assert.match(runtimeRole, /CREATE ROLE/);
    assert.match(runtimeRole, /GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE/);
    assert.match(runtimeRole, /respondent_rule/);
  });

  it('documents safe env defaults without real production secrets', () => {
    const env = read('deploy/staging/env.example');

    assert.match(env, /COMPOSE_PROJECT_NAME=instruments-platform-staging/);
    assert.match(env, /PUBLIC_DEV_AUTH_ENABLED=true/);
    assert.doesNotMatch(env, /BEGIN (RSA|OPENSSH) PRIVATE KEY/);
    assert.doesNotMatch(env, /ghp_[A-Za-z0-9_]+/);
  });

  it('documents Auth0-oriented VPS staging settings without development auth', () => {
    const env = read('deploy/staging/vps.env.example');
    const compose = read('deploy/staging/docker-compose.vps.yml');

    assert.match(env, /PUBLIC_DEV_AUTH_ENABLED=false/);
    assert.match(env, /PUBLIC_TENANT_ID=11111111-1111-4111-8111-111111111111/);
    assert.match(env, /Authentication__Oidc__InteractiveEnabled=true/);
    assert.match(env, /Authentication__Oidc__Authority=https:\/\/replace-with-auth0-domain\//);
    assert.match(env, /Authentication__Oidc__ClientId=replace-with-auth0-client-id/);
    assert.match(env, /Authentication__Oidc__ClientSecret=replace-with-auth0-client-secret/);
    assert.match(env, /Cors__AllowedOrigins__0=https:\/\/staging\.example\.com/);
    assert.match(env, /PUBLIC_AUTH_LOGIN_URL=https:\/\/staging-api\.example\.com\/auth\/login\?returnUrl=https%3A%2F%2Fstaging\.example\.com%2Fapp/);

    assert.match(compose, /ASPNETCORE_ENVIRONMENT: Production/);
    assert.match(compose, /Authentication__Dev__Enabled: "false"/);
    assert.match(compose, /Authentication__Oidc__InteractiveEnabled: \$\{Authentication__Oidc__InteractiveEnabled:-true\}/);
    assert.match(compose, /Authentication__Oidc__ClientSecret: \$\{Authentication__Oidc__ClientSecret:\?set Auth0 client secret\}/);
    assert.match(compose, /Cors__AllowedOrigins__0: \$\{Cors__AllowedOrigins__0:\?set staging web CORS origin\}/);
    assert.match(compose, /PUBLIC_AUTH_LOGIN_URL: \$\{PUBLIC_AUTH_LOGIN_URL:\?set API auth login URL with allowed web returnUrl\}/);
  });

  it('keeps the web runtime image install free of dev prepare scripts', () => {
    const dockerfile = read('deploy/staging/web.Dockerfile');

    assert.match(dockerfile, /RUN npm ci --omit=dev --ignore-scripts/);
    assert.doesNotMatch(dockerfile, /RUN npm ci --omit=dev\s*$/m);
  });

  it('does not remove volumes unless explicitly requested', () => {
    const stopScript = read('deploy/staging/stop-local-staging.ps1');

    assert.match(stopScript, /\[switch\]\$RemoveVolumes/);
    assert.match(stopScript, /if \(\$RemoveVolumes\)/);
    assert.match(stopScript, /docker compose/);
  });

  it('includes an owner-runnable live product spine smoke', () => {
    const smoke = read('deploy/staging/smoke-product-spine.ps1');

    for (const requiredPath of [
      '/workspace-overview',
      '/campaign-series',
      '/setup-workspace',
      '/operations-workspace',
      '/reports-workspace',
      '/waves-workspace',
      '/respondent/open-links/',
      '/respondent/sessions/',
      '/report-proof',
      '/report-proof/exports',
      '/response-exports',
      '/two-wave-proof',
      '/wave-comparison-proof'
    ]) {
      assert.match(smoke, new RegExp(requiredPath.replaceAll('/', '\\/')));
    }

    assert.match(smoke, /Owner inspection routes/);
    assert.match(smoke, /\/app\/campaign-series\/\$\(\$series\.id\)/);
    assert.match(smoke, /anonymous_longitudinal/);
    assert.match(smoke, /acceptedGrants/);
    assert.doesNotMatch(smoke, /OLBI/i);
    assert.doesNotMatch(smoke, /git(hub)? actions/i);
  });

  it('asserts live score-output metadata through product-spine smoke artifacts', () => {
    const smoke = read('deploy/staging/smoke-product-spine.ps1');
    const submitStart = smoke.indexOf('function Submit-LinkedResponse');
    const mainStart = smoke.indexOf('$envValues = Read-EnvFile', submitStart);
    const submitLinkedResponse = smoke.slice(submitStart, mainStart);

    for (const required of [
      'Assert-ScoreResponseMetadata',
      'Assert-ReportScoreMetadata',
      'Assert-WaveComparisonScoreMetadata',
      'Assert-ReportExportScoreMetadata',
      'Assert-ResponseExportScoreMetadata',
      'nValid',
      'nExpected',
      'missingPolicyStatus',
      'nValidTotal',
      'nExpectedTotal',
      'missingPolicyStatusSummary',
      'baselineNValidTotal',
      'baselineNExpectedTotal',
      'baselineMissingPolicyStatusSummary',
      'comparisonNValidTotal',
      'comparisonNExpectedTotal',
      'comparisonMissingPolicyStatusSummary',
      'n_valid_total',
      'n_expected_total',
      'missing_policy_status_summary',
      'score_total_n_valid',
      'score_total_n_expected',
      'score_total_missing_policy_status',
      'score_output_metadata',
      'suppressed_when_report_proof_suppressed',
      'per_submitted_response_score_metadata',
      'scoreMetadataDimensionCount'
    ]) {
      assert.match(smoke, new RegExp(required));
    }

    assert.doesNotMatch(
      submitLinkedResponse,
      /\/respondent\/sessions\/\$\(\$session\.id\)\/scores/,
      'Submit-LinkedResponse should rely on submit-time materialization, not per-session score calls.'
    );
    assert.match(smoke, /Manual score endpoint compatibility/);
  });

  it('defines source-safe validation demo tenant fixtures', () => {
    const catalogText = read('deploy/staging/validation-demo-fixtures/validation-demo-catalog.json');
    const catalog = JSON.parse(catalogText);

    assert.equal(catalog.environmentLabel, 'proof_demo');
    assert.equal(catalog.productionData, false);
    assert.equal(catalog.platformCanonicalInstruments, false);

    const slugs = catalog.tenants.map((tenant) => tenant.slug).sort();
    assert.deepEqual(slugs, [
      'validation-oh-research',
      'validation-osh-consulting',
      'validation-se-education'
    ]);

    for (const tenant of catalog.tenants) {
      assert.match(tenant.legalLabel, /Proof-of-concept demo/);
      assert.equal(tenant.productionData, false);
      assert.equal(tenant.platformCanonical, false);
      assert.ok(tenant.instruments.length >= 3, `${tenant.slug} should include at least three proof instruments`);
      assert.ok(tenant.story, `${tenant.slug} should define demo story metadata`);
      assert.match(tenant.story.mainSeriesName, /\S/, `${tenant.slug} should define a main series story name`);
      assert.match(tenant.story.linkedWaveSeriesName, /\S/, `${tenant.slug} should define a linked-wave story name`);
      assert.doesNotMatch(tenant.story.mainSeriesName, /^VAL0[0-9]\b/i);
      assert.doesNotMatch(tenant.story.linkedWaveSeriesName, /^VAL0[0-9]\b/i);

      for (const campaignName of ['draft', 'liveNoResponses', 'partial', 'completed', 'wave1', 'wave2']) {
        assert.match(
          tenant.story.campaignNames?.[campaignName] ?? '',
          /\S/,
          `${tenant.slug} should define ${campaignName} campaign story name`
        );
        assert.doesNotMatch(tenant.story.campaignNames[campaignName], /^VAL0[0-9]\b/i);
      }

      for (const profileName of ['partial', 'completed', 'waveBaseline', 'waveComparison']) {
        const profile = tenant.story.responseProfiles?.[profileName];
        assert.equal(Array.isArray(profile), true, `${tenant.slug} should define ${profileName} response profile`);
        assert.ok(profile.length >= 1, `${tenant.slug} ${profileName} response profile should not be empty`);
        assert.equal(
          profile.every((value) => Number.isInteger(value) && value >= 1 && value <= 5),
          true,
          `${tenant.slug} ${profileName} response profile should contain 1-5 integer values`
        );
      }

      assert.ok(
        tenant.story.responseProfiles.completed.length >= 5,
        `${tenant.slug} should keep at least five completed response profiles`
      );
      assert.ok(
        tenant.story.responseProfiles.waveBaseline.length >= 5 && tenant.story.responseProfiles.waveComparison.length >= 5,
        `${tenant.slug} should keep at least five linked-wave response profiles`
      );

      for (const instrument of tenant.instruments) {
        assert.equal(instrument.platformCanonical, false);
        assert.equal(instrument.availableToOtherTenants, false);
        assert.match(instrument.rightsBasis, /synthetic_demo|source_checked_private_demo|tenant_attested/);
        assert.match(instrument.reportLabel, /not platform-canonical|not official|not legal compliance advice|no real student data/i);
        assert.ok(
          instrument.questions.length >= 8,
          `${tenant.slug}/${instrument.code} should include at least eight questions for a credible demo instrument`
        );
      }

      const stateKinds = new Set(tenant.proofStates.map((state) => state.kind));
      for (const requiredKind of ['draft', 'live_no_responses', 'partial_response', 'completed_scored', 'closed_wave', 'export']) {
        assert.equal(stateKinds.has(requiredKind), true, `${tenant.slug} should include ${requiredKind}`);
      }
    }

    for (const forbidden of [
      /\bOLBI\b/i,
      /\bMBI\b/i,
      /Maslach/i,
      /Nordic Musculoskeletal Questionnaire/i,
      /How often do you feel/i,
      /Is your work emotionally exhausting/i
    ]) {
      assert.doesNotMatch(catalogText, forbidden);
    }
  });

  it('includes a validation demo seed script with guarded output and proof paths', () => {
    const script = read('deploy/staging/seed-validation-demo.ps1');

    for (const required of [
      'validation-demo-catalog.json',
      'tenant-bootstrap.sql',
      'ValidateOnly',
      'AllowDuplicateSeed',
      'X-Dev-Tenant-Memberships',
      '/campaign-series',
      '/open-link',
      '/response-exports',
      '/wave-comparison-proof',
      'Owner inspection routes',
      'Assert-ValidationTenantsEmpty',
      'Validation tenants already contain validation demo data',
      '-AllowDuplicateSeed',
      'campaign_series',
      'export_artifact'
    ]) {
      assert.match(script, new RegExp(required.replaceAll('/', '\\/')));
    }

    assert.match(script, /\$MinimumInstrumentQuestionCount\s*=\s*8/);
    assert.match(script, /story\.mainSeriesName/);
    assert.match(script, /story\.responseProfiles/);
    assert.doesNotMatch(script, /name = "VAL03 \$\(\$Tenant\.slug\)/);

    assert.doesNotMatch(script, /Write-Host[^\n]*(token|participant code|ParticipantCode)/i);
    assert.doesNotMatch(script, /OLBI|Maslach|Nordic Musculoskeletal Questionnaire/i);
    assert.doesNotMatch(script, /@gmail\.com|@algebra\.hr|@demo\.test/i);
  });

  it('defines source-safe validation demo auth user slots', () => {
    const usersText = read('deploy/staging/validation-demo-fixtures/validation-demo-auth-users.example.json');
    const users = JSON.parse(usersText);
    const gitignore = read('.gitignore');

    assert.equal(users.environmentLabel, 'proof_demo');
    assert.equal(users.productionData, false);

    const expectedSlugs = [
      'validation-oh-research',
      'validation-osh-consulting',
      'validation-se-education'
    ];
    const expectedRoles = ['analyst', 'researcher', 'tenant_owner', 'viewer'];
    const slugs = users.tenants.map((tenant) => tenant.slug).sort();
    assert.deepEqual(slugs, expectedSlugs);

    for (const tenant of users.tenants) {
      const roles = tenant.users.map((user) => user.role).sort();
      assert.deepEqual(roles, expectedRoles, `${tenant.slug} should define all demo auth roles`);

      const emails = tenant.users.map((user) => user.email);
      assert.equal(new Set(emails).size, emails.length, `${tenant.slug} should use one placeholder per role`);

      for (const email of emails) {
        assert.match(email, /^[^@\s]+@demo\.test$/);
      }
    }

    assert.doesNotMatch(usersText, /@gmail\.com|@algebra\.hr|@auth0\.com|martin|danijel/i);
    assert.doesNotMatch(usersText, /client_secret|provider_subject|password|connection string|token/i);
    assert.match(gitignore, /validation-demo-auth-users\.local\.json/);
  });

  it('includes a validation demo auth membership bootstrap with safe boundaries', () => {
    const script = read('deploy/staging/bootstrap-validation-demo-auth.ps1');

    for (const required of [
      'validation-demo-auth-users.local.json',
      'validation-demo-auth-users.example.json',
      'tenant-bootstrap.sql',
      'permission',
      'role_permission',
      'user_account',
      'role_assignment',
      'ON CONFLICT',
      'setup.manage',
      'team.manage',
      'tenant_owner',
      'researcher',
      'analyst',
      'viewer',
      'AllowPlaceholderEmails',
      'ValidateOnly'
    ]) {
      assert.equal(script.includes(required), true, `${required} should appear in auth bootstrap script`);
    }

    assert.equal(
      (script.match(/Permissions = @\('setup\.manage', 'team\.manage'\)/g) || []).length,
      3,
      'tenant_owner should receive setup.manage and team.manage in each validation tenant'
    );
    assert.equal(
      (script.match(/Permissions = @\('setup\.manage'\)/g) || []).length,
      3,
      'researcher should remain setup.manage-only in each validation tenant'
    );
    assert.doesNotMatch(script, /INSERT\s+INTO\s+external_auth_identity/i);
    assert.doesNotMatch(script, /INSERT\s+INTO\s+auth_session/i);
    assert.doesNotMatch(script, /Write-Host[^\n]*(email|ClientSecret|provider subject|token|password|connection string)/i);
    assert.doesNotMatch(script, /@gmail\.com|@algebra\.hr|@auth0\.com|martin|danijel/i);
  });

  it('includes a validation demo tenant switch helper with safe env boundaries', () => {
    const script = read('deploy/staging/select-validation-demo-tenant.ps1');

    for (const required of [
      'validation-oh-research',
      'validation-se-education',
      'validation-osh-consulting',
      '33333333-3333-4333-8333-333333333333',
      '44444444-4444-4444-8444-444444444444',
      '55555555-5555-4555-8555-555555555555',
      'PUBLIC_TENANT_ID',
      'PUBLIC_API_BASE_URL',
      'Cors__AllowedOrigins__0',
      'PUBLIC_AUTH_LOGIN_URL',
      'PUBLIC_AUTH_LOGOUT_URL',
      'prompt=login',
      'ValidateOnly',
      'Restart',
      'Update-EnvLines',
      'preserve unrelated'
    ]) {
      assert.equal(script.includes(required), true, `${required} should appear in tenant switch helper`);
    }

    assert.match(script, /docker compose/);
    assert.match(script, /returnUrl/);
    assert.doesNotMatch(script, /Write-Host[^\n]*(ClientSecret|provider subject|token|participant code|answer|password|connection string)/i);
    assert.doesNotMatch(script, /@gmail\.com|@algebra\.hr|@auth0\.com|martin|danijel|servok/i);
    assert.doesNotMatch(script, /INSERT\s+INTO|external_auth_identity|auth_session/i);
  });

  it('documents validation demo auth membership bootstrap run order', () => {
    const fixtureReadme = read('deploy/staging/validation-demo-fixtures/README.md');
    const validationTenants = read('docs/v2/80-agent-handoff/validation-demo-tenants.md');
    const vpsRunbook = read('docs/v2/40-ops/vps-staging-runbook.md');
    const deployment = read('docs/v2/40-ops/deployment.md');

    for (const doc of [fixtureReadme, validationTenants, vpsRunbook]) {
      assert.match(doc, /bootstrap-validation-demo-auth\.ps1/);
      assert.match(doc, /validation-demo-auth-users\.local\.json/);
      assert.match(doc, /seed-validation-demo\.ps1/);
      assert.match(doc, /external_auth_identity/);
      assert.match(doc, /Q-053/);
    }

    assert.match(deployment, /bootstrap-validation-demo-auth\.ps1/);
    assert.match(deployment, /Auth0 login smoke/i);
    assert.doesNotMatch(fixtureReadme + validationTenants + vpsRunbook + deployment, /@gmail\.com|@algebra\.hr|@auth0\.com/i);
  });

  it('documents validation demo tenant switching helper without product-scope claims', () => {
    const fixtureReadme = read('deploy/staging/validation-demo-fixtures/README.md');
    const walkthrough = read('docs/v2/80-agent-handoff/validation-demo-walkthrough-packet.md');

    for (const doc of [fixtureReadme, walkthrough]) {
      assert.match(doc, /select-validation-demo-tenant\.ps1/);
      assert.match(doc, /PUBLIC_TENANT_ID/);
      assert.match(doc, /ignored local.*\.env/i);
      assert.doesNotMatch(doc, /in-app tenant switcher/i);
      assert.doesNotMatch(doc, /@gmail\.com|@algebra\.hr|@auth0\.com/i);
    }
  });

  it('includes a validation demo preflight smoke helper with read-only boundaries', () => {
    const script = read('deploy/staging/smoke-validation-demo-preflight.ps1');

    for (const required of [
      'validation-oh-research',
      'validation-se-education',
      'validation-osh-consulting',
      '33333333-3333-4333-8333-333333333333',
      '44444444-4444-4444-8444-444444444444',
      '55555555-5555-4555-8555-555555555555',
      'SkipLiveChecks',
      'SkipDatabaseChecks',
      'RemoteOnly',
      'EvidencePath',
      'Write-RemotePreflightEvidence',
      'AllowPlaceholderEmails',
      'NoPromptLogin',
      'PUBLIC_TENANT_ID',
      'PUBLIC_API_BASE_URL',
      'Cors__AllowedOrigins__0',
      'PUBLIC_AUTH_LOGIN_URL',
      'PUBLIC_AUTH_LOGOUT_URL',
      'seed-validation-demo.ps1',
      'bootstrap-validation-demo-auth.ps1',
      'select-validation-demo-tenant.ps1',
      '/health/live',
      '/health/ready',
      '/auth/session',
      '/auth/login',
      'Access-Control-Allow-Origin',
      'Build-RemoteAuthUrls',
      'RemoteOnly requires -ApiOrigin',
      'RemoteOnly requires -WebOrigin',
      'Skipping local env role-slot tenant-switch and database checks for RemoteOnly',
      'Invoke-DatabaseCountChecks',
      'role_assignment',
      'campaign_series',
      'export_artifact',
      'Add-Type -AssemblyName System.Net.Http',
      'Last failure:',
      'validation-demo-catalog.json',
      'Assert-SelfServeWalkthroughContract',
      'Self-serve walkthrough route checklist',
      'setupSeriesName',
      'inCollectionSeriesName',
      'linkedWaveSeriesName',
      'Duplicate as study'
    ]) {
      assert.equal(script.includes(required), true, `${required} should appear in validation preflight helper`);
    }

    assert.match(script, /ValidateOnly/);
    assert.match(script, /SELECT/);
    assert.match(script, /\$lastFailure/);
    assert.match(script, /Set-Content -Path \$EvidencePath/);
    const scriptWithoutEvidenceWriter = script.replace(
      /function Write-RemotePreflightEvidence \{[\s\S]*?\n}\n\nfunction Invoke-Http/,
      'function Invoke-Http'
    );
    assert.doesNotMatch(script, /catch\s*\{\s*\}\s*Start-Sleep/s);
    assert.doesNotMatch(scriptWithoutEvidenceWriter, /WriteAllLines|Set-Content|Add-Content|Copy-Item|Remove-Item/i);
    assert.doesNotMatch(script, /docker compose[^\n]*(up|down|restart|rm|pull|push)/i);
    assert.doesNotMatch(script, /INSERT\s+INTO|UPDATE\s+|DELETE\s+FROM|external_auth_identity|auth_session/i);
    assert.doesNotMatch(script, /Write-Host[^\n]*(ClientSecret|provider subject|token|participant code|answer|password|connection string|email)/i);
    assert.doesNotMatch(script, /@gmail\.com|@algebra\.hr|@auth0\.com|martin|danijel|servok/i);
  });

  it('documents validation demo preflight smoke before walkthroughs', () => {
    const fixtureReadme = read('deploy/staging/validation-demo-fixtures/README.md');
    const walkthrough = read('docs/v2/80-agent-handoff/validation-demo-walkthrough-packet.md');

    for (const doc of [fixtureReadme, walkthrough]) {
      assert.match(doc, /smoke-validation-demo-preflight\.ps1/);
      assert.match(doc, /SkipLiveChecks/);
      assert.match(doc, /RemoteOnly/);
      assert.match(doc, /read-only/i);
      assert.doesNotMatch(doc, /@gmail\.com|@algebra\.hr|@auth0\.com/i);
    }
  });

  it('includes a backup restore smoke script with safe defaults', () => {
    const script = read('deploy/staging/backup-restore-smoke.ps1');

    for (const required of [
      'pg_dump',
      'pg_restore',
      'RestoreProjectName',
      'KeepRestoreVolume',
      'artifacts/deployment-dr/backups',
      'docker compose',
      'down --volumes',
      'SET session_replication_role',
      'SELECT COUNT(*) FROM information_schema.tables'
    ]) {
      assert.equal(script.includes(required), true, `${required} should appear in backup restore smoke script`);
    }

    assert.match(script, /COMPOSE_PROJECT_NAME/);
    assert.match(script, /POSTGRES_DB/);
    assert.match(script, /POSTGRES_USER/);
    assert.match(script, /POSTGRES_PASSWORD/);
    assert.doesNotMatch(script, /Write-Host[^\n]*(POSTGRES_PASSWORD|ConnectionStrings|ClientSecret|token|participant|answer)/i);
  });

  it('documents backup restore smoke without production claims', () => {
    const backupDr = read('docs/v2/40-ops/backup-and-dr.md');
    const vpsRunbook = read('docs/v2/40-ops/vps-staging-runbook.md');
    const deployment = read('docs/v2/40-ops/deployment.md');

    for (const doc of [backupDr, vpsRunbook, deployment]) {
      assert.match(doc, /backup-restore-smoke\.ps1/);
      assert.match(doc, /Q-053/);
    }

    assert.match(backupDr, /ADR-0012/);
    assert.match(backupDr, /pg_dump/);
    assert.match(backupDr, /pg_restore/);
    assert.match(backupDr, /real-person data remains blocked/i);
    assert.doesNotMatch(backupDr, /GDPR compliant|SLA ready|DPA ready/i);
  });
});
