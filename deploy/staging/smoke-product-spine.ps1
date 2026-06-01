param(
    [string]$ApiBaseUrl,
    [string]$WebBaseUrl,
    [string]$TenantId,
    [string]$UserId,
    [string]$ParticipantCodePrefix,
    [switch]$SkipWebCheck,
    [string]$SessionCookie,
    [string]$SessionCookiePath,
    [switch]$RequireAuthenticatedSession,
    [string]$EvidencePath = ''
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Net.Http

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$envFile = Join-Path $repoRoot 'deploy\staging\.env'

function Read-EnvFile {
    param([string]$Path)

    $values = @{}
    if (-not (Test-Path $Path)) {
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

    return $Body | ConvertTo-Json -Depth 40 -Compress
}

function Write-ProductSpineEvidence {
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
    Write-Host "Product-spine evidence written to $EvidencePath"
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

function Invoke-HttpClientRequest {
    param(
        [ValidateSet('GET', 'POST', 'PUT', 'PATCH')]
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [int]$TimeoutSec = 30,
        [switch]$AllowHttpError
    )

    $handler = New-Object System.Net.Http.HttpClientHandler
    $handler.UseCookies = $false
    $client = New-Object System.Net.Http.HttpClient($handler)
    $client.Timeout = [TimeSpan]::FromSeconds($TimeoutSec)
    $request = New-Object System.Net.Http.HttpRequestMessage(
        (New-Object System.Net.Http.HttpMethod($Method)),
        $Url)

    try {
        foreach ($key in $Headers.Keys) {
            if ($null -ne $Headers[$key]) {
                [void]$request.Headers.TryAddWithoutValidation([string]$key, [string]$Headers[$key])
            }
        }

        if ($null -ne $Body) {
            $request.Content = New-Object System.Net.Http.StringContent(
                (ConvertTo-JsonPayload $Body),
                [System.Text.Encoding]::UTF8,
                'application/json')
        }

        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        $bytes = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        $content = [System.Text.Encoding]::UTF8.GetString($bytes)
        $responseHeaders = @{}
        foreach ($header in $response.Headers) {
            $responseHeaders[$header.Key] = ($header.Value -join ',')
        }

        foreach ($header in $response.Content.Headers) {
            $responseHeaders[$header.Key] = ($header.Value -join ',')
        }

        if (-not $AllowHttpError -and -not $response.IsSuccessStatusCode) {
            throw "$Method $Url failed. HTTP $([int]$response.StatusCode). Body: $content"
        }

        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            Content = $content
            Headers = $responseHeaders
            RawContentBytes = $bytes
        }
    } finally {
        $request.Dispose()
        $client.Dispose()
        $handler.Dispose()
    }
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

    if ($Headers.ContainsKey('Cookie')) {
        $response = Invoke-HttpClientRequest `
            -Method $Method `
            -Url $url `
            -Headers $Headers `
            -Body $Body `
            -TimeoutSec $TimeoutSec
        if ([string]::IsNullOrWhiteSpace($response.Content)) {
            return $null
        }

        return $response.Content | ConvertFrom-Json
    }

    try {
        return Invoke-RestMethod @parameters
    } catch {
        throw (Get-HttpErrorMessage $Method $url $_)
    }
}

function Resolve-SessionCookie {
    param(
        [string]$DirectSessionCookie,
        [string]$SessionCookiePath,
        [switch]$RequireAuthenticatedSession
    )

    if (![string]::IsNullOrWhiteSpace($DirectSessionCookie) -and
        ![string]::IsNullOrWhiteSpace($SessionCookiePath)) {
        throw 'SessionCookie and SessionCookiePath cannot both be supplied.'
    }

    $resolved = $DirectSessionCookie
    if (![string]::IsNullOrWhiteSpace($SessionCookiePath)) {
        if (!(Test-Path -LiteralPath $SessionCookiePath -PathType Leaf)) {
            throw 'SessionCookiePath file was not found. Do not commit cookie files; keep them local and ignored.'
        }

        $resolved = Get-Content -Raw -LiteralPath $SessionCookiePath
    } elseif ([string]::IsNullOrWhiteSpace($resolved) -and
        ![string]::IsNullOrWhiteSpace($env:STAGING_SESSION_COOKIE)) {
        $resolved = $env:STAGING_SESSION_COOKIE
    }

    if ([string]::IsNullOrWhiteSpace($resolved)) {
        if ($RequireAuthenticatedSession) {
            throw 'Authenticated product-spine session proof required but no SessionCookie, SessionCookiePath, or STAGING_SESSION_COOKIE was supplied.'
        }

        return $null
    }

    Write-Host 'Authenticated product-spine session cookie source resolved.'
    return $resolved.Trim()
}

function Merge-CookieHeader {
    param(
        [string]$CookieHeader,
        [object]$SetCookieHeader
    )

    $cookies = [ordered]@{}
    foreach ($part in ($CookieHeader -split ';')) {
        $trimmed = $part.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed)) {
            continue
        }

        $pair = $trimmed.Split('=', 2)
        if ($pair.Length -eq 2 -and ![string]::IsNullOrWhiteSpace($pair[0])) {
            $cookies[$pair[0].Trim()] = $pair[1]
        }
    }

    foreach ($header in @($SetCookieHeader)) {
        if ([string]::IsNullOrWhiteSpace([string]$header)) {
            continue
        }

        $cookiePair = ([string]$header).Split(';', 2)[0].Trim()
        $pair = $cookiePair.Split('=', 2)
        if ($pair.Length -eq 2 -and ![string]::IsNullOrWhiteSpace($pair[0])) {
            $cookies[$pair[0].Trim()] = $pair[1]
        }
    }

    return (($cookies.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join '; ')
}

function Resolve-RemoteAuthenticatedRequestState {
    param([string]$CookieHeader)

    $csrfUrl = Join-Url $ApiBaseUrl '/auth/csrf'
    $csrfHeaders = @{
        Origin = $WebBaseUrl
        'X-Tenant-Id' = $TenantId
        Cookie = $CookieHeader
    }

    $csrfResponse = Invoke-HttpClientRequest `
        -Method GET `
        -Url $csrfUrl `
        -Headers $csrfHeaders `
        -TimeoutSec 30

    $csrfJson = $csrfResponse.Content | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace($csrfJson.csrfToken)) {
        throw 'Authenticated product-spine session proof failed because /auth/csrf did not return a csrfToken.'
    }

    return [pscustomobject]@{
        CookieHeader = Merge-CookieHeader -CookieHeader $CookieHeader -SetCookieHeader $csrfResponse.Headers['Set-Cookie']
        CsrfToken = $csrfJson.csrfToken
    }
}

function Get-SessionTenantId {
    param([object]$Session)

    if ($Session.PSObject.Properties.Name -contains 'tenantId') {
        return $Session.tenantId
    }

    if ($Session.PSObject.Properties.Name -contains 'tenant' -and $Session.tenant) {
        return $Session.tenant.id
    }

    return $null
}

function Get-SessionPermissions {
    param([object]$Session)

    if ($Session.PSObject.Properties.Name -contains 'permissions' -and $Session.permissions) {
        return @($Session.permissions)
    }

    return @()
}

function Invoke-AuthenticatedProductSpineSession {
    param([bool]$RemoteCookieAuthenticated)

    try {
        return Invoke-Json GET '/auth/session' $headers
    } catch {
        $message = [string]$_
        if ($message -match '401|403') {
            if ($RemoteCookieAuthenticated) {
                throw 'Authenticated product-spine smoke failed because the supplied browser session cookie was not accepted by /auth/session.'
            }

            throw 'Authenticated product-spine smoke failed because development authentication is disabled. Recreate local staging with Authentication__Dev__Enabled=true and PUBLIC_DEV_AUTH_ENABLED=true before running this smoke.'
        }

        throw
    }
}

function Invoke-AuthenticatedDevSession {
    param([bool]$RemoteCookieAuthenticated)

    return Invoke-AuthenticatedProductSpineSession -RemoteCookieAuthenticated:$RemoteCookieAuthenticated
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

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-TextContains {
    param(
        [string]$Text,
        [string]$Expected,
        [string]$Message
    )

    Assert-True ($Text -match [regex]::Escape($Expected)) $Message
}

function Get-ScoreByDimension {
    param(
        [object]$Scores,
        [string]$DimensionCode,
        [string]$Context
    )

    $score = @($Scores | Where-Object { $_.dimensionCode -eq $DimensionCode })[0]
    Assert-True ($null -ne $score) "$Context did not include score dimension '$DimensionCode'."

    return $score
}

function Convert-CodebookJson {
    param([object]$CodebookJson)

    if ($CodebookJson -is [string]) {
        return $CodebookJson | ConvertFrom-Json
    }

    return $CodebookJson
}

function Assert-CodebookColumn {
    param(
        [object]$Codebook,
        [string]$ColumnName,
        [string]$Source,
        [string]$DisclosureTreatment = $null,
        [string]$DimensionCode = $null,
        [string]$MetadataKind = $null
    )

    $column = @($Codebook.columns | Where-Object { $_.name -eq $ColumnName })[0]
    Assert-True ($null -ne $column) "Codebook did not include column '$ColumnName'."
    Assert-True ($column.source -eq $Source) "Codebook column '$ColumnName' source was '$($column.source)', expected '$Source'."

    if ($DisclosureTreatment) {
        Assert-True ($column.disclosureTreatment -eq $DisclosureTreatment) "Codebook column '$ColumnName' disclosure treatment was '$($column.disclosureTreatment)', expected '$DisclosureTreatment'."
    }

    if ($DimensionCode) {
        Assert-True ($column.dimensionCode -eq $DimensionCode) "Codebook column '$ColumnName' dimension code was '$($column.dimensionCode)', expected '$DimensionCode'."
    }

    if ($MetadataKind) {
        Assert-True ($column.metadataKind -eq $MetadataKind) "Codebook column '$ColumnName' metadata kind was '$($column.metadataKind)', expected '$MetadataKind'."
    }
}

function Assert-ScoreResponseMetadata {
    param(
        [object]$ScoreResponse,
        [string]$Context
    )

    $score = Get-ScoreByDimension $ScoreResponse.scores 'total' $Context
    Assert-True ([int]$score.nValid -eq 3) "$Context score nValid was '$($score.nValid)', expected 3."
    Assert-True ([int]$score.nExpected -eq 3) "$Context score nExpected was '$($score.nExpected)', expected 3."
    Assert-True ($score.missingPolicyStatus -eq 'ok') "$Context score missingPolicyStatus was '$($score.missingPolicyStatus)', expected ok."
}

function Assert-ReportScoreMetadata {
    param(
        [object]$ReportProof,
        [string]$Context
    )

    $score = Get-ScoreByDimension $ReportProof.scores 'total' $Context
    Assert-True ($score.disclosure -eq 'visible') "$Context score disclosure was '$($score.disclosure)', expected visible."
    Assert-True ($score.displayLabel -eq 'Synthetic wellbeing index') "$Context displayLabel was '$($score.displayLabel)', expected Synthetic wellbeing index."
    Assert-True ($score.calculation -eq 'mean_1_5') "$Context calculation was '$($score.calculation)', expected mean_1_5."
    Assert-True ($score.calculationLabel -eq 'Mean score') "$Context calculationLabel was '$($score.calculationLabel)', expected Mean score."
    Assert-True ([decimal]$score.scoreRangeMin -eq 1) "$Context scoreRangeMin was '$($score.scoreRangeMin)', expected 1."
    Assert-True ([decimal]$score.scoreRangeMax -eq 5) "$Context scoreRangeMax was '$($score.scoreRangeMax)', expected 5."
    Assert-True ([int]$score.scoreCount -eq 5) "$Context scoreCount was '$($score.scoreCount)', expected 5."
    Assert-True ([int]$score.nValidTotal -eq 15) "$Context nValidTotal was '$($score.nValidTotal)', expected 15."
    Assert-True ([int]$score.nExpectedTotal -eq 15) "$Context nExpectedTotal was '$($score.nExpectedTotal)', expected 15."
    Assert-True ($score.missingPolicyStatusSummary -eq 'ok') "$Context missingPolicyStatusSummary was '$($score.missingPolicyStatusSummary)', expected ok."
}

function Assert-WaveComparisonScoreMetadata {
    param([object]$WaveComparisonProof)

    $score = Get-ScoreByDimension $WaveComparisonProof.scores 'total' 'Wave-comparison proof'
    Assert-True ($score.disclosure -eq 'visible') "Wave-comparison score disclosure was '$($score.disclosure)', expected visible."
    Assert-True ($score.displayLabel -eq 'Synthetic wellbeing index') "Wave-comparison displayLabel was '$($score.displayLabel)', expected Synthetic wellbeing index."
    Assert-True ($score.baselineCalculation -eq 'mean_1_5') "Wave-comparison baselineCalculation was '$($score.baselineCalculation)', expected mean_1_5."
    Assert-True ($score.comparisonCalculation -eq 'mean_1_5') "Wave-comparison comparisonCalculation was '$($score.comparisonCalculation)', expected mean_1_5."
    Assert-True ($score.baselineCalculationLabel -eq 'Mean score') "Wave-comparison baselineCalculationLabel was '$($score.baselineCalculationLabel)', expected Mean score."
    Assert-True ($score.comparisonCalculationLabel -eq 'Mean score') "Wave-comparison comparisonCalculationLabel was '$($score.comparisonCalculationLabel)', expected Mean score."
    Assert-True ([decimal]$score.baselineScoreRangeMin -eq 1) "Wave-comparison baselineScoreRangeMin was '$($score.baselineScoreRangeMin)', expected 1."
    Assert-True ([decimal]$score.baselineScoreRangeMax -eq 5) "Wave-comparison baselineScoreRangeMax was '$($score.baselineScoreRangeMax)', expected 5."
    Assert-True ([decimal]$score.comparisonScoreRangeMin -eq 1) "Wave-comparison comparisonScoreRangeMin was '$($score.comparisonScoreRangeMin)', expected 1."
    Assert-True ([decimal]$score.comparisonScoreRangeMax -eq 5) "Wave-comparison comparisonScoreRangeMax was '$($score.comparisonScoreRangeMax)', expected 5."
    Assert-True ([int]$score.baselineScoreCount -eq 5) "Wave-comparison baselineScoreCount was '$($score.baselineScoreCount)', expected 5."
    Assert-True ([int]$score.comparisonScoreCount -eq 5) "Wave-comparison comparisonScoreCount was '$($score.comparisonScoreCount)', expected 5."
    Assert-True ([int]$score.baselineNValidTotal -eq 15) "Wave-comparison baselineNValidTotal was '$($score.baselineNValidTotal)', expected 15."
    Assert-True ([int]$score.baselineNExpectedTotal -eq 15) "Wave-comparison baselineNExpectedTotal was '$($score.baselineNExpectedTotal)', expected 15."
    Assert-True ($score.baselineMissingPolicyStatusSummary -eq 'ok') "Wave-comparison baselineMissingPolicyStatusSummary was '$($score.baselineMissingPolicyStatusSummary)', expected ok."
    Assert-True ([int]$score.comparisonNValidTotal -eq 15) "Wave-comparison comparisonNValidTotal was '$($score.comparisonNValidTotal)', expected 15."
    Assert-True ([int]$score.comparisonNExpectedTotal -eq 15) "Wave-comparison comparisonNExpectedTotal was '$($score.comparisonNExpectedTotal)', expected 15."
    Assert-True ($score.comparisonMissingPolicyStatusSummary -eq 'ok') "Wave-comparison comparisonMissingPolicyStatusSummary was '$($score.comparisonMissingPolicyStatusSummary)', expected ok."
}

function Assert-ReportExportScoreMetadata {
    param(
        [object]$Artifact,
        [string]$Context
    )

    Assert-TextContains $Artifact.csvContent 'dimension_code,score_display_label,score_calculation,score_calculation_label,score_range_min,score_range_max,disclosure,submitted_response_count,score_count,n_valid_total,n_expected_total,missing_policy_status_summary,mean' "$Context CSV did not include aggregate score definition metadata columns."
    Assert-TextContains $Artifact.csvContent 'total,Synthetic wellbeing index,mean_1_5,Mean score,1,5,visible,5,5,15,15,ok' "$Context CSV did not include visible aggregate score metadata values."

    $codebook = Convert-CodebookJson $Artifact.codebookJson
    Assert-True ($codebook.suppressionBasis -eq 'same_suppression_as_report_proof') "$Context codebook did not document report-proof suppression basis."
    Assert-CodebookColumn $codebook 'score_display_label' 'score_output_metadata' 'score_definition_metadata'
    Assert-CodebookColumn $codebook 'score_calculation' 'score_output_metadata' 'score_definition_metadata'
    Assert-CodebookColumn $codebook 'score_calculation_label' 'score_output_metadata' 'score_definition_metadata'
    Assert-CodebookColumn $codebook 'score_range_min' 'score_output_metadata' 'score_definition_metadata'
    Assert-CodebookColumn $codebook 'score_range_max' 'score_output_metadata' 'score_definition_metadata'
    Assert-CodebookColumn $codebook 'n_valid_total' 'score_output_metadata' 'suppressed_when_report_proof_suppressed'
    Assert-CodebookColumn $codebook 'n_expected_total' 'score_output_metadata' 'suppressed_when_report_proof_suppressed'
    Assert-CodebookColumn $codebook 'missing_policy_status_summary' 'score_output_metadata' 'suppressed_when_report_proof_suppressed'
}

function Assert-ResponseExportScoreMetadata {
    param(
        [object]$Artifact,
        [string]$Context
    )

    Assert-TextContains $Artifact.csvContent 'score_total_n_valid,score_total_n_expected,score_total_missing_policy_status' "$Context CSV did not include response score metadata columns."
    Assert-TextContains $Artifact.csvContent ',3,3,ok' "$Context CSV did not include response score metadata values."

    $codebook = Convert-CodebookJson $Artifact.codebookJson
    Assert-True ([int]$codebook.scoreMetadataDimensionCount -eq 1) "$Context codebook scoreMetadataDimensionCount was '$($codebook.scoreMetadataDimensionCount)', expected 1."
    Assert-CodebookColumn $codebook 'score_total_n_valid' 'score_output_metadata' 'per_submitted_response_score_metadata' 'total' 'n_valid'
    Assert-CodebookColumn $codebook 'score_total_n_expected' 'score_output_metadata' 'per_submitted_response_score_metadata' 'total' 'n_expected'
    Assert-CodebookColumn $codebook 'score_total_missing_policy_status' 'score_output_metadata' 'per_submitted_response_score_metadata' 'total' 'missing_policy_status'
}

function Assert-PostWithdrawalFreshExportArtifactSafety {
    param(
        [object]$Artifact,
        [string[]]$ForbiddenMarkers,
        [string]$Context
    )

    Assert-True ($Artifact.status -eq 'succeeded') "$Context did not succeed."
    Assert-True ($Artifact.canDownload -eq $true) "$Context was not downloadable."
    Assert-True ($null -eq $Artifact.deletedAt) "$Context was unexpectedly deleted."

    $payload = ConvertTo-JsonPayload $Artifact
    foreach ($marker in $ForbiddenMarkers) {
        if (-not [string]::IsNullOrWhiteSpace($marker)) {
            Assert-True (-not $payload.Contains($marker)) "$Context leaked forbidden marker '$marker'."
        }
    }

    Assert-True (-not ($payload -match 'wdr_|rawToken|storageKey|ConnectionStrings|Password|Secret')) "$Context leaked sensitive artifact metadata."
}

function Assert-PostWithdrawalReportPdfArtifactSafety {
    param(
        [object]$Artifact,
        [string]$ExpectedId,
        [string[]]$ForbiddenMarkers,
        [string]$Context
    )

    Assert-True ($Artifact.id -eq $ExpectedId) "$Context fetch returned artifact '$($Artifact.id)', expected '$ExpectedId'."
    Assert-True ($Artifact.artifactType -eq 'campaign_series_report_pdf') "$Context artifactType was '$($Artifact.artifactType)', expected campaign_series_report_pdf."
    Assert-True ($Artifact.format -eq 'pdf') "$Context format was '$($Artifact.format)', expected pdf."
    Assert-True (@('succeeded', 'failed') -contains $Artifact.status) "$Context status was '$($Artifact.status)', expected succeeded or failed."

    $payload = ConvertTo-JsonPayload $Artifact
    foreach ($marker in $ForbiddenMarkers) {
        if (-not [string]::IsNullOrWhiteSpace($marker)) {
            Assert-True (-not $payload.Contains($marker)) "$Context leaked forbidden marker '$marker'."
        }
    }

    Assert-True (-not ($payload -match 'wdr_|rawToken|storageKey|ConnectionStrings|Password|Secret')) "$Context leaked sensitive artifact metadata."
}

function Assert-WithdrawalTokenIssueAndConsume {
    param([object]$SubmittedResponse)

    $expiresAt = [DateTimeOffset]::UtcNow.AddHours(2).ToString('o')
    $issue = Invoke-Json POST '/withdrawal-requests/tokens' $headers @{
        responseSessionId = $SubmittedResponse.session.id
        requestedAction = 'anonymize'
        expiresAt = $expiresAt
        reasonCode = 'product_spine_smoke'
    }

    Assert-True ($issue.responseSessionId -eq $SubmittedResponse.session.id) "Withdrawal token issue returned responseSessionId '$($issue.responseSessionId)', expected '$($SubmittedResponse.session.id)'."
    Assert-True ($issue.requestedAction -eq 'anonymize') "Withdrawal token issue returned requestedAction '$($issue.requestedAction)', expected anonymize."
    Assert-True ($issue.rawToken -like 'wdr_*') 'Withdrawal token issue did not return a one-shot withdrawal token.'
    Assert-True ([DateTimeOffset]::Parse($issue.expiresAt) -gt [DateTimeOffset]::UtcNow) 'Withdrawal token issue returned an expired token.'

    $request = Invoke-Json POST '/withdrawal-requests/anonymous' @{} @{
        token = $issue.rawToken
        requestedAction = 'anonymize'
        reasonCode = 'product_spine_smoke'
    }

    Assert-True ($request.targetKind -eq 'response_session') "Withdrawal token consume targetKind was '$($request.targetKind)', expected response_session."
    Assert-True ($request.targetId -eq $SubmittedResponse.session.id) "Withdrawal token consume targetId was '$($request.targetId)', expected '$($SubmittedResponse.session.id)'."
    Assert-True ($request.requestedAction -eq 'anonymize') "Withdrawal token consume requestedAction was '$($request.requestedAction)', expected anonymize."
    Assert-True ($request.status -eq 'requested') "Withdrawal token consume status was '$($request.status)', expected requested."

    $responseBody = ConvertTo-JsonPayload $request
    Assert-True (-not $responseBody.Contains($issue.rawToken)) 'Withdrawal token consume response echoed the raw token.'
    Assert-True (-not ($responseBody -match 'wdr_')) 'Withdrawal token consume response echoed a withdrawal token marker.'

    return @{
        issue = $issue
        request = $request
    }
}

function Assert-WithdrawalRequestApproveAndExecute {
    param([object]$Withdrawal)

    $approved = Invoke-Json POST "/withdrawal-requests/$($Withdrawal.request.requestId)/approve" $headers @{
        reasonCode = 'product_spine_smoke'
    }
    Assert-True ($approved.requestId -eq $Withdrawal.request.requestId) "Withdrawal approve returned requestId '$($approved.requestId)', expected '$($Withdrawal.request.requestId)'."
    Assert-True ($approved.status -eq 'planned') "Withdrawal approve status was '$($approved.status)', expected planned."

    $executed = Invoke-Json POST "/withdrawal-requests/$($Withdrawal.request.requestId)/execute" $headers @{}
    Assert-True ($executed.withdrawalEventId -eq $Withdrawal.request.requestId) "Withdrawal execution returned event id '$($executed.withdrawalEventId)', expected '$($Withdrawal.request.requestId)'."
    Assert-True ($executed.status -eq 'completed') 'Withdrawal execution did not complete.'
    Assert-True ($null -ne $executed.processedAt) 'Withdrawal execution did not return processedAt.'
    Assert-True ($executed.dryRun.withdrawalEventId -eq $Withdrawal.request.requestId) 'Withdrawal execution dry-run referenced the wrong event.'
    Assert-True ([int]$executed.dryRun.responseSessionCount -ge 0) 'Withdrawal execution dry-run returned an invalid response-session count.'
    Assert-True ([int]$executed.dryRun.answerCount -ge 0) 'Withdrawal execution dry-run returned an invalid answer count.'
    Assert-True ([int]$executed.dryRun.scoreCount -ge 0) 'Withdrawal execution dry-run returned an invalid score count.'

    $responseBody = ConvertTo-JsonPayload $executed
    Assert-True (-not ($responseBody -match 'rawToken|rawAnswer|subject@example.com|identified-ip-hash|identified-user-agent-hash')) 'Withdrawal execution response leaked sensitive withdrawal data.'

    return @{
        approved = $approved
        executed = $executed
    }
}

function Assert-WithdrawalRequestReviewVisibility {
    param(
        [object]$Withdrawal,
        [string]$ExpectedStatus
    )

    $requests = Invoke-Json GET '/withdrawal-requests' $headers
    $listed = @($requests | Where-Object { $_.requestId -eq $Withdrawal.request.requestId })[0]
    Assert-True ($null -ne $listed) "Withdrawal review list did not include request '$($Withdrawal.request.requestId)'."
    Assert-True ($listed.targetKind -eq 'response_session') "Withdrawal review list targetKind was '$($listed.targetKind)', expected response_session."
    Assert-True ($listed.targetId -eq $Withdrawal.request.targetId) "Withdrawal review list targetId was '$($listed.targetId)', expected '$($Withdrawal.request.targetId)'."
    Assert-True ($listed.requestedAction -eq 'anonymize') "Withdrawal review list requestedAction was '$($listed.requestedAction)', expected anonymize."
    Assert-True ($listed.status -eq $ExpectedStatus) "Withdrawal review list status was '$($listed.status)', expected '$ExpectedStatus'."
    Assert-True ($null -ne $listed.requestedAt) 'Withdrawal review list did not include requestedAt.'
    Assert-True ([int]$listed.responseSessionCount -ge 0) 'Withdrawal review list returned an invalid responseSessionCount.'
    Assert-True ([int]$listed.answerCount -ge 0) 'Withdrawal review list returned an invalid answerCount.'
    Assert-True ([int]$listed.scoreRunCount -ge 0) 'Withdrawal review list returned an invalid scoreRunCount.'
    Assert-True ([int]$listed.scoreCount -ge 0) 'Withdrawal review list returned an invalid scoreCount.'

    $listBody = ConvertTo-JsonPayload $listed
    Assert-True (-not ($listBody -match 'rawToken|rawAnswer|subject@example.com|identified-ip-hash|identified-user-agent-hash|recipient|provider|participantCode|salt|storageKey|wdr_|ConnectionStrings|Password|Secret')) 'Withdrawal review list response leaked sensitive data.'

    $detail = Invoke-Json GET "/withdrawal-requests/$($Withdrawal.request.requestId)" $headers
    Assert-True ($detail.requestId -eq $Withdrawal.request.requestId) "Withdrawal review detail returned requestId '$($detail.requestId)', expected '$($Withdrawal.request.requestId)'."
    Assert-True ($detail.targetKind -eq 'response_session') "Withdrawal review detail targetKind was '$($detail.targetKind)', expected response_session."
    Assert-True ($detail.targetId -eq $Withdrawal.request.targetId) "Withdrawal review detail targetId was '$($detail.targetId)', expected '$($Withdrawal.request.targetId)'."
    Assert-True ($detail.requestedAction -eq 'anonymize') "Withdrawal review detail requestedAction was '$($detail.requestedAction)', expected anonymize."
    Assert-True ($detail.status -eq $ExpectedStatus) "Withdrawal review detail status was '$($detail.status)', expected '$ExpectedStatus'."
    Assert-True ($null -ne $detail.requestedAt) 'Withdrawal review detail did not include requestedAt.'
    if ($ExpectedStatus -eq 'completed') {
        Assert-True ($null -ne $detail.processedAt) 'Completed withdrawal review detail did not include processedAt.'
    }
    Assert-True ([int]$detail.responseSessionCount -ge 0) 'Withdrawal review detail returned an invalid responseSessionCount.'
    Assert-True ([int]$detail.answerCount -ge 0) 'Withdrawal review detail returned an invalid answerCount.'
    Assert-True ([int]$detail.scoreRunCount -ge 0) 'Withdrawal review detail returned an invalid scoreRunCount.'
    Assert-True ([int]$detail.scoreCount -ge 0) 'Withdrawal review detail returned an invalid scoreCount.'

    $detailBody = ConvertTo-JsonPayload $detail
    Assert-True (-not ($detailBody -match 'rawToken|rawAnswer|subject@example.com|identified-ip-hash|identified-user-agent-hash|recipient|provider|participantCode|salt|storageKey|wdr_|ConnectionStrings|Password|Secret')) 'Withdrawal review detail response leaked sensitive data.'

    return $detail
}

function Wait-OperationalNotificationForWithdrawalRequest {
    param(
        [string]$WithdrawalRequestId,
        [int]$Attempts = 30
    )

    for ($i = 0; $i -lt $Attempts; $i++) {
        $notifications = Invoke-Json GET '/operational-notifications?limit=50' $headers
        $match = @($notifications.notifications | Where-Object {
            $_.sourceAggregateId -eq $WithdrawalRequestId -and
            $_.notificationType -eq 'withdrawal_request_terminal' -and
            $_.sourceEventType -eq 'WithdrawalRequestTerminal'
        })
        if ($match.Count -gt 0) {
            return $match[0]
        }

        Start-Sleep -Seconds 2
    }

    throw "Timed out waiting for operational notification for withdrawal request $WithdrawalRequestId."
}

function Assert-WithdrawalTerminalNotification {
    param(
        [object]$Withdrawal,
        [object]$Execution
    )

    $notification = Wait-OperationalNotificationForWithdrawalRequest -WithdrawalRequestId $Withdrawal.request.requestId
    Assert-True ($notification.sourceAggregateId -eq $Withdrawal.request.requestId) "Withdrawal terminal notification referenced '$($notification.sourceAggregateId)', expected '$($Withdrawal.request.requestId)'."
    Assert-True ($notification.notificationType -eq 'withdrawal_request_terminal') "Withdrawal terminal notification type was '$($notification.notificationType)', expected withdrawal_request_terminal."
    Assert-True ($notification.sourceEventType -eq 'WithdrawalRequestTerminal') "Withdrawal terminal notification source event was '$($notification.sourceEventType)', expected WithdrawalRequestTerminal."
    Assert-True ($notification.severity -eq 'info') "Withdrawal terminal notification severity was '$($notification.severity)', expected info."
    Assert-True ($notification.status -eq 'unread') "Withdrawal terminal notification read status was '$($notification.status)', expected unread."
    Assert-True ($notification.sourceStatus -eq $Execution.executed.status) "Withdrawal terminal notification sourceStatus was '$($notification.sourceStatus)', expected '$($Execution.executed.status)'."

    $responseBody = ConvertTo-JsonPayload $notification
    Assert-True (-not ($responseBody -match 'rawToken|rawAnswer|subject@example.com|identified-ip-hash|identified-user-agent-hash|recipient|provider|participantCode|salt|storageKey|wdr_')) 'Withdrawal terminal notification leaked sensitive data.'

    return $notification
}

function Assert-OperationalNotificationSummaryAndMarkRead {
    param(
        [object]$Notification
    )

    $summaryBefore = Invoke-Json GET '/operational-notifications/summary' $headers
    $unreadBefore = [int]$summaryBefore.unreadCount
    $infoBefore = [int]$summaryBefore.infoUnreadCount
    $warningBefore = [int]$summaryBefore.warningUnreadCount
    Assert-True ($unreadBefore -ge 1) 'Operational notification summary did not report unread notifications before mark-read.'
    if ($Notification.severity -eq 'info') {
        Assert-True ($infoBefore -ge 1) 'Operational notification summary did not report an info unread notification before mark-read.'
    }
    if ($Notification.severity -eq 'warning') {
        Assert-True ($warningBefore -ge 1) 'Operational notification summary did not report a warning unread notification before mark-read.'
    }

    $summaryBeforeBody = ConvertTo-JsonPayload $summaryBefore
    Assert-True (-not ($summaryBeforeBody -match 'rawToken|rawAnswer|subject@example.com|identified-ip-hash|identified-user-agent-hash|recipient|provider|participantCode|salt|storageKey|wdr_|Smtp|Password|ConnectionStrings')) 'Operational notification summary response leaked sensitive data.'

    $marked = Invoke-Json POST "/operational-notifications/$($Notification.id)/mark-read" $headers @{}
    Assert-True ($marked.id -eq $Notification.id) "Operational notification mark-read returned id '$($marked.id)', expected '$($Notification.id)'."
    Assert-True ($marked.status -eq 'read') "Operational notification mark-read status was '$($marked.status)', expected read."
    Assert-True ($marked.notificationType -eq $Notification.notificationType) "Operational notification mark-read type was '$($marked.notificationType)', expected '$($Notification.notificationType)'."
    Assert-True ($marked.sourceAggregateId -eq $Notification.sourceAggregateId) "Operational notification mark-read sourceAggregateId was '$($marked.sourceAggregateId)', expected '$($Notification.sourceAggregateId)'."

    $markReadBody = ConvertTo-JsonPayload $marked
    Assert-True (-not ($markReadBody -match 'rawToken|rawAnswer|subject@example.com|identified-ip-hash|identified-user-agent-hash|recipient|provider|participantCode|salt|storageKey|wdr_|Smtp|Password|ConnectionStrings')) 'Operational notification mark-read response leaked sensitive data.'

    $summaryAfter = Invoke-Json GET '/operational-notifications/summary' $headers
    $unreadAfter = [int]$summaryAfter.unreadCount
    $infoAfter = [int]$summaryAfter.infoUnreadCount
    $warningAfter = [int]$summaryAfter.warningUnreadCount
    Assert-True ($unreadAfter -eq ($unreadBefore - 1)) 'Operational notification summary unread count did not decrement after mark-read.'
    if ($Notification.severity -eq 'info') {
        Assert-True ($infoAfter -eq ($infoBefore - 1)) 'Operational notification summary info unread count did not decrement after mark-read.'
        Assert-True ($warningAfter -eq $warningBefore) 'Operational notification summary warning unread count changed after marking an info notification read.'
    }
    if ($Notification.severity -eq 'warning') {
        Assert-True ($warningAfter -eq ($warningBefore - 1)) 'Operational notification summary warning unread count did not decrement after mark-read.'
        Assert-True ($infoAfter -eq $infoBefore) 'Operational notification summary info unread count changed after marking a warning notification read.'
    }

    $summaryAfterBody = ConvertTo-JsonPayload $summaryAfter
    Assert-True (-not ($summaryAfterBody -match 'rawToken|rawAnswer|subject@example.com|identified-ip-hash|identified-user-agent-hash|recipient|provider|participantCode|salt|storageKey|wdr_|Smtp|Password|ConnectionStrings')) 'Operational notification summary response leaked sensitive data after mark-read.'

    return @{
        before = $summaryBefore
        marked = $marked
        after = $summaryAfter
    }
}

function Assert-OperationalNotificationMarkAllRead {
    $summaryBefore = Invoke-Json GET '/operational-notifications/summary' $headers
    $unreadBefore = [int]$summaryBefore.unreadCount

    $marked = Invoke-Json POST '/operational-notifications/mark-all-read' $headers @{}
    Assert-True ([int]$marked.markedReadCount -eq $unreadBefore) 'Operational notification mark-all-read did not mark the expected unread notifications.'
    Assert-True ($null -ne $marked.readAt) 'Operational notification mark-all-read did not return readAt.'

    $markAllReadBody = ConvertTo-JsonPayload $marked
    Assert-True (-not ($markAllReadBody -match 'rawToken|rawAnswer|subject@example.com|identified-ip-hash|identified-user-agent-hash|recipient|provider|participantCode|salt|storageKey|sourceAggregateId|tenantId|wdr_|Smtp|Password|ConnectionStrings')) 'Operational notification mark-all-read response leaked sensitive data.'

    $summaryAfter = Invoke-Json GET '/operational-notifications/summary' $headers
    Assert-True ([int]$summaryAfter.unreadCount -eq 0) 'Operational notification summary unread count did not reach zero after mark-all-read.'
    Assert-True ([int]$summaryAfter.infoUnreadCount -eq 0) 'Operational notification summary info unread count did not reach zero after mark-all-read.'
    Assert-True ([int]$summaryAfter.warningUnreadCount -eq 0) 'Operational notification summary warning unread count did not reach zero after mark-all-read.'

    $summaryAfterBody = ConvertTo-JsonPayload $summaryAfter
    Assert-True (-not ($summaryAfterBody -match 'rawToken|rawAnswer|subject@example.com|identified-ip-hash|identified-user-agent-hash|recipient|provider|participantCode|salt|storageKey|sourceAggregateId|tenantId|wdr_|Smtp|Password|ConnectionStrings')) 'Operational notification summary response leaked sensitive data after mark-all-read.'

    return @{
        before = $summaryBefore
        marked = $marked
        after = $summaryAfter
    }
}

function Assert-WithdrawalInvalidatedDerivedArtifacts {
    param(
        [object[]]$Artifacts
    )

    foreach ($artifactRef in $Artifacts) {
        $artifact = Invoke-Json GET "/export-artifacts/$($artifactRef.id)" $headers
        Assert-True ($artifact.status -eq 'deleted') "$($artifactRef.label) status was '$($artifact.status)', expected deleted after withdrawal."
        Assert-True (-not [bool]$artifact.canDownload) "$($artifactRef.label) canDownload was '$($artifact.canDownload)', expected false after withdrawal."
        Assert-True ($null -ne $artifact.deletedAt) "$($artifactRef.label) did not expose deletedAt after withdrawal."
        Assert-True ([string]::IsNullOrWhiteSpace($artifact.checksumSha256)) "$($artifactRef.label) still exposed checksumSha256 after withdrawal."
        Assert-True ([string]::IsNullOrWhiteSpace($artifact.csvContent)) "$($artifactRef.label) still exposed CSV content after withdrawal."

        $artifactBody = ConvertTo-JsonPayload $artifact
        Assert-True (-not ($artifactBody -match 'total,visible,5,5,15,15,ok|dimension,total|answer,total|score_total_n_valid,score_total_n_expected')) 'Withdrawal invalidated artifact still exposed old CSV content.'
        Assert-True (-not ($artifactBody -match 'rawToken|rawAnswer|subject@example.com|identified-ip-hash|identified-user-agent-hash|storageKey|wdr_|Smtp|Password|ConnectionStrings')) 'Withdrawal invalidated artifact response leaked sensitive data.'
    }
}

function New-ScoringDocument {
    param([string]$RuleKey)

    return ConvertTo-JsonPayload @{
        rule_id = $RuleKey
        rule_version = '1.0.0'
        schema_version = '1.0.0'
        engine_min_version = '1.0.0'
        scale_defaults = @{
            agreement = @{
                min = 1
                max = 5
            }
        }
        inputs = @(
            @{
                id = 'core_items'
                kind = 'answers'
                items = @('q01', 'q02', 'q03')
            }
        )
        nodes = @(
            @{
                id = 'core_answers'
                op = 'select_answers'
                input = 'core_items'
            },
            @{
                id = 'scored_answers'
                op = 'reverse_code'
                input = 'core_answers'
                scale = 'agreement'
                reverse_flag_source = 'explicit_list'
                explicit_reverse_items = @('q03')
            },
            @{
                id = 'total'
                op = 'mean'
                input = 'scored_answers'
            }
        )
        outputs = @(
            @{
                code = 'total'
                node = 'total'
            }
        )
        missing_data = @{
            defaults = @{
                strategy = 'require_all'
            }
        }
    }
}

function New-ProducesDocument {
    return ConvertTo-JsonPayload @{
        scores = @('total')
        outputs = @(
            @{
                code = 'total'
                label = 'Synthetic wellbeing index'
                calculation = 'mean_1_5'
                calculation_label = 'Mean score'
                score_range = @{
                    min = 1
                    max = 5
                }
            }
        )
        interpretation = @{
            status = 'tenant_attested'
            source = 'tenant_defined'
            provenance = 'Synthetic QA01 local smoke score bands; not validated; not official.'
            scores = @{
                total = @(
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
        }
    }
}

function New-TemplateRequest {
    param(
        [string]$Suffix,
        [string]$InstrumentId
    )

    return @{
        templateName = "QA01 synthetic pulse template $Suffix"
        semver = '1.0.0'
        defaultLocale = 'en'
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
                code = 'agreement'
                type = 'likert'
                minValue = 1
                maxValue = 5
                step = 1
                naAllowed = $false
                anchors = '[{"value":1,"label":"Strongly disagree"},{"value":5,"label":"Strongly agree"}]'
            }
        )
        questions = @(
            @{
                ordinal = 1
                code = 'q01'
                type = 'likert'
                textDefault = 'After demanding work, I need extra time to recover.'
                sectionCode = 'core'
                scaleCode = 'agreement'
                required = $true
                reverseCoded = $false
                measurementLevel = 'ordinal'
                payload = '{}'
                missingCodes = '[]'
            },
            @{
                ordinal = 2
                code = 'q02'
                type = 'likert'
                textDefault = 'Small interruptions feel harder to handle during busy weeks.'
                sectionCode = 'core'
                scaleCode = 'agreement'
                required = $true
                reverseCoded = $false
                measurementLevel = 'ordinal'
                payload = '{}'
                missingCodes = '[]'
            },
            @{
                ordinal = 3
                code = 'q03'
                type = 'likert'
                textDefault = 'I can regain focus after a short break.'
                sectionCode = 'core'
                scaleCode = 'agreement'
                required = $true
                reverseCoded = $true
                measurementLevel = 'ordinal'
                payload = '{}'
                missingCodes = '[]'
            }
        )
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

function New-LaunchedWave {
    param(
        [string]$Name,
        [string]$TemplateVersionId,
        [string]$SeriesId
    )

    $campaign = Invoke-Json POST '/campaigns' $headers @{
        templateVersionId = $TemplateVersionId
        name = $Name
        responseIdentityMode = 'anonymous_longitudinal'
        campaignSeriesId = $SeriesId
        schedule = '{}'
        defaultLocale = 'en'
    }
    $readiness = Invoke-Json GET "/campaigns/$($campaign.id)/launch-readiness" $headers
    Assert-True $readiness.ready "$Name is not launch-ready."
    $launch = Invoke-Json POST "/campaigns/$($campaign.id)/launch" $headers @{}
    $openLink = Invoke-Json POST "/campaigns/$($campaign.id)/open-link" $headers @{}

    return @{
        campaign = $campaign
        readiness = $readiness
        launch = $launch
        openLink = $openLink
    }
}

function Submit-LinkedResponse {
    param(
        [object]$Wave,
        [string]$ParticipantCode,
        [string[]]$Values
    )

    $token = $Wave.openLink.token
    $entry = Invoke-Json GET "/respondent/open-links/$token"
    Assert-True ($entry.requiresParticipantCode -eq $true) "Expected participant-code step for $($Wave.campaign.name)."
    Assert-True ($entry.questions.Count -eq 3) "Expected three synthetic questions for $($Wave.campaign.name)."

    $session = Invoke-Json POST "/respondent/open-links/$token/sessions" @{} @{
        locale = 'en'
        acceptedConsentDocumentId = $entry.consentDocument.id
        acceptedGrants = $entry.consentDocument.requiredGrants
        participantCode = $ParticipantCode
    }
    $saved = Invoke-Json PUT "/respondent/open-links/$token/sessions/$($session.id)/answers" @{} (
        New-AnswersRequest $entry.questions $Values
    )
    Assert-True ($saved.savedAnswerCount -eq 3) "Expected three saved answers for $($Wave.campaign.name)."
    $submitted = Invoke-Json POST "/respondent/open-links/$token/sessions/$($session.id)/submit" @{} @{
        timeTakenMs = 1200
    }
    Assert-True ($submitted.id -eq $session.id) "Submitted response returned $($submitted.id), expected $($session.id)."

    return @{
        entry = $entry
        session = $session
        submitted = $submitted
    }
}

function Assert-CampaignEmailInvitationDelivery {
    param(
        [string]$TemplateVersionId,
        [string]$SeriesId,
        [string]$Suffix
    )

    $Campaign = Invoke-Json POST '/campaigns' $headers @{
        templateVersionId = $TemplateVersionId
        name = "QA01 Email invitation $Suffix"
        responseIdentityMode = 'anonymous'
        campaignSeriesId = $SeriesId
        schedule = '{}'
        defaultLocale = 'en'
    }
    $readiness = Invoke-Json GET "/campaigns/$($Campaign.id)/launch-readiness" $headers
    Assert-True $readiness.ready 'Email invitation smoke campaign is not launch-ready.'
    $launch = Invoke-Json POST "/campaigns/$($Campaign.id)/launch" $headers @{}
    Assert-True ($launch.status -eq 'live') "Email invitation smoke campaign launch status was '$($launch.status)', expected live."

    $recipient = "qa01-$Suffix@example.test"
    $batch = Invoke-Json POST "/campaigns/$($Campaign.id)/invitation-batches" $headers @{
        recipients = @(
            @{
                email = $recipient
            }
        )
    }
    Assert-True ($batch.campaignId -eq $Campaign.id) "Email invitation batch campaignId was '$($batch.campaignId)', expected '$($Campaign.id)'."
    Assert-True ([int]$batch.createdInvitationCount -eq 1) "Email invitation batch created '$($batch.createdInvitationCount)' invitations, expected 1."
    $invitation = @($batch.invitations)[0]
    Assert-True ($invitation.recipient -eq $recipient) "Email invitation recipient was '$($invitation.recipient)', expected '$recipient'."
    Assert-True ($invitation.status -eq 'queued') "Email invitation status was '$($invitation.status)', expected queued."
    Assert-True ($null -eq $invitation.token) 'Email invitation batch returned a raw invitation token.'
    Assert-True ($null -eq $invitation.respondentPath) 'Email invitation batch returned a raw respondent path.'

    $delivery = Invoke-Json POST "/campaigns/$($Campaign.id)/notification-deliveries/process" $headers @{
        batchSize = 5
    }
    Assert-True ($delivery.campaignId -eq $Campaign.id) "Email delivery campaignId was '$($delivery.campaignId)', expected '$($Campaign.id)'."
    Assert-True ([int]$delivery.processedCount -eq 1) "Email delivery processed '$($delivery.processedCount)' notifications, expected 1."
    Assert-True ([int]$delivery.sentCount -eq 1) "Email delivery sent '$($delivery.sentCount)' notifications, expected 1."
    Assert-True ([int]$delivery.failedCount -eq 0) "Email delivery failed '$($delivery.failedCount)' notifications, expected 0."
    $proof = @($delivery.deliveries)[0]
    Assert-True ($proof.notificationId -eq $invitation.notificationId) "Email delivery notificationId was '$($proof.notificationId)', expected '$($invitation.notificationId)'."
    Assert-True ($proof.recipient -eq $recipient) "Email delivery recipient was '$($proof.recipient)', expected '$recipient'."
    Assert-True ($proof.status -eq 'sent') "Email delivery status was '$($proof.status)', expected sent."
    Assert-True ($proof.provider -eq 'local-dev') "Email delivery provider was '$($proof.provider)', expected local-dev."
    Assert-True (-not [string]::IsNullOrWhiteSpace($proof.providerMessageId)) 'Email delivery did not return a provider message id.'
    Assert-True ($proof.respondentPath -like '/r/inv_*') 'Email delivery did not return a local-dev respondent path.'
    Assert-True ($null -eq $proof.error) "Email delivery returned unexpected error '$($proof.error)'."

    $deliveryBody = ConvertTo-JsonPayload $delivery
    Assert-True (-not ($deliveryBody -match '"token"\s*:')) 'Email delivery response included a raw token field.'
    Assert-True ([regex]::Matches($deliveryBody, 'inv_').Count -eq 1) 'Email delivery response exposed more than one raw invitation token marker.'
    Assert-True (-not ($deliveryBody -match 'Smtp|Password|secret|storageKey|ConnectionStrings|tenantId')) 'Campaign email delivery response leaked sensitive data.'

    $requeue = Invoke-Json POST "/campaigns/$($Campaign.id)/notification-deliveries/requeue-failed" $headers @{
        batchSize = 5
        confirmedAnotherEmailAppropriate = $true
    }
    Assert-True ($requeue.campaignId -eq $Campaign.id) "Email failed-delivery requeue campaignId was '$($requeue.campaignId)', expected '$($Campaign.id)'."
    Assert-True ([int]$requeue.requestedBatchSize -eq 5) "Email failed-delivery requeue requestedBatchSize was '$($requeue.requestedBatchSize)', expected 5."
    Assert-True ([int]$requeue.requeuedCount -eq 0) 'Campaign email failed-delivery requeue no-op did not return zero.'
    $requeueBody = ConvertTo-JsonPayload $requeue
    Assert-True (-not ($requeueBody -match 'inv_|Smtp|Password|secret|storageKey|ConnectionStrings|tenantId')) 'Campaign email failed-delivery requeue response leaked sensitive data.'

    return @{
        campaign = $Campaign
        batch = $batch
        delivery = $delivery
        requeue = $requeue
    }
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

if (-not $TenantId) {
    $TenantId = Get-EnvValue $envValues 'PUBLIC_DEV_TENANT_ID' '11111111-1111-4111-8111-111111111111'
}

if (-not $UserId) {
    $UserId = Get-EnvValue $envValues 'PUBLIC_DEV_USER_ID' '22222222-2222-4222-8222-222222222222'
}

if (-not $ParticipantCodePrefix) {
    $ParticipantCodePrefix = 'qa01'
}

$resolvedSessionCookie = Resolve-SessionCookie `
    -DirectSessionCookie $SessionCookie `
    -SessionCookiePath $SessionCookiePath `
    -RequireAuthenticatedSession:$RequireAuthenticatedSession
$remoteCookieAuthenticated = ![string]::IsNullOrWhiteSpace($resolvedSessionCookie)

if ($remoteCookieAuthenticated) {
    $resolvedAuth = Resolve-RemoteAuthenticatedRequestState -CookieHeader $resolvedSessionCookie
    $headers = @{
        'X-Tenant-Id' = $TenantId
        Origin = $WebBaseUrl
        Cookie = $resolvedAuth.CookieHeader
        'X-CSRF-TOKEN' = $resolvedAuth.CsrfToken
    }
} else {
    $headers = @{
        'X-Tenant-Id' = $TenantId
        'X-Dev-User-Id' = $UserId
        'X-Dev-Tenant-Memberships' = $TenantId
        'X-Dev-Permissions' = 'setup.manage'
    }
}

Write-Host "QA01 live product-spine smoke"
Write-Host "API: $ApiBaseUrl"
Write-Host "Web: $WebBaseUrl"

$health = Wait-HttpOk -Url (Join-Url $ApiBaseUrl '/health')
$healthJson = $health.Content | ConvertFrom-Json
Assert-True ($healthJson.status -eq 'ok') "Unexpected health status: $($health.Content)"

$unauthenticatedStatus = Get-HttpStatus -Url (Join-Url $ApiBaseUrl '/auth/session')
Assert-True ($unauthenticatedStatus -eq 401) "Expected unauthenticated /auth/session to return 401, got $unauthenticatedStatus."

$session = Invoke-AuthenticatedProductSpineSession -RemoteCookieAuthenticated:$remoteCookieAuthenticated
$sessionTenantId = Get-SessionTenantId -Session $session
$sessionPermissions = Get-SessionPermissions -Session $session
Assert-True ($sessionTenantId -eq $TenantId) "Expected authenticated tenant $TenantId, got $sessionTenantId."
Assert-True ($sessionPermissions -contains 'setup.manage') 'Authenticated product-spine session did not include setup.manage permission.'

$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 8)
$ruleKey = "qa01.$suffix.total"

$instrument = Invoke-Json POST '/instruments/private-imports' $headers @{
    code = "qa01-smoke-$suffix"
    version = '1.0.0'
    fullName = "QA01 synthetic pulse $suffix"
    domain = 'psychometric'
    provenanceNote = 'Synthetic QA01 local smoke content with tenant-attested local rights.'
    rightsStatus = 'attested_by_tenant'
    validityLabel = 'tenant_provided'
    licenseType = 'unknown'
}
$template = Invoke-Json POST '/template-versions' $headers (New-TemplateRequest $suffix $instrument.id)
$scoring = Invoke-Json POST '/scoring-rules' $headers @{
    templateVersionId = $template.templateVersionId
    ruleKey = $ruleKey
    ruleVersion = '1.0.0'
    schemaVersion = 'scoring-rule/v1'
    engineMinVersion = 'engine/v1'
    document = New-ScoringDocument $ruleKey
    produces = New-ProducesDocument
    compatibility = '{}'
}
$series = Invoke-Json POST '/campaign-series' $headers @{
    name = "QA01 live product spine $suffix"
}

$wave1 = New-LaunchedWave "QA01 Wave 1 $suffix" $template.templateVersionId $series.id
$wave2 = New-LaunchedWave "QA01 Wave 2 $suffix" $template.templateVersionId $series.id

$participantCodes = @(
    "$ParticipantCodePrefix-$suffix-alpha",
    "$ParticipantCodePrefix-$suffix-bravo",
    "$ParticipantCodePrefix-$suffix-charlie",
    "$ParticipantCodePrefix-$suffix-delta",
    "$ParticipantCodePrefix-$suffix-echo"
)
$baselineValues = @(
    @('3', '3', '4'),
    @('3', '4', '4'),
    @('4', '4', '5'),
    @('4', '5', '5'),
    @('5', '5', '5')
)
$comparisonValues = @(
    @('4', '4', '5'),
    @('4', '5', '5'),
    @('5', '5', '5'),
    @('5', '5', '5'),
    @('5', '5', '5')
)

$submittedResponses = @()
for ($i = 0; $i -lt $participantCodes.Count; $i++) {
    $submittedResponses += ,(Submit-LinkedResponse $wave1 $participantCodes[$i] $baselineValues[$i])
    $submittedResponses += ,(Submit-LinkedResponse $wave2 $participantCodes[$i] $comparisonValues[$i])
}

$withdrawalSmoke = Assert-WithdrawalTokenIssueAndConsume -SubmittedResponse $submittedResponses[0]
Assert-WithdrawalRequestReviewVisibility -Withdrawal $withdrawalSmoke -ExpectedStatus 'requested' | Out-Null

$overview = Invoke-Json GET '/workspace-overview' $headers
$list = Invoke-Json GET "/campaign-series?search=$suffix" $headers
$hub = Invoke-Json GET "/campaign-series/$($series.id)" $headers
$setupWorkspace = Invoke-Json GET "/campaign-series/$($series.id)/setup-workspace" $headers
$operationsWorkspace = Invoke-Json GET "/campaign-series/$($series.id)/operations-workspace" $headers
$reportsWorkspace = Invoke-Json GET "/campaign-series/$($series.id)/reports-workspace" $headers
$wavesWorkspace = Invoke-Json GET "/campaign-series/$($series.id)/waves-workspace" $headers

Assert-True ($overview.totals.campaignSeriesCount -ge 1) 'Workspace overview did not include campaign series totals.'
Assert-True ($list.items.Count -ge 1) 'Campaign-series list did not return the smoke series.'
Assert-True ($hub.id -eq $series.id) "Selected-series hub returned $($hub.id), expected $($series.id)."
Assert-True ($setupWorkspace.series.id -eq $series.id) 'Setup workspace returned the wrong series.'
Assert-True ($operationsWorkspace.summary.submittedResponseCount -ge 10) 'Operations workspace did not observe submitted responses.'
Assert-True ($reportsWorkspace.summary.submittedResponseCount -ge 10) 'Reports workspace did not observe submitted responses.'
Assert-True ($wavesWorkspace.summary.completeTrajectoryCount -ge 5) 'Waves workspace did not observe linked trajectories.'

$reportProof = Invoke-Json GET "/campaigns/$($wave1.campaign.id)/report-proof" $headers
Assert-True ($reportProof.scores.Count -gt 0) 'Report proof did not include scores.'
Assert-ReportScoreMetadata $reportProof 'Wave 1 report proof'
$reportExport = Invoke-Json POST "/campaigns/$($wave1.campaign.id)/report-proof/exports" $headers @{}
Assert-True ($reportExport.status -eq 'succeeded') 'Report proof export did not succeed.'
$artifact = Invoke-Json GET "/export-artifacts/$($reportExport.id)" $headers
Assert-True ($artifact.id -eq $reportExport.id) 'Export artifact fetch returned the wrong artifact.'
Assert-ReportExportScoreMetadata $artifact 'Wave 1 report export'
$responseExport = Invoke-Json POST "/campaign-series/$($series.id)/response-exports" $headers @{}
Assert-True ($responseExport.status -eq 'succeeded') 'Campaign-series response export did not succeed.'
$responseArtifact = Invoke-Json GET "/export-artifacts/$($responseExport.id)" $headers
Assert-True ($responseArtifact.id -eq $responseExport.id) 'Response export artifact fetch returned the wrong artifact.'
Assert-ResponseExportScoreMetadata $responseArtifact 'Campaign-series response export'

$reportPdfArtifact = Invoke-Json POST "/campaign-series/$($series.id)/report-pdf-artifacts" $headers @{}
Assert-True ($reportPdfArtifact.artifactType -eq 'campaign_series_report_pdf') "Expected campaign_series_report_pdf, got $($reportPdfArtifact.artifactType)."
Assert-True ($reportPdfArtifact.format -eq 'pdf') "Expected report PDF artifact format pdf, got $($reportPdfArtifact.format)."
Assert-True (@('succeeded', 'failed') -contains $reportPdfArtifact.status) "Expected terminal report PDF artifact status, got $($reportPdfArtifact.status)."
$reportPdfArtifactFetched = Invoke-Json GET "/export-artifacts/$($reportPdfArtifact.id)" $headers
Assert-True ($reportPdfArtifactFetched.id -eq $reportPdfArtifact.id) 'Report PDF artifact fetch returned the wrong artifact.'

function Assert-ReportPdfArtifactDeliveryState {
    param([object]$Artifact)

    if ($Artifact.status -eq 'succeeded') {
        if ($headers.ContainsKey('Cookie')) {
            $download = Invoke-HttpClientRequest `
                -Method GET `
                -Url (Join-Url $ApiBaseUrl "/export-artifacts/$($Artifact.id)/download") `
                -Headers $headers `
                -TimeoutSec 30
        } else {
            $download = Invoke-WebRequest `
                -Uri (Join-Url $ApiBaseUrl "/export-artifacts/$($Artifact.id)/download") `
                -Headers $headers `
                -UseBasicParsing `
                -TimeoutSec 30
        }

        Assert-True ($download.StatusCode -eq 200) "Report PDF download returned $($download.StatusCode)."
        $contentType = [string]$download.Headers['Content-Type']
        Assert-True ($contentType -match 'application/pdf') "Report PDF download returned content type $contentType."
        return
    }

    Assert-True ($Artifact.status -eq 'failed') "Expected report PDF artifact to be succeeded or failed, got $($Artifact.status)."
    Assert-True (-not [string]::IsNullOrWhiteSpace($Artifact.failureReasonCode)) 'Failed report PDF artifact did not include a safe failureReasonCode.'
}

function Invoke-WebRequestAllowHttpError {
    param([string]$Path)

    if ($headers.ContainsKey('Cookie')) {
        return Invoke-HttpClientRequest `
            -Method GET `
            -Url (Join-Url $ApiBaseUrl $Path) `
            -Headers $headers `
            -TimeoutSec 30 `
            -AllowHttpError
    }

    try {
        return Invoke-WebRequest `
            -Uri (Join-Url $ApiBaseUrl $Path) `
            -Headers $headers `
            -UseBasicParsing `
            -TimeoutSec 30
    }
    catch {
        $response = $_.Exception.Response
        if ($null -eq $response) {
            throw
        }

        if ($response -is [System.Net.Http.HttpResponseMessage]) {
            $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            return [pscustomobject]@{
                StatusCode = [int]$response.StatusCode
                Content = $content
            }
        }

        $stream = $response.GetResponseStream()
        $reader = [System.IO.StreamReader]::new($stream)
        try {
            $content = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            Content = $content
        }
    }
}

function Assert-ReportPdfArtifactSignedDownloadUrlState {
    param([object]$Artifact)

    if ($Artifact.status -ne 'succeeded') {
        return
    }

    $response = Invoke-WebRequestAllowHttpError -Path "/export-artifacts/$($Artifact.id)/signed-download-url"
    $body = [string]$response.Content
    Assert-True ($body -notmatch 'storageKey') 'Report PDF signed download URL response leaked storageKey.'
    Assert-True ($body -notmatch 'secret') 'Report PDF signed download URL response leaked secret material.'

    if ([int]$response.StatusCode -eq 200) {
        $payload = $body | ConvertFrom-Json
        Assert-True ($payload.id -eq $Artifact.id) 'Report PDF signed download URL returned the wrong artifact id.'
        Assert-True (-not [string]::IsNullOrWhiteSpace($payload.url)) 'Report PDF signed download URL response did not include a URL.'
        Assert-True (-not [string]::IsNullOrWhiteSpace($payload.expiresAt)) 'Report PDF signed download URL response did not include expiresAt.'
        return
    }

    if ([int]$response.StatusCode -eq 409) {
        $problem = $body | ConvertFrom-Json
        Assert-True ($problem.title -eq 'export_artifact_object.signed_urls_not_supported') 'Report PDF signed download URL did not fail with the safe local-storage unsupported problem.'
        return
    }

    throw "Report PDF signed download URL returned HTTP $($response.StatusCode)."
}

Assert-ReportPdfArtifactDeliveryState -Artifact $reportPdfArtifactFetched
Assert-ReportPdfArtifactSignedDownloadUrlState -Artifact $reportPdfArtifactFetched

function Wait-OperationalNotificationForArtifact {
    param(
        [string]$ArtifactId,
        [int]$Attempts = 30
    )

    for ($i = 0; $i -lt $Attempts; $i++) {
        $notifications = Invoke-Json GET '/operational-notifications?limit=50' $headers
        $match = @($notifications.notifications | Where-Object { $_.sourceAggregateId -eq $ArtifactId })
        if ($match.Count -gt 0) {
            return $match[0]
        }

        Start-Sleep -Seconds 2
    }

    throw "Timed out waiting for operational notification for report PDF artifact $ArtifactId."
}

$reportPdfNotification = Wait-OperationalNotificationForArtifact -ArtifactId $reportPdfArtifact.id
Assert-True ($reportPdfNotification.sourceAggregateId -eq $reportPdfArtifact.id) 'Operational notification did not reference the report PDF artifact.'

$twoWaveProof = Invoke-Json GET "/campaign-series/$($series.id)/two-wave-proof" $headers
Assert-True ($twoWaveProof.completeTrajectoryCount -ge 5) 'Two-wave proof did not count five complete trajectories.'
$waveComparisonProof = Invoke-Json GET "/campaign-series/$($series.id)/wave-comparison-proof" $headers
Assert-True ($waveComparisonProof.scores.Count -gt 0) 'Wave-comparison proof did not include score comparisons.'
Assert-WaveComparisonScoreMetadata $waveComparisonProof

$manualScoreProof = Invoke-Json POST "/respondent/sessions/$($submittedResponses[0].session.id)/scores" $headers @{}
Assert-True ($manualScoreProof.scores.Count -gt 0) 'Manual score endpoint compatibility proof did not return scores.'
Assert-ScoreResponseMetadata $manualScoreProof 'Manual score endpoint compatibility'

$closed = Invoke-Json POST "/campaign-series/$($series.id)/campaigns/$($wave2.campaign.id)/close" $headers @{
    reason = 'QA01 smoke collection complete'
}
Assert-True ($closed.status -eq 'closed') "Expected closed campaign status, got $($closed.status)."
Assert-True ($null -ne $closed.closedAt) 'Closed campaign did not return closedAt provenance.'
$closedEntryStatus = Get-HttpStatus -Url (Join-Url $ApiBaseUrl "/respondent/open-links/$($wave2.openLink.token)")
Assert-True ($closedEntryStatus -eq 404) "Expected closed public entry to return 404, got $closedEntryStatus."

$operationsAfterClose = Invoke-Json GET "/campaign-series/$($series.id)/operations-workspace" $headers
$closedCampaign = @($operationsAfterClose.campaigns | Where-Object { $_.id -eq $wave2.campaign.id })[0]
Assert-True ($closedCampaign.status -eq 'closed') "Operations workspace did not expose closed campaign state for $($wave2.campaign.id)."
Assert-True ($null -ne $closedCampaign.closedAt) 'Operations workspace did not expose closedAt provenance.'
$reportsAfterClose = Invoke-Json GET "/campaign-series/$($series.id)/reports-workspace" $headers
$wavesAfterClose = Invoke-Json GET "/campaign-series/$($series.id)/waves-workspace" $headers
Assert-True ($reportsAfterClose.summary.submittedResponseCount -ge 10) 'Reports workspace lost submitted responses after close.'
Assert-True ($reportsAfterClose.summary.preliminaryLiveReportCount -ge 1) 'Reports workspace did not expose preliminary-live report finality.'
Assert-True ($reportsAfterClose.summary.closedWaveReportCount -ge 1) 'Reports workspace did not expose closed-wave report finality.'
Assert-True ($wavesAfterClose.summary.completeTrajectoryCount -ge 5) 'Waves workspace lost linked trajectories after close.'
Assert-True ($wavesAfterClose.summary.preliminaryLiveWaveCount -ge 1) 'Waves workspace did not expose preliminary-live wave finality.'
Assert-True ($wavesAfterClose.summary.closedWaveCount -ge 1) 'Waves workspace did not expose closed-wave finality.'
$liveReportCampaign = @($reportsAfterClose.campaigns | Where-Object { $_.id -eq $wave1.campaign.id })[0]
$closedReportCampaign = @($reportsAfterClose.campaigns | Where-Object { $_.id -eq $wave2.campaign.id })[0]
$liveWave = @($wavesAfterClose.waves | Where-Object { $_.id -eq $wave1.campaign.id })[0]
$closedWave = @($wavesAfterClose.waves | Where-Object { $_.id -eq $wave2.campaign.id })[0]
Assert-True ($liveReportCampaign.dataFinality -eq 'preliminary_live') "Expected live report finality preliminary_live, got $($liveReportCampaign.dataFinality)."
Assert-True ($closedReportCampaign.dataFinality -eq 'closed_wave') "Expected closed report finality closed_wave, got $($closedReportCampaign.dataFinality)."
Assert-True ($null -ne $closedReportCampaign.closedAt) 'Reports workspace did not expose closed report closedAt provenance.'
Assert-True ($liveWave.dataFinality -eq 'preliminary_live') "Expected live wave finality preliminary_live, got $($liveWave.dataFinality)."
Assert-True ($closedWave.dataFinality -eq 'closed_wave') "Expected closed wave finality closed_wave, got $($closedWave.dataFinality)."
Assert-True ($null -ne $closedWave.closedAt) 'Waves workspace did not expose closed wave closedAt provenance.'
$closedReportProof = Invoke-Json GET "/campaigns/$($wave2.campaign.id)/report-proof" $headers
Assert-True ($closedReportProof.dataFinality -eq 'closed_wave') "Expected closed report proof finality closed_wave, got $($closedReportProof.dataFinality)."
Assert-True ($null -ne $closedReportProof.closedAt) 'Closed report proof did not expose closedAt provenance.'
Assert-ReportScoreMetadata $closedReportProof 'Closed wave report proof'
$closedReportExport = Invoke-Json POST "/campaigns/$($wave2.campaign.id)/report-proof/exports" $headers @{}
$closedReportArtifact = Invoke-Json GET "/export-artifacts/$($closedReportExport.id)" $headers
Assert-True ($closedReportArtifact.csvContent -match 'campaign_closed_at,campaign_data_finality') 'Closed report export CSV did not include finality columns.'
Assert-True ($closedReportArtifact.csvContent -match 'closed_wave') 'Closed report export CSV did not include closed_wave finality.'
Assert-True ($closedReportArtifact.codebookJson -match 'closed_wave') 'Closed report export codebook did not include closed_wave finality.'
Assert-ReportExportScoreMetadata $closedReportArtifact 'Closed report export'
$responseExportAfterClose = Invoke-Json POST "/campaign-series/$($series.id)/response-exports" $headers @{}
$responseArtifactAfterClose = Invoke-Json GET "/export-artifacts/$($responseExportAfterClose.id)" $headers
Assert-True ($responseArtifactAfterClose.csvContent -match 'campaign_status,campaign_closed_at,campaign_data_finality') 'Response export CSV did not include campaign lifecycle columns.'
Assert-True ($responseArtifactAfterClose.csvContent -match 'preliminary_live') 'Response export CSV did not include preliminary_live finality.'
Assert-True ($responseArtifactAfterClose.csvContent -match 'closed_wave') 'Response export CSV did not include closed_wave finality.'
Assert-True ($responseArtifactAfterClose.codebookJson -match 'closedWaveResponseCount') 'Response export codebook did not include closed-wave response count.'
Assert-ResponseExportScoreMetadata $responseArtifactAfterClose 'Post-close response export'
$closedReportPdfArtifact = Invoke-Json POST "/campaign-series/$($series.id)/report-pdf-artifacts" $headers @{}
Assert-True ($closedReportPdfArtifact.artifactType -eq 'campaign_series_report_pdf') "Expected post-close campaign_series_report_pdf, got $($closedReportPdfArtifact.artifactType)."
Assert-True ($closedReportPdfArtifact.format -eq 'pdf') "Expected post-close report PDF artifact format pdf, got $($closedReportPdfArtifact.format)."
Assert-True (@('succeeded', 'failed') -contains $closedReportPdfArtifact.status) "Expected terminal post-close report PDF artifact status, got $($closedReportPdfArtifact.status)."
$closedReportPdfArtifactFetched = Invoke-Json GET "/export-artifacts/$($closedReportPdfArtifact.id)" $headers
Assert-True ($closedReportPdfArtifactFetched.id -eq $closedReportPdfArtifact.id) 'Post-close report PDF artifact fetch returned the wrong artifact.'
Assert-ReportPdfArtifactDeliveryState -Artifact $closedReportPdfArtifactFetched
Assert-ReportPdfArtifactSignedDownloadUrlState -Artifact $closedReportPdfArtifactFetched
$closedReportPdfNotification = Wait-OperationalNotificationForArtifact -ArtifactId $closedReportPdfArtifact.id
Assert-True ($closedReportPdfNotification.sourceAggregateId -eq $closedReportPdfArtifact.id) 'Operational notification did not reference the post-close report PDF artifact.'
$withdrawalExecution = Assert-WithdrawalRequestApproveAndExecute -Withdrawal $withdrawalSmoke
Assert-WithdrawalRequestReviewVisibility -Withdrawal $withdrawalSmoke -ExpectedStatus 'completed' | Out-Null
$invalidationArtifacts = @(
    [pscustomobject]@{
        id = $reportExport.id
        label = 'Wave 1 report export'
    },
    [pscustomobject]@{
        id = $responseExport.id
        label = 'Pre-withdrawal response export'
    },
    [pscustomobject]@{
        id = $responseExportAfterClose.id
        label = 'Post-close response export'
    }
)
if ($reportPdfArtifactFetched.status -eq 'succeeded') {
    $invalidationArtifacts += [pscustomobject]@{
        id = $reportPdfArtifactFetched.id
        label = 'Report PDF artifact'
    }
}
if ($closedReportPdfArtifactFetched.status -eq 'succeeded') {
    $invalidationArtifacts += [pscustomobject]@{
        id = $closedReportPdfArtifactFetched.id
        label = 'Post-close report PDF artifact'
    }
}
Assert-WithdrawalInvalidatedDerivedArtifacts -Artifacts $invalidationArtifacts | Out-Null
$postWithdrawalForbiddenMarkers = @($withdrawalSmoke.issue.rawToken) + $participantCodes
$postWithdrawalReportExport = Invoke-Json POST "/campaigns/$($wave1.campaign.id)/report-proof/exports" $headers @{}
Assert-True ($postWithdrawalReportExport.status -eq 'succeeded') 'Post-withdrawal report export did not succeed.'
$postWithdrawalReportArtifact = Invoke-Json GET "/export-artifacts/$($postWithdrawalReportExport.id)" $headers
Assert-PostWithdrawalFreshExportArtifactSafety -Artifact $postWithdrawalReportArtifact -ForbiddenMarkers $postWithdrawalForbiddenMarkers -Context 'Post-withdrawal report export'
Assert-ReportExportScoreMetadata $postWithdrawalReportArtifact 'Post-withdrawal report export'
$postWithdrawalResponseExport = Invoke-Json POST "/campaign-series/$($series.id)/response-exports" $headers @{}
Assert-True ($postWithdrawalResponseExport.status -eq 'succeeded') 'Post-withdrawal response export did not succeed.'
$postWithdrawalResponseArtifact = Invoke-Json GET "/export-artifacts/$($postWithdrawalResponseExport.id)" $headers
Assert-PostWithdrawalFreshExportArtifactSafety -Artifact $postWithdrawalResponseArtifact -ForbiddenMarkers $postWithdrawalForbiddenMarkers -Context 'Post-withdrawal response export'
Assert-ResponseExportScoreMetadata $postWithdrawalResponseArtifact 'Post-withdrawal response export'
$postWithdrawalReportPdfArtifact = Invoke-Json POST "/campaign-series/$($series.id)/report-pdf-artifacts" $headers @{}
$postWithdrawalReportPdfArtifactFetched = Invoke-Json GET "/export-artifacts/$($postWithdrawalReportPdfArtifact.id)" $headers
Assert-PostWithdrawalReportPdfArtifactSafety -Artifact $postWithdrawalReportPdfArtifactFetched -ExpectedId $postWithdrawalReportPdfArtifact.id -ForbiddenMarkers $postWithdrawalForbiddenMarkers -Context 'Post-withdrawal report PDF artifact'
Assert-ReportPdfArtifactDeliveryState -Artifact $postWithdrawalReportPdfArtifactFetched
Assert-ReportPdfArtifactSignedDownloadUrlState -Artifact $postWithdrawalReportPdfArtifactFetched
$postWithdrawalReportPdfNotification = Wait-OperationalNotificationForArtifact -ArtifactId $postWithdrawalReportPdfArtifact.id
Assert-True ($postWithdrawalReportPdfNotification.sourceAggregateId -eq $postWithdrawalReportPdfArtifact.id) 'Operational notification did not reference the post-withdrawal report PDF artifact.'
$withdrawalNotification = Assert-WithdrawalTerminalNotification -Withdrawal $withdrawalSmoke -Execution $withdrawalExecution
$notificationSummaryAndMarkReadProof = Assert-OperationalNotificationSummaryAndMarkRead -Notification $withdrawalNotification
$notificationMarkAllReadProof = Assert-OperationalNotificationMarkAllRead
$campaignEmailDeliveryProof = Assert-CampaignEmailInvitationDelivery -TemplateVersionId $template.templateVersionId -SeriesId $series.id -Suffix $suffix

if (-not $SkipWebCheck) {
    $web = Wait-HttpOk -Url (Join-Url $WebBaseUrl '/')
    Assert-True ($web.Content -match 'Tenant setup workspace|Workspace') 'Frontend did not return the expected shell.'
}

$ownerInspectionRoutes = @(
    (Join-Url $WebBaseUrl "/app/campaign-series/$($series.id)"),
    (Join-Url $WebBaseUrl "/app/campaign-series/$($series.id)/setup"),
    (Join-Url $WebBaseUrl "/app/campaign-series/$($series.id)/operations"),
    (Join-Url $WebBaseUrl "/app/campaign-series/$($series.id)/reports"),
    (Join-Url $WebBaseUrl "/app/campaign-series/$($series.id)/waves")
)

$productSpineEvidence = [ordered]@{
    schemaVersion = 1
    generatedAt = [DateTimeOffset]::UtcNow.ToString('o')
    runner = 'deploy/staging/smoke-product-spine.ps1'
    status = 'passed'
    authProof = [ordered]@{
        mode = $(if ($remoteCookieAuthenticated) { 'remoteCookieAuthenticated' } else { 'localDevelopmentAuth' })
        authenticatedSessionProven = $true
        setupManagePermissionProven = $true
        csrfTokenProven = $remoteCookieAuthenticated
        cookieSourceProven = $remoteCookieAuthenticated
    }
    ownerInspectionRoutes = $ownerInspectionRoutes
    productMilestones = [ordered]@{
        campaignSeriesCreated = $true
        waveOneCreated = $true
        waveTwoCreated = $true
        preliminaryLiveFinalityProven = $true
        closedWaveFinalityProven = $true
        reportExportsProven = $true
        responseExportsProven = $true
        scoreDefinitionMetadataProven = $true
        reportPdfArtifactsProven = $true
        withdrawalRequestReviewVisibilityProven = $true
        withdrawalExecutionProven = $true
        derivedArtifactInvalidationProven = $true
        postWithdrawalExportRegenerationProven = $true
        postWithdrawalReportPdfRegenerationProven = $true
        operationalNotificationsProven = $true
        campaignInvitationDeliveryProven = $true
    }
    artifactProofs = [ordered]@{
        reportExportArtifact = [ordered]@{
            id = $reportExport.id
            status = $reportExport.status
            artifactType = $reportArtifact.artifactType
            format = $reportArtifact.format
        }
        responseExportArtifact = [ordered]@{
            id = $responseExport.id
            status = $responseExport.status
            artifactType = $responseArtifact.artifactType
            format = $responseArtifact.format
        }
        closedReportExportArtifact = [ordered]@{
            id = $closedReportExport.id
            status = $closedReportExport.status
            artifactType = $closedReportArtifact.artifactType
            format = $closedReportArtifact.format
        }
        postCloseResponseExportArtifact = [ordered]@{
            id = $responseExportAfterClose.id
            status = $responseExportAfterClose.status
            artifactType = $responseArtifactAfterClose.artifactType
            format = $responseArtifactAfterClose.format
        }
        postCloseReportPdfArtifact = [ordered]@{
            id = $closedReportPdfArtifact.id
            status = $closedReportPdfArtifactFetched.status
            artifactType = $closedReportPdfArtifactFetched.artifactType
            format = $closedReportPdfArtifactFetched.format
        }
        postWithdrawalReportExportArtifact = [ordered]@{
            id = $postWithdrawalReportExport.id
            status = $postWithdrawalReportArtifact.status
            artifactType = $postWithdrawalReportArtifact.artifactType
            format = $postWithdrawalReportArtifact.format
        }
        postWithdrawalResponseExportArtifact = [ordered]@{
            id = $postWithdrawalResponseExport.id
            status = $postWithdrawalResponseArtifact.status
            artifactType = $postWithdrawalResponseArtifact.artifactType
            format = $postWithdrawalResponseArtifact.format
        }
        postWithdrawalReportPdfArtifact = [ordered]@{
            id = $postWithdrawalReportPdfArtifact.id
            status = $postWithdrawalReportPdfArtifactFetched.status
            artifactType = $postWithdrawalReportPdfArtifactFetched.artifactType
            format = $postWithdrawalReportPdfArtifactFetched.format
        }
    }
    withdrawalProof = [ordered]@{
        requestId = $withdrawalSmoke.request.requestId
        targetKind = $withdrawalSmoke.request.targetKind
        requestedAction = $withdrawalSmoke.request.requestedAction
        finalStatus = 'completed'
        reviewVisibilityProven = $true
        terminalNotificationProven = $true
        invalidatedDerivedArtifactCount = @($invalidationArtifacts).Count
        postWithdrawalRegenerationProven = $true
    }
    operationalNotificationProof = [ordered]@{
        terminalNotificationProven = $true
        summaryProven = $true
        markReadProven = $true
        markAllReadProven = $true
        inAppOnly = $true
        emailRoutingProven = $false
        unreadBeforeMarkRead = [int]$notificationSummaryAndMarkReadProof.before.unreadCount
        unreadAfterMarkRead = [int]$notificationSummaryAndMarkReadProof.after.unreadCount
        markAllReadInputUnreadCount = [int]$notificationMarkAllReadProof.before.unreadCount
        markAllReadMarkedReadCount = [int]$notificationMarkAllReadProof.marked.markedReadCount
        unreadAfterMarkAllRead = [int]$notificationMarkAllReadProof.after.unreadCount
    }
    reportExportProof = [ordered]@{
        scoreMetadataProven = $true
        reportExportCodebookMetadataProven = $true
        responseExportScoreMetadataProven = $true
        waveComparisonScoreMetadataProven = $true
        reportPdfDeliveryChecked = $true
        signedDownloadUrlChecked = $true
        postCloseReportPdfProven = $true
        postWithdrawalExportRegenerationProven = $true
        postWithdrawalReportPdfRegenerationProven = $true
        artifactLeakChecksProven = $true
    }
    campaignEmailDeliveryProof = [ordered]@{
        invitationBatchProven = $true
        deliveryProcessingProven = $true
        provider = 'local-dev'
        localDevProviderProven = $true
        createdInvitationCount = [int]$campaignEmailDeliveryProof.batch.createdInvitationCount
        processedCount = [int]$campaignEmailDeliveryProof.delivery.processedCount
        sentCount = [int]$campaignEmailDeliveryProof.delivery.sentCount
        failedCount = [int]$campaignEmailDeliveryProof.delivery.failedCount
        failedRequeueNoopProven = $true
        failedRequeueNoopRequeuedCount = [int]$campaignEmailDeliveryProof.requeue.requeuedCount
        smtpDeliveryProven = $false
        failedRequeueRecoveryProven = $false
    }
    limitations = @(
        'Q-053 blocks real-person production legal/GDPR/DPA claims; this evidence is engineering proof only.',
        'Q-054 blocks outbound operational-notification email routing and claims that operational events are emailed.',
        'The product-spine evidence omits cookies, raw headers, session bodies, raw withdrawal tokens, participant codes, answers, storage keys, credential values, connection strings, tenant ids, and email addresses.',
        'Remote VPS product-spine proof requires owner-supplied origins and a current browser session cookie supplied through an ignored file or STAGING_SESSION_COOKIE.'
    )
}

Write-ProductSpineEvidence -Evidence $productSpineEvidence

Write-Host ''
Write-Host 'Owner inspection routes'
$ownerInspectionRoutes | ForEach-Object { Write-Host $_ }
Write-Host ''
Write-Host "Created campaign series: $($series.id)"
Write-Host "Wave 1 campaign: $($wave1.campaign.id)"
Write-Host "Wave 2 campaign: $($wave2.campaign.id)"
Write-Host "Report export artifact: $($reportExport.id)"
Write-Host "Response export artifact: $($responseExport.id)"
Write-Host "Closed report export artifact: $($closedReportExport.id)"
Write-Host "Post-close response export artifact: $($responseExportAfterClose.id)"
Write-Host "Post-close report PDF artifact: $($closedReportPdfArtifact.id)"
Write-Host "Post-withdrawal response export artifact: $($postWithdrawalResponseExport.id)"
Write-Host "Post-withdrawal report PDF artifact: $($postWithdrawalReportPdfArtifact.id)"
Write-Host 'QA01 live product-spine smoke passed.'
