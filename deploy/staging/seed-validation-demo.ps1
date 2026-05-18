param(
    [string]$ApiBaseUrl,
    [string]$WebBaseUrl,
    [string]$RunTag,
    [switch]$ValidateOnly,
    [switch]$SkipTenantBootstrap,
    [switch]$SkipWebCheck,
    [switch]$AllowDuplicateSeed
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$envFile = Join-Path $repoRoot 'deploy\staging\.env'
$composeFile = Join-Path $repoRoot 'deploy\staging\docker-compose.yml'
$fixtureDir = Join-Path $PSScriptRoot 'validation-demo-fixtures'
$catalogFile = Join-Path $fixtureDir 'validation-demo-catalog.json'
$tenantBootstrapFile = Join-Path $fixtureDir 'tenant-bootstrap.sql'
$MinimumInstrumentQuestionCount = 8
$KnownSampleScenarios = @(
    'mixed_lifecycle',
    'longitudinal',
    'setup',
    'in_collection',
    'completed',
    'blocked'
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

function Join-Url {
    param(
        [string]$BaseUrl,
        [string]$Path
    )

    return $BaseUrl.TrimEnd('/') + '/' + $Path.TrimStart('/')
}

function ConvertTo-JsonPayload {
    param([object]$Body)

    return $Body | ConvertTo-Json -Depth 60 -Compress
}

function Get-HttpErrorMessage {
    param(
        [string]$Method,
        [string]$Url,
        [object]$ErrorRecord
    )

    $detail = $ErrorRecord.Exception.Message
    if ($ErrorRecord.ErrorDetails -and $ErrorRecord.ErrorDetails.Message) {
        $detail = $ErrorRecord.ErrorDetails.Message
    } elseif ($ErrorRecord.Exception.Response) {
        try {
            $stream = $ErrorRecord.Exception.Response.GetResponseStream()
            if ($stream) {
                $reader = New-Object System.IO.StreamReader($stream)
                $body = $reader.ReadToEnd()
                if ($body) {
                    $detail = "$detail Body: $body"
                }
            }
        } catch {
        }
    }

    return "$Method $Url failed. $detail"
}

function Invoke-Json {
    param(
        [ValidateSet('GET', 'POST', 'PUT', 'PATCH')]
        [string]$Method,
        [string]$Path,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [int]$TimeoutSec = 30
    )

    $url = Join-Url $ApiBaseUrl $Path
    $parameters = @{
        Uri = $url
        Method = $Method
        Headers = $Headers
        TimeoutSec = $TimeoutSec
        ErrorAction = 'Stop'
    }

    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = ConvertTo-JsonPayload $Body
    }

    try {
        return Invoke-RestMethod @parameters
    } catch {
        throw (Get-HttpErrorMessage $Method $url $_)
    }
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

function Read-FixtureCatalog {
    Assert-True (Test-Path -LiteralPath $catalogFile) "Missing fixture catalog: $catalogFile"
    return Get-Content -LiteralPath $catalogFile -Raw | ConvertFrom-Json
}

function Validate-FixtureCatalog {
    param([object]$Catalog)

    Assert-True ($Catalog.environmentLabel -eq 'proof_demo') 'Catalog environmentLabel must be proof_demo.'
    Assert-True ($Catalog.productionData -eq $false) 'Catalog must not contain production data.'
    Assert-True ($Catalog.platformCanonicalInstruments -eq $false) 'Catalog must not mark instruments as platform-canonical.'

    $expectedSlugs = @(
        'validation-oh-research',
        'validation-se-education',
        'validation-osh-consulting'
    )
    $seenSlugs = @{}
    foreach ($tenant in @($Catalog.tenants)) {
        $seenSlugs[[string]$tenant.slug] = $true
        Assert-True ($tenant.productionData -eq $false) "Tenant $($tenant.slug) must not contain production data."
        Assert-True ($tenant.platformCanonical -eq $false) "Tenant $($tenant.slug) must not be platform-canonical."
        Assert-True (@($tenant.instruments).Count -ge 3) "Tenant $($tenant.slug) must include at least three instruments."
        Assert-True ($null -ne $tenant.story) "Tenant $($tenant.slug) must include story metadata."
        Assert-StoryName ([string]$tenant.story.setupSeriesName) "Tenant $($tenant.slug) story.setupSeriesName"
        Assert-StoryName ([string]$tenant.story.inCollectionSeriesName) "Tenant $($tenant.slug) story.inCollectionSeriesName"
        Assert-StoryName ([string]$tenant.story.mainSeriesName) "Tenant $($tenant.slug) story.mainSeriesName"
        Assert-StoryName ([string]$tenant.story.linkedWaveSeriesName) "Tenant $($tenant.slug) story.linkedWaveSeriesName"
        Assert-True ($null -ne $tenant.story.sampleScenarios) "Tenant $($tenant.slug) must include story.sampleScenarios."
        Assert-StarterSampleCoverage $tenant

        foreach ($campaignName in @('draft', 'liveNoResponses', 'partial', 'completed', 'wave1', 'wave2')) {
            $storyName = Get-CampaignStoryName $tenant $campaignName
            Assert-StoryName $storyName "Tenant $($tenant.slug) campaignNames.$campaignName"
        }

        foreach ($profileName in @('partial', 'completed', 'waveBaseline', 'waveComparison')) {
            $profile = Get-ResponseProfileBaseValues $tenant $profileName
            Assert-True ($profile.Count -ge 1) "Tenant $($tenant.slug) responseProfiles.$profileName must not be empty."
            foreach ($value in $profile) {
                Assert-True ($value -ge 1 -and $value -le 5) "Tenant $($tenant.slug) responseProfiles.$profileName values must be between 1 and 5."
            }
        }

        Assert-True ((Get-ResponseProfileBaseValues $tenant 'completed').Count -ge 5) "Tenant $($tenant.slug) completed profile must include at least five submitted responses."
        Assert-True ((Get-ResponseProfileBaseValues $tenant 'waveBaseline').Count -ge 5) "Tenant $($tenant.slug) baseline wave profile must include at least five linked responses."
        Assert-True ((Get-ResponseProfileBaseValues $tenant 'waveComparison').Count -ge 5) "Tenant $($tenant.slug) comparison wave profile must include at least five linked responses."
        Assert-DirectoryFixture $tenant

        foreach ($instrument in @($tenant.instruments)) {
            Assert-True ($instrument.platformCanonical -eq $false) "Instrument $($instrument.code) must not be platform-canonical."
            Assert-True ($instrument.availableToOtherTenants -eq $false) "Instrument $($instrument.code) must be tenant-private."
            Assert-True (@('psychometric', 'ergonomic', 'medical', 'educational', 'regulatory', 'other') -contains [string]$instrument.domain) "Instrument $($instrument.code) has an unknown domain."
            Assert-True (@($instrument.questions).Count -ge $MinimumInstrumentQuestionCount) "Instrument $($instrument.code) must include at least $MinimumInstrumentQuestionCount questions."
            Assert-True (@($instrument.scoreOutputs).Count -ge 1) "Instrument $($instrument.code) must include at least one score output."
        }

        $stateKinds = @{}
        foreach ($state in @($tenant.proofStates)) {
            $stateKinds[[string]$state.kind] = $true
        }

        foreach ($requiredState in @('draft', 'live_no_responses', 'partial_response', 'completed_scored', 'closed_wave', 'export')) {
            Assert-True $stateKinds.ContainsKey($requiredState) "Tenant $($tenant.slug) is missing proof state $requiredState."
        }
    }

    foreach ($slug in $expectedSlugs) {
        Assert-True $seenSlugs.ContainsKey($slug) "Catalog is missing tenant $slug."
    }
}

function Invoke-TenantBootstrap {
    param(
        [hashtable]$EnvValues,
        [switch]$Skip
    )

    if ($Skip) {
        Write-Host 'Skipping tenant bootstrap; assuming tenant rows already exist.'
        return
    }

    Assert-True (Test-Path -LiteralPath $envFile) "Missing staging env file: $envFile. Run deploy/staging/start-local-staging.ps1 first."
    Assert-True (Test-Path -LiteralPath $tenantBootstrapFile) "Missing tenant-bootstrap.sql: $tenantBootstrapFile"

    $dbUser = Get-EnvValue $EnvValues 'POSTGRES_USER' 'platform'
    $dbName = Get-EnvValue $EnvValues 'POSTGRES_DB' 'platform'
    $sql = Get-Content -LiteralPath $tenantBootstrapFile -Raw

    Push-Location $repoRoot
    try {
        $sql | docker compose --env-file $envFile -f $composeFile exec -T postgres psql -v ON_ERROR_STOP=1 -U $dbUser -d $dbName
        if ($LASTEXITCODE -ne 0) {
            throw "Tenant bootstrap failed with exit code $LASTEXITCODE."
        }
    } finally {
        Pop-Location
    }
}

function Invoke-PostgresRows {
    param(
        [hashtable]$EnvValues,
        [string]$Sql
    )

    Assert-True (Test-Path -LiteralPath $envFile) "Missing staging env file: $envFile. Run deploy/staging/start-local-staging.ps1 first."

    $dbUser = Get-EnvValue $EnvValues 'POSTGRES_USER' 'platform'
    $dbName = Get-EnvValue $EnvValues 'POSTGRES_DB' 'platform'

    Push-Location $repoRoot
    try {
        $output = $Sql | docker compose --env-file $envFile -f $composeFile exec -T postgres psql -v ON_ERROR_STOP=1 -U $dbUser -d $dbName -t -A 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw 'Postgres validation demo count query failed. Inspect local Compose Postgres logs; command output was intentionally not echoed.'
        }

        return @($output)
    } finally {
        Pop-Location
    }
}

function Assert-ValidationTenantsEmpty {
    param(
        [hashtable]$EnvValues,
        [object]$Catalog,
        [switch]$AllowDuplicateSeed
    )

    if ($AllowDuplicateSeed) {
        Write-Host 'AllowDuplicateSeed set; duplicate validation demo data check skipped.'
        return
    }

    $tenantRows = @()
    foreach ($tenant in @($Catalog.tenants)) {
        try {
            [void][Guid]::Parse([string]$tenant.id)
        } catch {
            throw "Validation demo tenant $($tenant.slug) has an invalid tenant id."
        }

        $tenantRows += "('$($tenant.id)'::uuid)"
    }

    Assert-True ($tenantRows.Count -gt 0) 'Validation demo catalog did not include any tenants.'
    $tenantValues = $tenantRows -join ",`n    "

    $sql = @"
WITH validation_tenants(id) AS (
    VALUES
    $tenantValues
)
SELECT 'instrument=' || COUNT(*) FROM instrument WHERE tenant_id IN (SELECT id FROM validation_tenants)
UNION ALL SELECT 'campaign_series=' || COUNT(*) FROM campaign_series WHERE tenant_id IN (SELECT id FROM validation_tenants)
UNION ALL SELECT 'campaign=' || COUNT(*) FROM campaign WHERE tenant_id IN (SELECT id FROM validation_tenants)
UNION ALL SELECT 'response_session=' || COUNT(*) FROM response_session WHERE tenant_id IN (SELECT id FROM validation_tenants)
UNION ALL SELECT 'export_artifact=' || COUNT(*) FROM export_artifact WHERE tenant_id IN (SELECT id FROM validation_tenants)
UNION ALL SELECT 'subject=' || COUNT(*) FROM subject WHERE tenant_id IN (SELECT id FROM validation_tenants)
UNION ALL SELECT 'subject_group=' || COUNT(*) FROM subject_group WHERE tenant_id IN (SELECT id FROM validation_tenants)
UNION ALL SELECT 'subject_relationship=' || COUNT(*) FROM subject_relationship WHERE tenant_id IN (SELECT id FROM validation_tenants);
"@

    $counts = @{}
    foreach ($line in (Invoke-PostgresRows $EnvValues $sql)) {
        $trimmed = ([string]$line).Trim()
        if ($trimmed -match '^([a-z_]+)=(\d+)$') {
            $counts[$matches[1]] = [int]$matches[2]
        }
    }

    $requiredKeys = @(
        'instrument',
        'campaign_series',
        'campaign',
        'response_session',
        'export_artifact',
        'subject',
        'subject_group',
        'subject_relationship'
    )
    $total = 0
    foreach ($key in $requiredKeys) {
        Assert-True ($counts.ContainsKey($key)) "Validation demo duplicate-seed count result missing $key."
        $total += $counts[$key]
    }

    if ($total -gt 0) {
        $summary = ($requiredKeys | ForEach-Object { "$_=$($counts[$_])" }) -join ', '
        throw "Validation tenants already contain validation demo data ($summary). Refusing to append another seed. Reset the disposable staging database, or rerun with -AllowDuplicateSeed if duplicate demo data is intentional."
    }
}

function Set-CampaignSeriesSampleMetadata {
    param(
        [hashtable]$EnvValues,
        [string]$TenantId,
        [string]$SeriesId,
        [string]$SampleScenario
    )

    [void][Guid]::Parse($TenantId)
    [void][Guid]::Parse($SeriesId)
    Assert-SampleScenario $SampleScenario "Campaign series $SeriesId sampleScenario"

    $sql = @"
WITH updated AS (
    UPDATE campaign_series
    SET study_kind = 'sample',
        sample_scenario = '$SampleScenario'
    WHERE tenant_id = '$TenantId'::uuid
      AND id = '$SeriesId'::uuid
    RETURNING id
)
SELECT 'updated=' || COUNT(*) FROM updated;
"@

    $updatedCount = 0
    foreach ($line in (Invoke-PostgresRows $EnvValues $sql)) {
        $trimmed = ([string]$line).Trim()
        if ($trimmed -match '^updated=(\d+)$') {
            $updatedCount = [int]$matches[1]
        }
    }

    Assert-True ($updatedCount -eq 1) "Failed to mark campaign series $SeriesId as sample for tenant $TenantId."
}

function New-Headers {
    param(
        [string]$TenantId,
        [string]$UserId
    )

    return @{
        'X-Tenant-Id' = $TenantId
        'X-Dev-User-Id' = $UserId
        'X-Dev-Tenant-Memberships' = $TenantId
        'X-Dev-Permissions' = 'setup.manage'
    }
}

function New-SafeCode {
    param([string]$Value)

    $safe = $Value.ToLowerInvariant() -replace '[^a-z0-9]+', '-'
    return $safe.Trim('-')
}

function New-SuffixedEmail {
    param(
        [string]$Email,
        [string]$Suffix
    )

    if ([string]::IsNullOrWhiteSpace($Email)) {
        return $null
    }

    $trimmed = $Email.Trim()
    $parts = $trimmed.Split('@', 2)
    if ($parts.Length -ne 2) {
        return $trimmed
    }

    return "$($parts[0])+$Suffix@$($parts[1])"
}

function New-SuffixedExternalId {
    param(
        [string]$ExternalId,
        [string]$Suffix
    )

    if ([string]::IsNullOrWhiteSpace($ExternalId)) {
        return $null
    }

    return "$($ExternalId.Trim())-$Suffix"
}

function New-DirectoryAttributes {
    param([object]$Attributes)

    if ($null -eq $Attributes) {
        return '{}'
    }

    return ConvertTo-JsonPayload $Attributes
}

function Get-RequiredPropertyValue {
    param(
        [object]$Object,
        [string]$Name,
        [string]$Context
    )

    $property = $Object.PSObject.Properties[$Name]
    Assert-True ($null -ne $property) "$Context is missing required property '$Name'."
    Assert-True ($null -ne $property.Value) "$Context property '$Name' must not be null."

    return $property.Value
}

function Assert-StoryName {
    param(
        [string]$Value,
        [string]$Context
    )

    Assert-True (-not [string]::IsNullOrWhiteSpace($Value)) "$Context must not be blank."
    Assert-True (-not ($Value -match '^VAL0[0-9]\b')) "$Context must be a tenant story name, not a generic VAL seed label."
}

function Assert-SampleScenario {
    param(
        [string]$Value,
        [string]$Context
    )

    Assert-True (-not [string]::IsNullOrWhiteSpace($Value)) "$Context must not be blank."
    Assert-True ($KnownSampleScenarios -contains $Value) "$Context has unknown sample scenario '$Value'."
}

function Assert-StarterSampleCoverage {
    param([object]$Tenant)

    $required = @(
        @{ Key = 'setupSeries'; Label = 'setup or blocked'; Allowed = @('setup', 'blocked') },
        @{ Key = 'inCollectionSeries'; Label = 'in-collection or partial'; Allowed = @('in_collection') },
        @{ Key = 'mainSeries'; Label = 'completed or results'; Allowed = @('mixed_lifecycle', 'completed') },
        @{ Key = 'linkedWaveSeries'; Label = 'longitudinal or waves'; Allowed = @('longitudinal') }
    )

    foreach ($entry in $required) {
        $context = "Tenant $($Tenant.slug) story.sampleScenarios.$($entry.Key)"
        $value = [string](Get-RequiredPropertyValue $Tenant.story.sampleScenarios $entry.Key "Tenant $($Tenant.slug) story.sampleScenarios")
        Assert-SampleScenario $value $context
        Assert-True ($entry.Allowed -contains $value) "$context must cover $($entry.Label), got '$value'."
    }
}

function Get-CampaignStoryName {
    param(
        [object]$Tenant,
        [string]$Name
    )

    return [string](Get-RequiredPropertyValue $Tenant.story.campaignNames $Name "Tenant $($Tenant.slug) campaignNames")
}

function Get-ResponseProfileBaseValues {
    param(
        [object]$Tenant,
        [string]$Name
    )

    $profile = @(Get-RequiredPropertyValue $Tenant.story.responseProfiles $Name "Tenant $($Tenant.slug) responseProfiles")
    return @($profile | ForEach-Object { [int]$_ })
}

function Assert-DirectoryFixture {
    param([object]$Tenant)

    $directoryProperty = $Tenant.PSObject.Properties['directory']
    Assert-True ($null -ne $directoryProperty -and $null -ne $directoryProperty.Value) "Tenant $($Tenant.slug) must include directory metadata."
    $directory = $directoryProperty.Value
    $subjects = @($directory.subjects)
    $groups = @($directory.groups)
    $memberships = @($directory.memberships)
    $relationships = @($directory.managerRelationships)

    Assert-True ($subjects.Count -ge 5) "Tenant $($Tenant.slug) directory must include at least five subjects."
    Assert-True ($groups.Count -ge 2) "Tenant $($Tenant.slug) directory must include at least two groups."
    Assert-True ($memberships.Count -ge 4) "Tenant $($Tenant.slug) directory must include at least four memberships."
    Assert-True ($relationships.Count -ge 2) "Tenant $($Tenant.slug) directory must include at least two manager relationships."

    $subjectKeys = @{}
    foreach ($subject in $subjects) {
        $key = [string](Get-RequiredPropertyValue $subject 'key' "Tenant $($Tenant.slug) directory.subjects")
        Assert-True (-not $subjectKeys.ContainsKey($key)) "Tenant $($Tenant.slug) has duplicate subject key $key."
        $subjectKeys[$key] = $true
        $displayName = [string]$subject.displayName
        $email = [string]$subject.email
        $externalId = [string]$subject.externalId
        Assert-True (
            -not [string]::IsNullOrWhiteSpace($displayName) -or
            -not [string]::IsNullOrWhiteSpace($email) -or
            -not [string]::IsNullOrWhiteSpace($externalId)
        ) "Tenant $($Tenant.slug) subject $key must include displayName, email, or externalId."
    }

    $groupKeys = @{}
    foreach ($group in $groups) {
        $key = [string](Get-RequiredPropertyValue $group 'key' "Tenant $($Tenant.slug) directory.groups")
        Assert-True (-not $groupKeys.ContainsKey($key)) "Tenant $($Tenant.slug) has duplicate group key $key."
        $groupKeys[$key] = $true
        Assert-StoryName ([string]$group.type) "Tenant $($Tenant.slug) group $key type"
        Assert-StoryName ([string]$group.name) "Tenant $($Tenant.slug) group $key name"
    }

    foreach ($group in $groups) {
        $parentKey = [string]$group.parentKey
        if (-not [string]::IsNullOrWhiteSpace($parentKey)) {
            Assert-True $groupKeys.ContainsKey($parentKey) "Tenant $($Tenant.slug) group $($group.key) references unknown parent group $parentKey."
        }
    }

    foreach ($membership in $memberships) {
        $subjectKey = [string](Get-RequiredPropertyValue $membership 'subjectKey' "Tenant $($Tenant.slug) directory.memberships")
        $groupKey = [string](Get-RequiredPropertyValue $membership 'groupKey' "Tenant $($Tenant.slug) directory.memberships")
        Assert-True $subjectKeys.ContainsKey($subjectKey) "Tenant $($Tenant.slug) membership references unknown subject $subjectKey."
        Assert-True $groupKeys.ContainsKey($groupKey) "Tenant $($Tenant.slug) membership references unknown group $groupKey."
    }

    foreach ($relationship in $relationships) {
        $managerKey = [string](Get-RequiredPropertyValue $relationship 'managerKey' "Tenant $($Tenant.slug) directory.managerRelationships")
        $subjectKey = [string](Get-RequiredPropertyValue $relationship 'subjectKey' "Tenant $($Tenant.slug) directory.managerRelationships")
        Assert-True $subjectKeys.ContainsKey($managerKey) "Tenant $($Tenant.slug) manager relationship references unknown manager $managerKey."
        Assert-True $subjectKeys.ContainsKey($subjectKey) "Tenant $($Tenant.slug) manager relationship references unknown subject $subjectKey."
        Assert-True ($managerKey -ne $subjectKey) "Tenant $($Tenant.slug) manager relationship cannot be self-referential for $managerKey."
    }
}

function New-ScoringDocument {
    param(
        [string]$RuleKey,
        [object[]]$Questions,
        [object[]]$ScoreOutputs
    )

    $itemCodes = @($Questions | ForEach-Object { [string]$_.code })
    $outputs = @()
    foreach ($output in @($ScoreOutputs)) {
        $outputs += @{
            code = [string]$output
            node = 'mean_score'
        }
    }

    return ConvertTo-JsonPayload @{
        rule_id = $RuleKey
        rule_version = '1.0.0'
        schema_version = '1.0.0'
        engine_min_version = '1.0.0'
        scale_defaults = @{
            response = @{
                min = 1
                max = 5
            }
        }
        inputs = @(
            @{
                id = 'core_items'
                kind = 'answers'
                items = $itemCodes
            }
        )
        nodes = @(
            @{
                id = 'core_answers'
                op = 'select_answers'
                input = 'core_items'
            },
            @{
                id = 'mean_score'
                op = 'mean'
                input = 'core_answers'
            }
        )
        outputs = $outputs
        missing_data = @{
            defaults = @{
                strategy = 'require_all'
            }
        }
    }
}

function New-ProducesDocument {
    param(
        [object[]]$ScoreOutputs,
        [string]$Provenance
    )

    $bands = @{}
    foreach ($output in @($ScoreOutputs)) {
        $bands[[string]$output] = @(
            @{
                code = 'lower'
                label = 'Synthetic lower range'
                min = 1
                max = 2.49
            },
            @{
                code = 'middle'
                label = 'Synthetic middle range'
                min = 2.5
                max = 3.49
            },
            @{
                code = 'higher'
                label = 'Synthetic higher range'
                min = 3.5
                max = 5
            }
        )
    }

    return ConvertTo-JsonPayload @{
        scores = @($ScoreOutputs | ForEach-Object { [string]$_ })
        interpretation = @{
            status = 'tenant_attested'
            source = 'tenant_defined'
            provenance = $Provenance
            scores = $bands
        }
    }
}

function New-TemplateRequest {
    param(
        [object]$Tenant,
        [object]$Instrument,
        [string]$Suffix,
        [string]$InstrumentId
    )

    $questions = @()
    $ordinal = 1
    foreach ($question in @($Instrument.questions)) {
        $questions += @{
            ordinal = $ordinal
            code = [string]$question.code
            type = 'likert'
            textDefault = [string]$question.prompt
            sectionCode = 'core'
            scaleCode = 'response'
            required = $true
            reverseCoded = $false
            measurementLevel = 'ordinal'
            payload = '{}'
            missingCodes = '[]'
        }
        $ordinal++
    }

    return @{
        templateName = "$($Instrument.name) $Suffix"
        semver = '1.0.0'
        defaultLocale = [string]$Tenant.defaultLocale
        instrumentId = $InstrumentId
        sections = @(
            @{
                ordinal = 1
                code = 'core'
                titleDefault = 'Core'
            }
        )
        scales = @(
            @{
                code = 'response'
                type = 'likert'
                minValue = 1
                maxValue = 5
                step = 1
                naAllowed = $false
                anchors = '[{"value":1,"label":"Very low"},{"value":5,"label":"Very high"}]'
            }
        )
        questions = $questions
    }
}

function New-AnswersRequest {
    param(
        [object[]]$Questions,
        [string[]]$Values
    )

    $answers = @()
    for ($i = 0; $i -lt $Questions.Count; $i++) {
        $answers += @{
            questionId = $Questions[$i].id
            value = $Values[$i]
            isSkipped = $false
            isNa = $false
        }
    }

    return @{ answers = $answers }
}

function New-ResponseValues {
    param(
        [object[]]$Questions,
        [int]$BaseValue
    )

    $values = @()
    for ($i = 0; $i -lt $Questions.Count; $i++) {
        $value = $BaseValue + ($i % 2)
        if ($value -gt 5) {
            $value = 5
        }
        if ($value -lt 1) {
            $value = 1
        }

        $values += [string]$value
    }

    return $values
}

function New-DemoInstrument {
    param(
        [hashtable]$Headers,
        [object]$Tenant,
        [object]$Instrument,
        [string]$Suffix
    )

    $safeInstrumentCode = New-SafeCode ([string]$Instrument.code)
    $ruleKey = "val08.$($Tenant.slug).$safeInstrumentCode.$Suffix.mean"
    $provenance = "VAL08 proof fixture for $($Tenant.slug): $($Instrument.reportLabel)"

    $instrumentResponse = Invoke-Json POST '/instruments/private-imports' $Headers @{
        code = "$safeInstrumentCode-$Suffix"
        version = '1.0.0'
        fullName = "$($Instrument.name) [$Suffix]"
        domain = [string]$Instrument.domain
        provenanceNote = "$provenance $($Instrument.provenanceNote)"
        rightsStatus = 'attested_by_tenant'
        validityLabel = 'tenant_provided'
        licenseType = 'unknown'
    }

    $template = Invoke-Json POST '/template-versions' $Headers (
        New-TemplateRequest $Tenant $Instrument $Suffix $instrumentResponse.id
    )

    $scoring = Invoke-Json POST '/scoring-rules' $Headers @{
        templateVersionId = $template.templateVersionId
        ruleKey = $ruleKey
        ruleVersion = '1.0.0'
        schemaVersion = 'scoring-rule/v1'
        engineMinVersion = 'engine/v1'
        document = New-ScoringDocument $ruleKey @($Instrument.questions) @($Instrument.scoreOutputs)
        produces = New-ProducesDocument @($Instrument.scoreOutputs) $provenance
        compatibility = '{}'
    }

    return @{
        catalog = $Instrument
        instrument = $instrumentResponse
        template = $template
        scoring = $scoring
    }
}

function New-DirectoryHierarchy {
    param(
        [hashtable]$Headers,
        [object]$Tenant,
        [string]$Suffix
    )

    $subjectIds = @{}
    foreach ($subject in @($Tenant.directory.subjects)) {
        $locale = [string]$subject.locale
        if ([string]::IsNullOrWhiteSpace($locale)) {
            $locale = [string]$Tenant.defaultLocale
        }

        $created = Invoke-Json POST '/subjects' $Headers @{
            displayName = [string]$subject.displayName
            email = New-SuffixedEmail ([string]$subject.email) $Suffix
            externalId = New-SuffixedExternalId ([string]$subject.externalId) $Suffix
            locale = $locale
            attributes = New-DirectoryAttributes $subject.attributes
        }

        $subjectIds[[string]$subject.key] = $created.id
    }

    $groupIds = @{}
    foreach ($group in @($Tenant.directory.groups)) {
        $parentGroupId = $null
        $parentKey = [string]$group.parentKey
        if (-not [string]::IsNullOrWhiteSpace($parentKey)) {
            $parentGroupId = $groupIds[$parentKey]
        }

        $created = Invoke-Json POST '/subject-groups' $Headers @{
            type = [string]$group.type
            name = "$($group.name) $Suffix"
            parentGroupId = $parentGroupId
            attributes = New-DirectoryAttributes $group.attributes
        }

        $groupIds[[string]$group.key] = $created.id
    }

    foreach ($membership in @($Tenant.directory.memberships)) {
        $roleInGroup = [string]$membership.roleInGroup
        if ([string]::IsNullOrWhiteSpace($roleInGroup)) {
            $roleInGroup = $null
        }

        [void](Invoke-Json POST "/subject-groups/$($groupIds[[string]$membership.groupKey])/members" $Headers @{
            subjectId = $subjectIds[[string]$membership.subjectKey]
            roleInGroup = $roleInGroup
        })
    }

    foreach ($relationship in @($Tenant.directory.managerRelationships)) {
        [void](Invoke-Json PUT "/subjects/$($subjectIds[[string]$relationship.subjectKey])/manager" $Headers @{
            managerSubjectId = $subjectIds[[string]$relationship.managerKey]
        })
    }

    $directory = Invoke-Json GET '/subjects' $Headers
    $groups = Invoke-Json GET '/subject-groups' $Headers
    Assert-True ($directory.summary.subjectCount -ge $subjectIds.Count) "Directory subject count did not include seeded subjects for $($Tenant.slug)."
    Assert-True ($groups.groups.Count -ge $groupIds.Count) "Directory group count did not include seeded groups for $($Tenant.slug)."
    Assert-True ($directory.summary.managerRelationshipCount -ge @($Tenant.directory.managerRelationships).Count) "Directory manager relationship count did not include seeded relationships for $($Tenant.slug)."

    return @{
        subjectCount = $subjectIds.Count
        groupCount = $groupIds.Count
        managerRelationshipCount = @($Tenant.directory.managerRelationships).Count
    }
}

function New-Campaign {
    param(
        [hashtable]$Headers,
        [string]$Name,
        [string]$TemplateVersionId,
        [string]$SeriesId,
        [string]$ResponseIdentityMode
    )

    return Invoke-Json POST '/campaigns' $Headers @{
        templateVersionId = $TemplateVersionId
        name = $Name
        responseIdentityMode = $ResponseIdentityMode
        campaignSeriesId = $SeriesId
        schedule = '{}'
        defaultLocale = 'en'
    }
}

function New-LaunchedCampaign {
    param(
        [hashtable]$Headers,
        [string]$Name,
        [string]$TemplateVersionId,
        [string]$SeriesId,
        [string]$ResponseIdentityMode
    )

    $campaign = New-Campaign $Headers $Name $TemplateVersionId $SeriesId $ResponseIdentityMode
    $readiness = Invoke-Json GET "/campaigns/$($campaign.id)/launch-readiness" $Headers
    Assert-True $readiness.ready "$Name is not launch-ready."
    $launch = Invoke-Json POST "/campaigns/$($campaign.id)/launch" $Headers @{}
    $openLink = Invoke-Json POST "/campaigns/$($campaign.id)/open-link" $Headers @{}

    return @{
        campaign = $campaign
        readiness = $readiness
        launch = $launch
        openLink = $openLink
    }
}

function Submit-OpenLinkResponse {
    param(
        [object]$OpenLink,
        [string[]]$Values,
        [bool]$Submit,
        [string]$ParticipantCode
    )

    $token = $OpenLink.token
    $entry = Invoke-Json GET "/respondent/open-links/$token"
    Assert-True (@($entry.questions).Count -eq $Values.Count) "Open-link entry question count did not match generated answers."

    $sessionBody = @{
        locale = 'en'
        acceptedConsentDocumentId = $entry.consentDocument.id
        acceptedGrants = $entry.consentDocument.requiredGrants
    }

    if ($ParticipantCode) {
        Assert-True ($entry.requiresParticipantCode -eq $true) 'Expected participant-code flow for linked wave.'
        $sessionBody.participantCode = $ParticipantCode
    }

    $session = Invoke-Json POST "/respondent/open-links/$token/sessions" @{} $sessionBody
    $saved = Invoke-Json PUT "/respondent/open-links/$token/sessions/$($session.id)/answers" @{} (
        New-AnswersRequest @($entry.questions) $Values
    )
    Assert-True ($saved.savedAnswerCount -eq $Values.Count) "Saved answer count was $($saved.savedAnswerCount), expected $($Values.Count)."

    $submitted = $null
    if ($Submit) {
        $submitted = Invoke-Json POST "/respondent/open-links/$token/sessions/$($session.id)/submit" @{} @{
            timeTakenMs = 1800
        }
    }

    return @{
        entry = $entry
        session = $session
        saved = $saved
        submitted = $submitted
    }
}

function Invoke-ProductSurfaceChecks {
    param(
        [hashtable]$Headers,
        [string]$SeriesId,
        [string]$SampleScenario
    )

    $overview = Invoke-Json GET '/workspace-overview' $Headers
    $hub = Invoke-Json GET "/campaign-series/$SeriesId" $Headers
    $setup = Invoke-Json GET "/campaign-series/$SeriesId/setup-workspace" $Headers
    $operations = Invoke-Json GET "/campaign-series/$SeriesId/operations-workspace" $Headers
    $reports = Invoke-Json GET "/campaign-series/$SeriesId/reports-workspace" $Headers
    $waves = Invoke-Json GET "/campaign-series/$SeriesId/waves-workspace" $Headers

    Assert-True ($overview.totals.campaignSeriesCount -ge 1) 'Workspace overview did not include campaign series totals.'
    Assert-True ($hub.id -eq $SeriesId) "Selected-series hub returned $($hub.id), expected $SeriesId."
    Assert-True ($setup.series.id -eq $SeriesId) 'Setup workspace returned the wrong series.'
    Assert-True ($hub.studyKind -eq 'sample') "Hub did not mark series $SeriesId as sample."
    Assert-True ($hub.isSample -eq $true) "Hub did not expose isSample for series $SeriesId."
    Assert-True ($hub.sampleScenario -eq $SampleScenario) "Hub sample scenario was $($hub.sampleScenario), expected $SampleScenario."
    Assert-True ($hub.readOnlyReason -eq 'sample_study') "Hub read-only reason was $($hub.readOnlyReason), expected sample_study."
    Assert-True ($setup.series.isSample -eq $true) "Setup workspace did not expose sample series metadata."
    Assert-True ($operations.series.isSample -eq $true) "Operations workspace did not expose sample series metadata."
    Assert-True ($reports.series.isSample -eq $true) "Reports workspace did not expose sample series metadata."
    Assert-True ($waves.series.isSample -eq $true) "Waves workspace did not expose sample series metadata."

    return @{
        operations = $operations
        reports = $reports
        waves = $waves
    }
}

function Get-SeriesItem {
    param(
        [object[]]$Items,
        [string]$SeriesId,
        [string]$SurfaceName
    )

    $matches = @($Items | Where-Object { [string]$_.id -eq $SeriesId })
    Assert-True ($matches.Count -eq 1) "$SurfaceName did not expose required sample series $SeriesId."
    return $matches[0]
}

function Assert-ProductSampleStateCoverage {
    param(
        [hashtable]$Headers,
        [hashtable]$SeriesIds
    )

    $overview = Invoke-Json GET '/workspace-overview' $Headers
    $portfolio = Invoke-Json GET '/campaign-series' $Headers
    $overviewSamples = @($overview.studyCollections.sampleStudies)
    $portfolioItems = @($portfolio.items)

    Assert-True ($overviewSamples.Count -ge 4) "Workspace overview exposed $($overviewSamples.Count) sample studies; expected at least four starter sample states."

    $setupOverview = Get-SeriesItem $overviewSamples ([string]$SeriesIds.setup) 'Workspace overview'
    $collectionOverview = Get-SeriesItem $overviewSamples ([string]$SeriesIds.inCollection) 'Workspace overview'
    $completedOverview = Get-SeriesItem $overviewSamples ([string]$SeriesIds.completed) 'Workspace overview'
    $longitudinalOverview = Get-SeriesItem $overviewSamples ([string]$SeriesIds.longitudinal) 'Workspace overview'

    $setup = Get-SeriesItem $portfolioItems ([string]$SeriesIds.setup) 'Studies portfolio'
    $collection = Get-SeriesItem $portfolioItems ([string]$SeriesIds.inCollection) 'Studies portfolio'
    $completed = Get-SeriesItem $portfolioItems ([string]$SeriesIds.completed) 'Studies portfolio'
    $longitudinal = Get-SeriesItem $portfolioItems ([string]$SeriesIds.longitudinal) 'Studies portfolio'

    foreach ($item in @($setupOverview, $collectionOverview, $completedOverview, $longitudinalOverview, $setup, $collection, $completed, $longitudinal)) {
        Assert-True ($item.isSample -eq $true) "Starter sample $($item.id) was not exposed as sample."
        Assert-True ($item.readOnlyReason -eq 'sample_study') "Starter sample $($item.id) read-only reason was $($item.readOnlyReason), expected sample_study."
    }

    Assert-True (@('setup', 'blocked') -contains [string]$setup.sampleScenario) "Setup sample scenario was $($setup.sampleScenario)."
    Assert-True ($setup.campaignCount -eq 0) "Setup sample should have no campaigns so it appears as setup/blocked, got $($setup.campaignCount)."

    Assert-True ([string]$collection.sampleScenario -eq 'in_collection') "Collection sample scenario was $($collection.sampleScenario)."
    Assert-True ($collection.liveCampaignCount -ge 1) "Collection sample should have a live campaign."
    Assert-True ($collection.submittedResponseCount -eq 0) "Collection sample should show in-progress collection without submitted responses, got $($collection.submittedResponseCount)."

    Assert-True (@('mixed_lifecycle', 'completed') -contains [string]$completed.sampleScenario) "Results sample scenario was $($completed.sampleScenario)."
    Assert-True ($completed.submittedResponseCount -ge 5) "Results sample should expose submitted responses, got $($completed.submittedResponseCount)."

    Assert-True ([string]$longitudinal.sampleScenario -eq 'longitudinal') "Longitudinal sample scenario was $($longitudinal.sampleScenario)."
    Assert-True ($longitudinal.submittedResponseCount -ge 5) "Longitudinal sample should expose submitted wave responses, got $($longitudinal.submittedResponseCount)."
}

function Seed-Tenant {
    param(
        [object]$Tenant,
        [string]$UserId,
        [string]$Suffix,
        [hashtable]$EnvValues
    )

    $headers = New-Headers ([string]$Tenant.id) $UserId
    $session = Invoke-Json GET '/auth/session' $headers
    Assert-True ($session.tenantId -eq [string]$Tenant.id) "Dev auth returned tenant $($session.tenantId), expected $($Tenant.id)."

    $directorySeed = New-DirectoryHierarchy $headers $Tenant $Suffix

    $createdInstruments = @()
    foreach ($instrument in @($Tenant.instruments)) {
        $createdInstruments += ,(New-DemoInstrument $headers $Tenant $instrument $Suffix)
    }

    $primary = $createdInstruments[0]
    $secondary = $createdInstruments[1]
    $story = $Tenant.story

    $setupSeries = Invoke-Json POST '/campaign-series' $headers @{
        name = "$($story.setupSeriesName) $Suffix"
    }
    $setupSeriesSampleScenario = [string]$story.sampleScenarios.setupSeries
    Set-CampaignSeriesSampleMetadata $EnvValues ([string]$Tenant.id) ([string]$setupSeries.id) $setupSeriesSampleScenario
    [void](Invoke-ProductSurfaceChecks $headers $setupSeries.id $setupSeriesSampleScenario)

    $inCollectionSeries = Invoke-Json POST '/campaign-series' $headers @{
        name = "$($story.inCollectionSeriesName) $Suffix"
    }
    $inCollectionSeriesSampleScenario = [string]$story.sampleScenarios.inCollectionSeries
    Set-CampaignSeriesSampleMetadata $EnvValues ([string]$Tenant.id) ([string]$inCollectionSeries.id) $inCollectionSeriesSampleScenario
    $inCollectionCampaign = New-LaunchedCampaign $headers "$($story.inCollectionSeriesName) live sample $Suffix" $primary.template.templateVersionId $inCollectionSeries.id 'anonymous'
    $inCollectionProfile = Get-ResponseProfileBaseValues $Tenant 'partial'
    $inCollectionValues = New-ResponseValues @($primary.catalog.questions) $inCollectionProfile[0]
    $inCollectionResponse = Submit-OpenLinkResponse $inCollectionCampaign.openLink $inCollectionValues $false $null
    [void](Invoke-ProductSurfaceChecks $headers $inCollectionSeries.id $inCollectionSeriesSampleScenario)

    $mainSeries = Invoke-Json POST '/campaign-series' $headers @{
        name = "$($story.mainSeriesName) $Suffix"
    }
    $mainSeriesSampleScenario = [string]$story.sampleScenarios.mainSeries
    Set-CampaignSeriesSampleMetadata $EnvValues ([string]$Tenant.id) ([string]$mainSeries.id) $mainSeriesSampleScenario

    $draftCampaign = New-Campaign $headers "$(Get-CampaignStoryName $Tenant 'draft') $Suffix" $secondary.template.templateVersionId $mainSeries.id 'anonymous'
    $liveNoResponses = New-LaunchedCampaign $headers "$(Get-CampaignStoryName $Tenant 'liveNoResponses') $Suffix" $primary.template.templateVersionId $mainSeries.id 'anonymous'
    $partial = New-LaunchedCampaign $headers "$(Get-CampaignStoryName $Tenant 'partial') $Suffix" $primary.template.templateVersionId $mainSeries.id 'anonymous'
    $partialProfile = Get-ResponseProfileBaseValues $Tenant 'partial'
    $partialValues = New-ResponseValues @($primary.catalog.questions) $partialProfile[0]
    $partialResponse = Submit-OpenLinkResponse $partial.openLink $partialValues $false $null

    $completed = New-LaunchedCampaign $headers "$(Get-CampaignStoryName $Tenant 'completed') $Suffix" $primary.template.templateVersionId $mainSeries.id 'anonymous'
    foreach ($baseValue in (Get-ResponseProfileBaseValues $Tenant 'completed')) {
        $values = New-ResponseValues @($primary.catalog.questions) $baseValue
        [void](Submit-OpenLinkResponse $completed.openLink $values $true $null)
    }

    $mainChecks = Invoke-ProductSurfaceChecks $headers $mainSeries.id $mainSeriesSampleScenario
    Assert-True ($mainChecks.operations.summary.submittedResponseCount -ge 5) 'Operations workspace did not observe submitted proof responses.'
    Assert-True ($mainChecks.reports.summary.submittedResponseCount -ge 5) 'Reports workspace did not observe submitted proof responses.'

    $reportProof = Invoke-Json GET "/campaigns/$($completed.campaign.id)/report-proof" $headers
    Assert-True ($reportProof.scores.Count -gt 0) 'Completed campaign report proof did not include scores.'
    $reportExport = Invoke-Json POST "/campaigns/$($completed.campaign.id)/report-proof/exports" $headers @{}
    Assert-True ($reportExport.status -eq 'succeeded') 'Completed campaign report export did not succeed.'
    $responseExport = Invoke-Json POST "/campaign-series/$($mainSeries.id)/response-exports" $headers @{}
    Assert-True ($responseExport.status -eq 'succeeded') 'Campaign-series response export did not succeed.'

    $closed = Invoke-Json POST "/campaign-series/$($mainSeries.id)/campaigns/$($completed.campaign.id)/close" $headers @{
        reason = 'VAL08 proof collection complete'
    }
    Assert-True ($closed.status -eq 'closed') "Expected closed campaign status, got $($closed.status)."
    $closedEntryStatus = Get-HttpStatus -Url (Join-Url $ApiBaseUrl "/respondent/open-links/$($completed.openLink.token)")
    Assert-True ($closedEntryStatus -eq 404) "Expected closed public entry to return 404, got $closedEntryStatus."
    $closedReportProof = Invoke-Json GET "/campaigns/$($completed.campaign.id)/report-proof" $headers
    Assert-True ($closedReportProof.dataFinality -eq 'closed_wave') "Expected closed report proof finality closed_wave, got $($closedReportProof.dataFinality)."
    $closedReportExport = Invoke-Json POST "/campaigns/$($completed.campaign.id)/report-proof/exports" $headers @{}
    Assert-True ($closedReportExport.status -eq 'succeeded') 'Closed campaign report export did not succeed.'

    $waveSeries = Invoke-Json POST '/campaign-series' $headers @{
        name = "$($story.linkedWaveSeriesName) $Suffix"
    }
    $waveSeriesSampleScenario = [string]$story.sampleScenarios.linkedWaveSeries
    Set-CampaignSeriesSampleMetadata $EnvValues ([string]$Tenant.id) ([string]$waveSeries.id) $waveSeriesSampleScenario
    $wave1 = New-LaunchedCampaign $headers "$(Get-CampaignStoryName $Tenant 'wave1') $Suffix" $primary.template.templateVersionId $waveSeries.id 'anonymous_longitudinal'
    $wave2 = New-LaunchedCampaign $headers "$(Get-CampaignStoryName $Tenant 'wave2') $Suffix" $primary.template.templateVersionId $waveSeries.id 'anonymous_longitudinal'

    $baselineProfiles = Get-ResponseProfileBaseValues $Tenant 'waveBaseline'
    $comparisonProfiles = Get-ResponseProfileBaseValues $Tenant 'waveComparison'
    $linkedResponseCount = [Math]::Min($baselineProfiles.Count, $comparisonProfiles.Count)
    for ($i = 0; $i -lt $linkedResponseCount; $i++) {
        $participantCode = 'val08-' + [Guid]::NewGuid().ToString('N').Substring(0, 16)
        $baselineValues = New-ResponseValues @($primary.catalog.questions) $baselineProfiles[$i]
        $comparisonValues = New-ResponseValues @($primary.catalog.questions) $comparisonProfiles[$i]
        [void](Submit-OpenLinkResponse $wave1.openLink $baselineValues $true $participantCode)
        [void](Submit-OpenLinkResponse $wave2.openLink $comparisonValues $true $participantCode)
    }

    $waveChecks = Invoke-ProductSurfaceChecks $headers $waveSeries.id $waveSeriesSampleScenario
    Assert-True ($waveChecks.waves.summary.completeTrajectoryCount -ge 5) 'Waves workspace did not observe linked trajectories.'
    $twoWaveProof = Invoke-Json GET "/campaign-series/$($waveSeries.id)/two-wave-proof" $headers
    Assert-True ($twoWaveProof.completeTrajectoryCount -ge 5) 'Two-wave proof did not count complete trajectories.'
    $waveComparisonProof = Invoke-Json GET "/campaign-series/$($waveSeries.id)/wave-comparison-proof" $headers
    Assert-True ($waveComparisonProof.scores.Count -gt 0) 'Wave-comparison proof did not include score comparisons.'

    Assert-ProductSampleStateCoverage $headers @{
        setup = $setupSeries.id
        inCollection = $inCollectionSeries.id
        completed = $mainSeries.id
        longitudinal = $waveSeries.id
    }

    return @{
        tenant = $Tenant
        directory = $directorySeed
        instrumentCount = $createdInstruments.Count
        setupSeries = $setupSeries
        inCollectionSeries = $inCollectionSeries
        mainSeries = $mainSeries
        waveSeries = $waveSeries
        inCollectionCampaign = $inCollectionCampaign.campaign
        inCollectionSessionId = $inCollectionResponse.session.id
        draftCampaign = $draftCampaign
        liveNoResponsesCampaign = $liveNoResponses.campaign
        partialCampaign = $partial.campaign
        partialSessionId = $partialResponse.session.id
        completedCampaign = $completed.campaign
        completedReportExport = $reportExport
        responseExport = $responseExport
        closedReportExport = $closedReportExport
        wave1Campaign = $wave1.campaign
        wave2Campaign = $wave2.campaign
        waveComparison = $waveComparisonProof
    }
}

$catalog = Read-FixtureCatalog
Validate-FixtureCatalog $catalog

if (-not $RunTag) {
    $RunTag = 'val08-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
}
$RunTag = New-SafeCode $RunTag

if ($ValidateOnly) {
    Write-Host "Validation demo fixture catalog passed: $catalogFile"
    foreach ($tenant in @($catalog.tenants)) {
        Write-Host "$($tenant.slug): $(@($tenant.instruments).Count) instruments, $(@($tenant.directory.subjects).Count) directory subjects, $(@($tenant.directory.groups).Count) groups, $(@($tenant.proofStates).Count) proof states"
    }
    return
}

$envValues = Read-EnvFile -Path $envFile

if (-not $ApiBaseUrl) {
    $apiPort = Get-EnvValue $envValues 'API_HTTP_PORT' '5055'
    $ApiBaseUrl = "http://127.0.0.1:$apiPort"
}

if (-not $WebBaseUrl) {
    $webPort = Get-EnvValue $envValues 'WEB_HTTP_PORT' '5174'
    $WebBaseUrl = "http://127.0.0.1:$webPort"
}

$userId = Get-EnvValue $envValues 'PUBLIC_DEV_USER_ID' '22222222-2222-4222-8222-222222222222'

Write-Host "VAL08 validation demo seed: $RunTag"
Write-Host "API: $ApiBaseUrl"
Write-Host "Web: $WebBaseUrl"

$health = Wait-HttpOk -Url (Join-Url $ApiBaseUrl '/health')
$healthJson = $health.Content | ConvertFrom-Json
Assert-True ($healthJson.status -eq 'ok') "Unexpected health status: $($health.Content)"

$unauthenticatedStatus = Get-HttpStatus -Url (Join-Url $ApiBaseUrl '/auth/session')
Assert-True ($unauthenticatedStatus -eq 401) "Expected unauthenticated /auth/session to return 401, got $unauthenticatedStatus."

Invoke-TenantBootstrap $envValues -Skip:$SkipTenantBootstrap
Assert-ValidationTenantsEmpty $envValues $catalog -AllowDuplicateSeed:$AllowDuplicateSeed

$seedResults = @()
foreach ($tenant in @($catalog.tenants)) {
    Write-Host "Seeding tenant $($tenant.slug)"
    $seedResults += ,(Seed-Tenant $tenant $userId $RunTag $envValues)
}

if (-not $SkipWebCheck) {
    $web = Wait-HttpOk -Url (Join-Url $WebBaseUrl '/')
    Assert-True ($web.Content -match 'Tenant setup workspace|Workspace') 'Frontend did not return the expected shell.'
}

Write-Host ''
Write-Host 'Owner inspection routes'
foreach ($result in $seedResults) {
    Write-Host ''
    Write-Host "$($result.tenant.slug)"
    Write-Host (Join-Url $WebBaseUrl '/app/directory')
    Write-Host (Join-Url $WebBaseUrl "/app/campaign-series/$($result.mainSeries.id)")
    Write-Host (Join-Url $WebBaseUrl "/app/campaign-series/$($result.mainSeries.id)/setup")
    Write-Host (Join-Url $WebBaseUrl "/app/campaign-series/$($result.mainSeries.id)/operations")
    Write-Host (Join-Url $WebBaseUrl "/app/campaign-series/$($result.mainSeries.id)/reports")
    Write-Host (Join-Url $WebBaseUrl "/app/campaign-series/$($result.mainSeries.id)/waves")
    Write-Host (Join-Url $WebBaseUrl "/app/campaign-series/$($result.waveSeries.id)")
    Write-Host (Join-Url $WebBaseUrl "/app/campaign-series/$($result.waveSeries.id)/waves")
    Write-Host "Directory subjects: $($result.directory.subjectCount)"
    Write-Host "Directory groups: $($result.directory.groupCount)"
    Write-Host "Directory manager links: $($result.directory.managerRelationshipCount)"
    Write-Host "Private instruments: $($result.instrumentCount)"
    Write-Host "Draft campaign: $($result.draftCampaign.id)"
    Write-Host "Live empty campaign: $($result.liveNoResponsesCampaign.id)"
    Write-Host "Partial campaign: $($result.partialCampaign.id)"
    Write-Host "Completed campaign: $($result.completedCampaign.id)"
    Write-Host "Wave 1 campaign: $($result.wave1Campaign.id)"
    Write-Host "Wave 2 campaign: $($result.wave2Campaign.id)"
    Write-Host "Report export artifact: $($result.completedReportExport.id)"
    Write-Host "Response export artifact: $($result.responseExport.id)"
    Write-Host "Closed report export artifact: $($result.closedReportExport.id)"
}

Write-Host ''
Write-Host 'VAL08 validation demo seed passed.'
