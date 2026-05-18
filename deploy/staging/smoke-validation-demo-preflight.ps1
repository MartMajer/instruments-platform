param(
    [Parameter(Mandatory = $true)]
    [string]$TenantSlug,
    [string]$EnvFile,
    [string]$UsersFile,
    [string]$ApiOrigin,
    [string]$WebOrigin,
    [string]$EvidencePath,
    [switch]$RemoteOnly,
    [switch]$SkipLiveChecks,
    [switch]$SkipDatabaseChecks,
    [switch]$AllowPlaceholderEmails,
    [switch]$NoPromptLogin
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Net.Http

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$composeFile = Join-Path $repoRoot 'deploy\staging\docker-compose.yml'
$vpsComposeFile = Join-Path $repoRoot 'deploy\staging\docker-compose.vps.yml'
$fixtureDir = Join-Path $PSScriptRoot 'validation-demo-fixtures'
$catalogFile = Join-Path $fixtureDir 'validation-demo-catalog.json'
$seedScript = Join-Path $PSScriptRoot 'seed-validation-demo.ps1'
$authBootstrapScript = Join-Path $PSScriptRoot 'bootstrap-validation-demo-auth.ps1'
$tenantSwitchScript = Join-Path $PSScriptRoot 'select-validation-demo-tenant.ps1'

if (-not $EnvFile) {
    $EnvFile = Join-Path $repoRoot 'deploy\staging\.env'
}

if (-not $UsersFile) {
    $UsersFile = Join-Path $fixtureDir 'validation-demo-auth-users.local.json'
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

$script:Failures = New-Object System.Collections.Generic.List[string]
$script:CompletedChecks = New-Object System.Collections.Generic.List[string]
$script:LiveCheckEvidence = [ordered]@{
    apiHealth = 'not_run'
    apiReadiness = 'not_run'
    webApp = 'not_run'
    authSessionCors = 'not_run'
    loginRedirect = 'not_run'
    databaseCounts = 'not_run'
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

    Assert-True (Test-Path -LiteralPath $Path) "Missing env file '$Path'. Run select-validation-demo-tenant.ps1 for the intended validation tenant first."

    $values = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#')) {
            continue
        }

        $parts = $trimmed.Split('=', 2)
        if ($parts.Length -eq 2) {
            $values[$parts[0].Trim()] = $parts[1].Trim().Trim('"')
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
    Assert-True ([System.Uri]::TryCreate($normalized, [System.UriKind]::Absolute, [ref]$uri)) "$Name must be an absolute http(s) origin."
    Assert-True ($uri.Scheme -eq 'http' -or $uri.Scheme -eq 'https') "$Name must use http or https."
    Assert-True ([string]::IsNullOrWhiteSpace($uri.AbsolutePath) -or $uri.AbsolutePath -eq '/') "$Name must be an origin only."

    return $normalized
}

function Get-Origin {
    param([System.Uri]$Uri)

    if ($Uri.IsDefaultPort) {
        return "$($Uri.Scheme)://$($Uri.Host)"
    }

    return "$($Uri.Scheme)://$($Uri.Host):$($Uri.Port)"
}

function Join-OriginPath {
    param(
        [string]$Origin,
        [string]$Path
    )

    return $Origin.TrimEnd('/') + '/' + $Path.TrimStart('/')
}

function Get-QueryParameters {
    param([System.Uri]$Uri)

    $values = @{}
    $query = $Uri.Query.TrimStart('?')
    if ([string]::IsNullOrWhiteSpace($query)) {
        return $values
    }

    foreach ($part in $query.Split('&')) {
        if ([string]::IsNullOrWhiteSpace($part)) {
            continue
        }

        $pieces = $part.Split('=', 2)
        $key = [System.Uri]::UnescapeDataString($pieces[0].Replace('+', ' '))
        $value = ''
        if ($pieces.Length -eq 2) {
            $value = [System.Uri]::UnescapeDataString($pieces[1].Replace('+', ' '))
        }

        $values[$key] = $value
    }

    return $values
}

function Add-QueryParameterIfMissing {
    param(
        [string]$Url,
        [string]$Name,
        [string]$Value
    )

    if ($Url -match "(^|[?&])$([regex]::Escape($Name))=") {
        return $Url
    }

    $hashIndex = $Url.IndexOf('#')
    $base = $Url
    $hash = ''
    if ($hashIndex -ge 0) {
        $base = $Url.Substring(0, $hashIndex)
        $hash = $Url.Substring($hashIndex)
    }

    $separator = '?'
    if ($base.Contains('?')) {
        $separator = '&'
    }

    return "$base$separator$Name=$([System.Uri]::EscapeDataString($Value))$hash"
}

function Assert-AbsoluteUrl {
    param(
        [string]$Value,
        [string]$Name
    )

    $uri = $null
    Assert-True ([System.Uri]::TryCreate($Value, [System.UriKind]::Absolute, [ref]$uri)) "$Name must be an absolute URL."
    Assert-True ($uri.Scheme -eq 'http' -or $uri.Scheme -eq 'https') "$Name must use http or https."

    return $uri
}

function Assert-AuthUrlShape {
    param(
        [hashtable]$EnvValues,
        [hashtable]$Definition,
        [string]$ResolvedApiOrigin,
        [string]$ResolvedWebOrigin
    )

    $loginUrl = Get-EnvValue $EnvValues 'PUBLIC_AUTH_LOGIN_URL' ''
    $logoutUrl = Get-EnvValue $EnvValues 'PUBLIC_AUTH_LOGOUT_URL' ''
    Assert-True (-not [string]::IsNullOrWhiteSpace($loginUrl)) 'PUBLIC_AUTH_LOGIN_URL is required.'
    Assert-True (-not [string]::IsNullOrWhiteSpace($logoutUrl)) 'PUBLIC_AUTH_LOGOUT_URL is required.'

    $loginUri = Assert-AbsoluteUrl $loginUrl 'PUBLIC_AUTH_LOGIN_URL'
    Assert-True ((Get-Origin $loginUri) -eq $ResolvedApiOrigin) 'PUBLIC_AUTH_LOGIN_URL must use the configured API origin.'
    Assert-True ($loginUri.AbsolutePath -eq '/auth/login') 'PUBLIC_AUTH_LOGIN_URL must point to /auth/login.'

    $loginParams = Get-QueryParameters $loginUri
    Assert-True ($loginParams.ContainsKey('returnUrl')) 'PUBLIC_AUTH_LOGIN_URL must include returnUrl.'
    Assert-True ($loginParams['returnUrl'] -eq (Join-OriginPath $ResolvedWebOrigin '/app')) 'PUBLIC_AUTH_LOGIN_URL returnUrl must point to the web /app route.'
    if ($loginParams.ContainsKey('tenantId')) {
        Assert-True ($loginParams['tenantId'] -eq $Definition.Id) 'PUBLIC_AUTH_LOGIN_URL tenantId must match the selected validation tenant.'
    }
    if (-not $NoPromptLogin) {
        Assert-True ($loginParams.ContainsKey('prompt') -and $loginParams['prompt'] -eq 'login') 'PUBLIC_AUTH_LOGIN_URL must include prompt=login for local role-slot smoke.'
    }

    $logoutUri = Assert-AbsoluteUrl $logoutUrl 'PUBLIC_AUTH_LOGOUT_URL'
    Assert-True ((Get-Origin $logoutUri) -eq $ResolvedApiOrigin) 'PUBLIC_AUTH_LOGOUT_URL must use the configured API origin.'
    Assert-True ($logoutUri.AbsolutePath -eq '/auth/logout') 'PUBLIC_AUTH_LOGOUT_URL must point to /auth/logout.'

    $logoutParams = Get-QueryParameters $logoutUri
    Assert-True ($logoutParams.ContainsKey('returnUrl')) 'PUBLIC_AUTH_LOGOUT_URL must include returnUrl.'
    Assert-True ($logoutParams['returnUrl'] -eq (Join-OriginPath $ResolvedWebOrigin '/')) 'PUBLIC_AUTH_LOGOUT_URL returnUrl must point to the web root.'
}

function Build-RemoteAuthUrls {
    param(
        [string]$ResolvedApiOrigin,
        [string]$ResolvedWebOrigin,
        [hashtable]$Definition
    )

    $loginUrl = Join-OriginPath $ResolvedApiOrigin '/auth/login'
    $loginUrl = Add-QueryParameterIfMissing -Url $loginUrl -Name 'returnUrl' -Value (Join-OriginPath $ResolvedWebOrigin '/app')
    $loginUrl = Add-QueryParameterIfMissing -Url $loginUrl -Name 'tenantId' -Value $Definition.Id
    if (-not $NoPromptLogin) {
        $loginUrl = Add-QueryParameterIfMissing -Url $loginUrl -Name 'prompt' -Value 'login'
    }

    $logoutUrl = Join-OriginPath $ResolvedApiOrigin '/auth/logout'
    $logoutUrl = Add-QueryParameterIfMissing -Url $logoutUrl -Name 'returnUrl' -Value (Join-OriginPath $ResolvedWebOrigin '/')

    return @{
        PUBLIC_TENANT_ID = $Definition.Id
        PUBLIC_API_BASE_URL = $ResolvedApiOrigin
        'Cors__AllowedOrigins__0' = $ResolvedWebOrigin
        PUBLIC_AUTH_LOGIN_URL = $loginUrl
        PUBLIC_AUTH_LOGOUT_URL = $logoutUrl
    }
}

function Get-SafeMessage {
    param([object]$ErrorRecord)

    $message = [string]$ErrorRecord.Exception.Message
    if ($message.Length -gt 220) {
        $message = $message.Substring(0, 220) + '...'
    }

    return $message
}

function Invoke-Check {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    try {
        & $Action
        $script:CompletedChecks.Add($Name) | Out-Null
        Write-Host "[PASS] $Name"
    } catch {
        $script:Failures.Add($Name)
        Write-Host "[FAIL] $Name - $(Get-SafeMessage $_)"
    }
}

function Set-RemotePreflightCheckStatus {
    param(
        [string]$Name,
        [string]$Status
    )

    $script:LiveCheckEvidence[$Name] = $Status
}

function Write-RemotePreflightEvidence {
    if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
        return
    }

    $directory = Split-Path -Path $EvidencePath -Parent
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $authUrlShapePassed =
        @($script:CompletedChecks | Where-Object {
            $_ -eq 'remote auth URL shape' -or $_ -eq 'selected tenant and URL shape in local env'
        }).Count -gt 0

    $remotePreflightChecks = [ordered]@{
        helperAndFixtureFiles = @($script:CompletedChecks | Where-Object { $_ -eq 'expected helper and fixture files exist' }).Count -gt 0
        authUrlShape = $authUrlShapePassed
        fixtureCatalog = @($script:CompletedChecks | Where-Object { $_ -eq 'fixture catalog validates' }).Count -gt 0
        selfServeWalkthroughContract = @($script:CompletedChecks | Where-Object { $_ -eq 'self-serve walkthrough contract' }).Count -gt 0
        roleSlotFile = if ($RemoteOnly) { 'skipped_remote_only' } elseif (@($script:CompletedChecks | Where-Object { $_ -eq 'auth role-slot file validates' }).Count -gt 0) { 'passed' } else { 'not_run' }
        apiHealth = $script:LiveCheckEvidence['apiHealth']
        apiReadiness = $script:LiveCheckEvidence['apiReadiness']
        webApp = $script:LiveCheckEvidence['webApp']
        authSessionCors = $script:LiveCheckEvidence['authSessionCors']
        loginRedirect = $script:LiveCheckEvidence['loginRedirect']
        databaseCounts = $script:LiveCheckEvidence['databaseCounts']
    }

    $remotePreflightEvidence = [ordered]@{
        schemaVersion = 1
        generatedAt = [DateTimeOffset]::UtcNow.ToString('o')
        runner = 'deploy/staging/smoke-validation-demo-preflight.ps1'
        status = 'passed'
        mode = if ($RemoteOnly) { 'remote' } else { 'local' }
        validationTenantSlug = $TenantSlug
        inputs = [ordered]@{
            tenantSlugConfigured = -not [string]::IsNullOrWhiteSpace($TenantSlug)
            apiOriginConfigured = -not [string]::IsNullOrWhiteSpace($ApiOrigin)
            webOriginConfigured = -not [string]::IsNullOrWhiteSpace($WebOrigin)
            remoteOnly = [bool]$RemoteOnly
            skipLiveChecks = [bool]$SkipLiveChecks
            skipDatabaseChecks = [bool]$SkipDatabaseChecks
        }
        remotePreflightChecks = $remotePreflightChecks
        limitations = @(
            'Q-053 blocks real-person production legal/GDPR/DPA claims; this remote preflight evidence is engineering proof only.',
            'Q-054 blocks outbound operational-notification email routing and claims that operational events are emailed.',
            'Remote preflight evidence records configured booleans and check names only; raw origins, provider redirect URLs, cookies, tokens, auth headers, response bodies, credential values, and connection strings are omitted.'
        )
    }

    $json = $remotePreflightEvidence | ConvertTo-Json -Depth 12
    Set-Content -Path $EvidencePath -Value $json -Encoding utf8
    Write-Host ''
    Write-Host "Remote preflight evidence written to $EvidencePath"
}

function Invoke-Http {
    param(
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers = @{},
        [int]$TimeoutSec = 10
    )

    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds($TimeoutSec)

    try {
        $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), $Url)
        foreach ($key in $Headers.Keys) {
            [void]$request.Headers.TryAddWithoutValidation($key, [string]$Headers[$key])
        }

        return $client.SendAsync($request).GetAwaiter().GetResult()
    } finally {
        if ($request) {
            $request.Dispose()
        }
        $client.Dispose()
        $handler.Dispose()
    }
}

function Wait-HttpSuccess {
    param(
        [string]$Url,
        [int]$Attempts = 12
    )

    $lastFailure = 'no response received'
    for ($i = 0; $i -lt $Attempts; $i++) {
        try {
            $response = Invoke-Http -Method 'GET' -Url $Url -TimeoutSec 5
            $statusCode = [int]$response.StatusCode
            if ($statusCode -ge 200 -and $statusCode -lt 300) {
                return $response
            }

            $lastFailure = "HTTP $statusCode"
        } catch {
            $lastFailure = Get-SafeMessage $_
        }

        Start-Sleep -Seconds 2
    }

    throw "Timed out waiting for successful response from $Url. Last failure: $lastFailure"
}

function Assert-CorsHeaders {
    param(
        [System.Net.Http.HttpResponseMessage]$Response,
        [string]$ExpectedOrigin
    )

    Assert-True ($Response.Headers.Contains('Access-Control-Allow-Origin')) 'Access-Control-Allow-Origin header is missing.'
    $origins = @($Response.Headers.GetValues('Access-Control-Allow-Origin'))
    Assert-True ($origins -contains $ExpectedOrigin) 'Access-Control-Allow-Origin does not match the configured web origin.'

    Assert-True ($Response.Headers.Contains('Access-Control-Allow-Credentials')) 'Access-Control-Allow-Credentials header is missing.'
    $credentials = @($Response.Headers.GetValues('Access-Control-Allow-Credentials'))
    Assert-True ($credentials -contains 'true') 'Access-Control-Allow-Credentials must be true.'
}

function Invoke-LiveChecks {
    param(
        [string]$ResolvedApiOrigin,
        [string]$ResolvedWebOrigin,
        [hashtable]$Definition,
        [hashtable]$AuthValues
    )

    [void](Wait-HttpSuccess -Url (Join-OriginPath $ResolvedApiOrigin '/health/live'))
    Set-RemotePreflightCheckStatus -Name 'apiHealth' -Status 'passed'
    [void](Wait-HttpSuccess -Url (Join-OriginPath $ResolvedApiOrigin '/health/ready'))
    Set-RemotePreflightCheckStatus -Name 'apiReadiness' -Status 'passed'
    [void](Wait-HttpSuccess -Url (Join-OriginPath $ResolvedWebOrigin '/app'))
    Set-RemotePreflightCheckStatus -Name 'webApp' -Status 'passed'

    $sessionUrl = Join-OriginPath $ResolvedApiOrigin '/auth/session'
    $sessionResponse = Invoke-Http -Method 'GET' -Url $sessionUrl -Headers @{
        Origin = $ResolvedWebOrigin
        'X-Tenant-Id' = $Definition.Id
    }
    Assert-True ([int]$sessionResponse.StatusCode -eq 401) 'Unauthenticated /auth/session must return 401.'
    Assert-CorsHeaders -Response $sessionResponse -ExpectedOrigin $ResolvedWebOrigin

    $preflightResponse = Invoke-Http -Method 'OPTIONS' -Url $sessionUrl -Headers @{
        Origin = $ResolvedWebOrigin
        'Access-Control-Request-Method' = 'GET'
        'Access-Control-Request-Headers' = 'X-Tenant-Id'
    }
    Assert-True ([int]$preflightResponse.StatusCode -eq 204) 'CORS preflight for /auth/session must return 204.'
    Assert-CorsHeaders -Response $preflightResponse -ExpectedOrigin $ResolvedWebOrigin
    Set-RemotePreflightCheckStatus -Name 'authSessionCors' -Status 'passed'

    $loginUrl = Get-EnvValue $AuthValues 'PUBLIC_AUTH_LOGIN_URL' ''
    $loginUrl = Add-QueryParameterIfMissing -Url $loginUrl -Name 'tenantId' -Value $Definition.Id
    if (-not $NoPromptLogin) {
        $loginUrl = Add-QueryParameterIfMissing -Url $loginUrl -Name 'prompt' -Value 'login'
    }

    $loginResponse = Invoke-Http -Method 'GET' -Url $loginUrl
    $statusCode = [int]$loginResponse.StatusCode
    Assert-True ($statusCode -ge 300 -and $statusCode -lt 400) '/auth/login must return a provider redirect without following it.'
    Set-RemotePreflightCheckStatus -Name 'loginRedirect' -Status 'passed'
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
        throw 'docker.exe was not found for database count checks.'
    }

    return $docker.Source
}

function Invoke-DatabaseCountChecks {
    param(
        [string]$TenantId,
        [hashtable]$EnvValues
    )

    $docker = Get-DockerCommand
    $dbUser = Get-EnvValue -Values $EnvValues -Name 'POSTGRES_USER' -Default 'platform'
    $dbName = Get-EnvValue -Values $EnvValues -Name 'POSTGRES_DB' -Default 'platform'

    $sql = @"
BEGIN;
SELECT set_config('app.current_tenant_id', '$TenantId', true);
SELECT 'tenant=' || COUNT(*) FROM tenant WHERE id = '$TenantId'::uuid;
SELECT 'role_assignment=' || COUNT(*) FROM role_assignment WHERE tenant_id = '$TenantId'::uuid;
SELECT 'campaign_series=' || COUNT(*) FROM campaign_series WHERE tenant_id = '$TenantId'::uuid;
SELECT 'instrument=' || COUNT(*) FROM instrument WHERE tenant_id = '$TenantId'::uuid;
SELECT 'campaign=' || COUNT(*) FROM campaign WHERE tenant_id = '$TenantId'::uuid;
SELECT 'export_artifact=' || COUNT(*) FROM export_artifact WHERE tenant_id = '$TenantId'::uuid;
COMMIT;
"@

    Push-Location $repoRoot
    try {
        $arguments = @(
            'compose',
            '--env-file', $EnvFile,
            '-f', $composeFile,
            '-f', $vpsComposeFile,
            'exec', '-T', 'postgres',
            'psql', '-v', 'ON_ERROR_STOP=1',
            '-U', $dbUser,
            '-d', $dbName,
            '-t', '-A'
        )

        $output = $sql | & $docker @arguments 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw 'Database count check failed. Inspect local Compose Postgres logs; command output was intentionally not echoed.'
        }
    } finally {
        Pop-Location
    }

    $counts = @{}
    foreach ($line in @($output)) {
        $trimmed = ([string]$line).Trim()
        if ($trimmed -match '^([a-z_]+)=(\d+)$') {
            $counts[$matches[1]] = [int]$matches[2]
        }
    }

    foreach ($key in @('tenant', 'role_assignment', 'campaign_series', 'instrument', 'campaign', 'export_artifact')) {
        Assert-True ($counts.ContainsKey($key)) "Database count result missing $key."
        Write-Host "  $key=$($counts[$key])"
    }

    Assert-True ($counts['tenant'] -eq 1) 'Selected tenant row is missing.'
    Assert-True ($counts['role_assignment'] -ge 1) 'Selected tenant has no role assignments. Run bootstrap-validation-demo-auth.ps1.'
    Assert-True ($counts['campaign_series'] -ge 1) 'Selected tenant has no campaign series. Run seed-validation-demo.ps1.'
    Assert-True ($counts['instrument'] -ge 1) 'Selected tenant has no instruments. Run seed-validation-demo.ps1.'
    Assert-True ($counts['campaign'] -ge 1) 'Selected tenant has no campaigns. Run seed-validation-demo.ps1.'
    Assert-True ($counts['export_artifact'] -ge 1) 'Selected tenant has no export artifacts. Run seed-validation-demo.ps1.'
}

function Read-ValidationDemoCatalog {
    Assert-True (Test-Path -LiteralPath $catalogFile) "Missing validation demo catalog: $catalogFile"
    return Get-Content -LiteralPath $catalogFile -Raw | ConvertFrom-Json
}

function Get-ValidationDemoCatalogTenant {
    param(
        [object]$Catalog,
        [string]$Slug
    )

    $matches = @($Catalog.tenants | Where-Object { $_.slug -eq $Slug })
    Assert-True ($matches.Count -eq 1) "Validation demo catalog must contain exactly one tenant with slug '$Slug'."
    return $matches[0]
}

function Get-RequiredCatalogText {
    param(
        [object]$Object,
        [string]$Name,
        [string]$Context
    )

    $property = $Object.PSObject.Properties[$Name]
    Assert-True ($null -ne $property) "$Context.$Name is missing."
    $value = [string]$property.Value
    Assert-True (-not [string]::IsNullOrWhiteSpace($value)) "$Context.$Name must not be blank."
    return $value
}

function Assert-SelfServeWalkthroughContract {
    param(
        [object]$CatalogTenant,
        [string]$ResolvedWebOrigin
    )

    Assert-True ($null -ne $CatalogTenant.story) "Tenant $($CatalogTenant.slug) must define story metadata."
    Assert-True ($null -ne $CatalogTenant.story.sampleScenarios) "Tenant $($CatalogTenant.slug) must define story.sampleScenarios."

    $story = $CatalogTenant.story
    $sampleScenarios = $story.sampleScenarios
    $setupName = Get-RequiredCatalogText $story 'setupSeriesName' "Tenant $($CatalogTenant.slug) story"
    $collectionName = Get-RequiredCatalogText $story 'inCollectionSeriesName' "Tenant $($CatalogTenant.slug) story"
    $resultsName = Get-RequiredCatalogText $story 'mainSeriesName' "Tenant $($CatalogTenant.slug) story"
    $wavesName = Get-RequiredCatalogText $story 'linkedWaveSeriesName' "Tenant $($CatalogTenant.slug) story"

    $setupScenario = Get-RequiredCatalogText $sampleScenarios 'setupSeries' "Tenant $($CatalogTenant.slug) story.sampleScenarios"
    $collectionScenario = Get-RequiredCatalogText $sampleScenarios 'inCollectionSeries' "Tenant $($CatalogTenant.slug) story.sampleScenarios"
    $resultsScenario = Get-RequiredCatalogText $sampleScenarios 'mainSeries' "Tenant $($CatalogTenant.slug) story.sampleScenarios"
    $wavesScenario = Get-RequiredCatalogText $sampleScenarios 'linkedWaveSeries' "Tenant $($CatalogTenant.slug) story.sampleScenarios"

    Assert-True (@('setup', 'blocked') -contains $setupScenario) "Setup sample scenario must be setup or blocked."
    Assert-True ($collectionScenario -eq 'in_collection') 'Collection sample scenario must be in_collection.'
    Assert-True (@('mixed_lifecycle', 'completed') -contains $resultsScenario) 'Results sample scenario must be mixed_lifecycle or completed.'
    Assert-True ($wavesScenario -eq 'longitudinal') 'Longitudinal sample scenario must be longitudinal.'

    Write-Host 'Self-serve walkthrough route checklist:'
    Write-Host "  Home: $(Join-OriginPath $ResolvedWebOrigin '/app')"
    Write-Host "  Studies: $(Join-OriginPath $ResolvedWebOrigin '/app/campaign-series')"
    Write-Host "  Setup sample: $setupName -> expect Setup sample read-only state."
    Write-Host "  Collection sample: $collectionName -> expect Collection sample read-only state."
    Write-Host "  Results sample: $resultsName -> expect Results sample read-only state."
    Write-Host "  Longitudinal sample: $wavesName -> expect Longitudinal sample read-only state."
    Write-Host '  Setup-manager role slots should expose Duplicate as study on sample rows and selected sample overview.'
    Write-Host '  Analyst/viewer smoke role slots should inspect samples without setup mutation buttons.'
}

Assert-True $tenantDefinitions.ContainsKey($TenantSlug) "Unknown validation tenant '$TenantSlug'. Use one of: $($tenantDefinitions.Keys -join ', ')."

$definition = $tenantDefinitions[$TenantSlug]
if ($RemoteOnly) {
    Assert-True (-not [string]::IsNullOrWhiteSpace($ApiOrigin)) 'RemoteOnly requires -ApiOrigin.'
    Assert-True (-not [string]::IsNullOrWhiteSpace($WebOrigin)) 'RemoteOnly requires -WebOrigin.'
    $envValues = @{}
    $resolvedApiOrigin = Normalize-Origin $ApiOrigin 'ApiOrigin'
    $resolvedWebOrigin = Normalize-Origin $WebOrigin 'WebOrigin'
    $authValues = Build-RemoteAuthUrls -ResolvedApiOrigin $resolvedApiOrigin -ResolvedWebOrigin $resolvedWebOrigin -Definition $definition
} else {
    $envValues = Read-EnvFile -Path $EnvFile

    $resolvedApiOrigin = Normalize-Origin `
        (Get-FirstValue @($ApiOrigin, $envValues['STAGING_API_ORIGIN'], $envValues['PUBLIC_API_BASE_URL']) 'http://127.0.0.1:5055') `
        'ApiOrigin'
    $resolvedWebOrigin = Normalize-Origin `
        (Get-FirstValue @($WebOrigin, $envValues['STAGING_WEB_ORIGIN'], $envValues['Cors__AllowedOrigins__0']) 'http://127.0.0.1:5174') `
        'WebOrigin'
    $authValues = $envValues
}

Write-Host "Validation preflight tenant: $TenantSlug"
Write-Host "Tenant id: $($definition.Id)"
Write-Host "Primary walkthrough role slots: $($definition.PrimaryRoles -join ', ')"
Write-Host "Membership smoke role slots: $($definition.SmokeRoles -join ', ')"
Write-Host "App URL: $(Join-OriginPath $resolvedWebOrigin '/app')"

Invoke-Check 'expected helper and fixture files exist' {
    $requiredPaths = @($seedScript)
    if (-not $RemoteOnly) {
        $requiredPaths += @($authBootstrapScript, $tenantSwitchScript, $UsersFile)
    }

    foreach ($path in $requiredPaths) {
        Assert-True (Test-Path -LiteralPath $path) "Missing expected file: $path"
    }
}

if ($RemoteOnly) {
    Write-Host '[SKIP] Skipping local env role-slot tenant-switch and database checks for RemoteOnly'
    Invoke-Check 'remote auth URL shape' {
        Assert-AuthUrlShape -EnvValues $authValues -Definition $definition -ResolvedApiOrigin $resolvedApiOrigin -ResolvedWebOrigin $resolvedWebOrigin
    }
} else {
    Invoke-Check 'selected tenant and URL shape in local env' {
        Assert-True ((Get-EnvValue $envValues 'PUBLIC_TENANT_ID' '') -eq $definition.Id) 'PUBLIC_TENANT_ID does not match the requested validation tenant. Run select-validation-demo-tenant.ps1 first.'
        Assert-True ((Get-EnvValue $envValues 'PUBLIC_API_BASE_URL' '') -eq $resolvedApiOrigin) 'PUBLIC_API_BASE_URL does not match the resolved API origin.'
        Assert-True ((Get-EnvValue $envValues 'Cors__AllowedOrigins__0' '') -eq $resolvedWebOrigin) 'Cors__AllowedOrigins__0 does not match the resolved web origin.'
        Assert-AuthUrlShape -EnvValues $envValues -Definition $definition -ResolvedApiOrigin $resolvedApiOrigin -ResolvedWebOrigin $resolvedWebOrigin
    }

    Invoke-Check 'tenant switch helper validates without writing' {
        & $tenantSwitchScript $TenantSlug -EnvFile $EnvFile -ValidateOnly -ApiOrigin $resolvedApiOrigin -WebOrigin $resolvedWebOrigin -NoPromptLogin:$NoPromptLogin *> $null
    }
}

Invoke-Check 'fixture catalog validates' {
    & $seedScript -ValidateOnly *> $null
}

Invoke-Check 'self-serve walkthrough contract' {
    $catalog = Read-ValidationDemoCatalog
    $catalogTenant = Get-ValidationDemoCatalogTenant -Catalog $catalog -Slug $TenantSlug
    Assert-SelfServeWalkthroughContract -CatalogTenant $catalogTenant -ResolvedWebOrigin $resolvedWebOrigin
}

if (-not $RemoteOnly) {
    Invoke-Check 'auth role-slot file validates' {
        & $authBootstrapScript -UsersFile $UsersFile -ValidateOnly -AllowPlaceholderEmails:$AllowPlaceholderEmails *> $null
    }
}

if ($SkipLiveChecks) {
    Write-Host '[SKIP] live API/web/session/CORS/login checks'
} else {
    Invoke-Check 'live API web session CORS and login redirect checks' {
        Invoke-LiveChecks -ResolvedApiOrigin $resolvedApiOrigin -ResolvedWebOrigin $resolvedWebOrigin -Definition $definition -AuthValues $authValues
    }

    if ($RemoteOnly -or $SkipDatabaseChecks) {
        Write-Host '[SKIP] database selected-tenant counts'
    } else {
        Invoke-Check 'database selected-tenant counts' {
            Invoke-DatabaseCountChecks -TenantId $definition.Id -EnvValues $envValues
            Set-RemotePreflightCheckStatus -Name 'databaseCounts' -Status 'passed'
        }
    }
}

if ($script:Failures.Count -gt 0) {
    throw "Validation demo preflight failed: $($script:Failures -join ', ')."
}

Write-RemotePreflightEvidence

Write-Host 'Validation demo preflight passed.'
Write-Host 'Next: open the app URL and sign in with a matching owner or researcher role slot.'
