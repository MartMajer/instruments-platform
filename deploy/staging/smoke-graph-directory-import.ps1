param(
    [string]$ApiOrigin = $env:GRAPH_DIRECTORY_IMPORT_API_ORIGIN,
    [string]$SessionCookie = $env:STAGING_SESSION_COOKIE,
    [string]$SessionCookiePath = '',
    [string]$MicrosoftTenantId = $env:GRAPH_TENANT_ID,
    [string]$MicrosoftDisplayName = $env:GRAPH_DIRECTORY_DISPLAY_NAME,
    [string]$MicrosoftPrimaryDomain = $env:GRAPH_DIRECTORY_PRIMARY_DOMAIN,
    [string]$RuleName = 'Sandbox Microsoft Graph directory import',
    [string]$Departments = '',
    [string]$GroupIds = '',
    [switch]$IncludeManagerLinks,
    [switch]$MirrorMode,
    [string]$MirrorConfirmation = '',
    [int]$MinimumMatchedUsers = 1,
    [int]$MinimumSubjectCountIncrease = 1,
    [switch]$PreviewOnly,
    [string]$EvidencePath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$safePreviewSummary = $null
$safeApplySummary = $null
$safeBeforeDirectorySummary = $null
$safeAfterDirectorySummary = $null
$safeDirectoryDelta = $null

function Resolve-RequiredText {
    param(
        [string]$Name,
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "$Name is required."
    }

    return $Value.Trim()
}

function Resolve-SessionCookie {
    if (-not [string]::IsNullOrWhiteSpace($SessionCookie) -and
        -not [string]::IsNullOrWhiteSpace($SessionCookiePath)) {
        throw 'SessionCookie and SessionCookiePath cannot both be supplied.'
    }

    if (-not [string]::IsNullOrWhiteSpace($SessionCookiePath)) {
        if (-not (Test-Path -LiteralPath $SessionCookiePath)) {
            throw 'SessionCookiePath does not exist.'
        }

        return (Get-Content -Raw -LiteralPath $SessionCookiePath).Trim()
    }

    return $SessionCookie.Trim()
}

function Join-ApiUri {
    param([string]$Path)

    $origin = Resolve-RequiredText -Name 'GRAPH_DIRECTORY_IMPORT_API_ORIGIN' -Value $ApiOrigin
    return $origin.TrimEnd('/') + $Path
}

function Invoke-GraphDirectorySmokeJson {
    param(
        [ValidateSet('GET', 'POST')]
        [string]$Method,
        [string]$Path,
        [object]$Body = $null
    )

    $headers = @{
        Cookie = $script:resolvedSessionCookie
    }
    $request = @{
        Method = $Method
        Uri = Join-ApiUri -Path $Path
        Headers = $headers
    }

    if ($null -ne $Body) {
        $request['ContentType'] = 'application/json'
        $request['Body'] = ($Body | ConvertTo-Json -Depth 8)
    }

    Invoke-RestMethod @request
}

function Split-SmokeList {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return @()
    }

    return @($Value -split '[,\n]' | ForEach-Object { $_.Trim() } | Where-Object {
        -not [string]::IsNullOrWhiteSpace($_)
    })
}

function Get-OrCreateConnection {
    param([object]$Workspace)

    $connection = @($Workspace.connections | Where-Object {
        $_.externalTenantId -eq $script:resolvedMicrosoftTenantId
    } | Select-Object -First 1)
    if ($connection.Count -gt 0) {
        return $connection[0]
    }

    $body = [ordered]@{
        externalTenantId = $script:resolvedMicrosoftTenantId
        displayName = $script:resolvedMicrosoftDisplayName
        primaryDomain = $script:resolvedMicrosoftPrimaryDomain
        grantedScopes = @(
            'User.Read.All',
            'Group.Read.All',
            'GroupMember.Read.All'
        )
    }

    Invoke-GraphDirectorySmokeJson -Method POST -Path '/directory-connections' -Body $body
}

function Get-OrCreateRule {
    param(
        [object]$Workspace,
        [object]$Connection
    )

    $rule = @($Workspace.rules | Where-Object {
        $_.connectionId -eq $Connection.id -and $_.name -eq $RuleName
    } | Select-Object -First 1)
    if ($rule.Count -gt 0) {
        return $rule[0]
    }

    $criteria = [ordered]@{
        accountEnabled = $true
        excludeGuests = $true
    }
    $departmentList = Split-SmokeList -Value $Departments
    if ($departmentList.Count -gt 0) {
        $criteria['departments'] = @($departmentList)
    }

    $groupIdList = Split-SmokeList -Value $GroupIds
    if ($groupIdList.Count -gt 0) {
        $criteria['groupIds'] = @($groupIdList)
    }

    if ($IncludeManagerLinks) {
        $criteria['includeManagerChain'] = $true
    }

    $body = [ordered]@{
        connectionId = $Connection.id
        name = $RuleName
        criteria = $criteria
        fieldSelection = [ordered]@{
            fields = @(
                'displayName',
                'mail',
                'userPrincipalName',
                'department',
                'jobTitle',
                'employeeType',
                'officeLocation',
                'preferredLanguage'
            )
        }
        mirrorMode = [bool]$MirrorMode
        mirrorConfirmation = if ($MirrorMode) { $MirrorConfirmation } else { $null }
    }

    Invoke-GraphDirectorySmokeJson -Method POST -Path '/directory-import-rules' -Body $body
}

function Assert-PreviewSummary {
    param([object]$Preview)

    if ($Preview.status -ne 'previewed') {
        throw 'Directory import preview did not finish with previewed status.'
    }

    $summary = [ordered]@{
        matchedUserCount = [int]$Preview.summary.matchedUserCount
        createSubjectCount = [int]$Preview.summary.createSubjectCount
        updateSubjectCount = [int]$Preview.summary.updateSubjectCount
        noChangeCount = [int]$Preview.summary.noChangeCount
        warningCount = [int]$Preview.summary.warningCount
    }

    if ($summary.matchedUserCount -lt $MinimumMatchedUsers) {
        throw "Directory import preview matched fewer users than expected minimum $MinimumMatchedUsers."
    }

    return $summary
}

function Assert-ApplySummary {
    param([object]$Apply)

    if ($Apply.status -ne 'applied') {
        throw 'Directory import apply did not finish with applied status.'
    }

    [ordered]@{
        createdSubjectCount = [int]$Apply.summary.createdSubjectCount
        updatedSubjectCount = [int]$Apply.summary.updatedSubjectCount
        noChangeSubjectCount = [int]$Apply.summary.noChangeSubjectCount
        createdGroupCount = [int]$Apply.summary.createdGroupCount
        addedMembershipCount = [int]$Apply.summary.addedMembershipCount
        setManagerCount = [int]$Apply.summary.setManagerCount
        warningCount = [int]$Apply.summary.warningCount
    }
}

function Get-SafeDirectorySummary {
    param([object]$Directory)

    if ($null -eq $Directory -or $null -eq $Directory.summary) {
        throw 'Directory response did not include a summary.'
    }

    [ordered]@{
        subjectCount = [int]$Directory.summary.subjectCount
        groupCount = [int]$Directory.summary.groupCount
        managerRelationshipCount = [int]$Directory.summary.managerRelationshipCount
    }
}

function Assert-DirectoryCountDelta {
    param(
        [object]$Before,
        [object]$After
    )

    if ($MinimumSubjectCountIncrease -lt 0) {
        throw 'MinimumSubjectCountIncrease cannot be negative.'
    }

    $subjectCountIncrease = [int]$After.subjectCount - [int]$Before.subjectCount
    $groupCountIncrease = [int]$After.groupCount - [int]$Before.groupCount
    $managerRelationshipCountIncrease =
        [int]$After.managerRelationshipCount - [int]$Before.managerRelationshipCount

    if ($subjectCountIncrease -lt $MinimumSubjectCountIncrease) {
        throw "Directory subject count increased by $subjectCountIncrease, below expected minimum $MinimumSubjectCountIncrease."
    }

    [ordered]@{
        subjectCountIncrease = $subjectCountIncrease
        groupCountIncrease = $groupCountIncrease
        managerRelationshipCountIncrease = $managerRelationshipCountIncrease
    }
}

function Write-SmokeEvidence {
    if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
        return
    }

    $directory = Split-Path -Path $EvidencePath -Parent
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $evidence = [ordered]@{
        schemaVersion = 1
        generatedAt = [DateTimeOffset]::UtcNow.ToString('o')
        status = 'passed'
        apiOriginConfigured = -not [string]::IsNullOrWhiteSpace($ApiOrigin)
        microsoftTenantIdConfigured = -not [string]::IsNullOrWhiteSpace($MicrosoftTenantId)
        microsoftPrimaryDomainConfigured = -not [string]::IsNullOrWhiteSpace($MicrosoftPrimaryDomain)
        previewOnly = [bool]$PreviewOnly
        safePreviewSummary = $script:safePreviewSummary
        safeApplySummary = $script:safeApplySummary
        safeBeforeDirectorySummary = $script:safeBeforeDirectorySummary
        safeAfterDirectorySummary = $script:safeAfterDirectorySummary
        safeDirectoryDelta = $script:safeDirectoryDelta
    }

    Set-Content -LiteralPath $EvidencePath -Value ($evidence | ConvertTo-Json -Depth 6) -Encoding utf8
}

$script:resolvedSessionCookie = Resolve-RequiredText -Name 'STAGING_SESSION_COOKIE or SessionCookiePath' -Value (Resolve-SessionCookie)
$script:resolvedMicrosoftTenantId = Resolve-RequiredText -Name 'GRAPH_TENANT_ID' -Value $MicrosoftTenantId
$script:resolvedMicrosoftDisplayName = if ([string]::IsNullOrWhiteSpace($MicrosoftDisplayName)) {
    'Microsoft Graph sandbox'
}
else {
    $MicrosoftDisplayName.Trim()
}
$script:resolvedMicrosoftPrimaryDomain = Resolve-RequiredText -Name 'GRAPH_DIRECTORY_PRIMARY_DOMAIN' -Value $MicrosoftPrimaryDomain

$workspace = Invoke-GraphDirectorySmokeJson -Method GET -Path '/directory-imports/workspace'
$connection = Get-OrCreateConnection -Workspace $workspace
$workspace = Invoke-GraphDirectorySmokeJson -Method GET -Path '/directory-imports/workspace'
$rule = Get-OrCreateRule -Workspace $workspace -Connection $connection
$beforeDirectory = Invoke-GraphDirectorySmokeJson -Method GET -Path '/subjects'
$safeBeforeDirectorySummary = Get-SafeDirectorySummary -Directory $beforeDirectory

$preview = Invoke-GraphDirectorySmokeJson -Method POST -Path "/directory-import-rules/$($rule.id)/preview"
$safePreviewSummary = Assert-PreviewSummary -Preview $preview

Write-Host "Graph directory import preview matched $($safePreviewSummary.matchedUserCount) users."
Write-Host "Preview actions: create=$($safePreviewSummary.createSubjectCount), update=$($safePreviewSummary.updateSubjectCount), noChange=$($safePreviewSummary.noChangeCount), warnings=$($safePreviewSummary.warningCount)."

if (-not $PreviewOnly) {
    $apply = Invoke-GraphDirectorySmokeJson -Method POST -Path "/directory-import-runs/$($preview.runId)/apply"
    $safeApplySummary = Assert-ApplySummary -Apply $apply
    $afterDirectory = Invoke-GraphDirectorySmokeJson -Method GET -Path '/subjects'
    $safeAfterDirectorySummary = Get-SafeDirectorySummary -Directory $afterDirectory
    $safeDirectoryDelta = Assert-DirectoryCountDelta `
        -Before $safeBeforeDirectorySummary `
        -After $safeAfterDirectorySummary

    Write-Host "Graph directory import apply completed."
    Write-Host "Apply actions: created=$($safeApplySummary.createdSubjectCount), updated=$($safeApplySummary.updatedSubjectCount), memberships=$($safeApplySummary.addedMembershipCount), managers=$($safeApplySummary.setManagerCount), warnings=$($safeApplySummary.warningCount)."
    Write-Host "Directory subjects: before=$($safeBeforeDirectorySummary.subjectCount), after=$($safeAfterDirectorySummary.subjectCount), increase=$($safeDirectoryDelta.subjectCountIncrease)."
}

Write-SmokeEvidence
