param(
    [switch]$SkipDelta
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Net.Http

function Require-EnvValue {
    param(
        [string]$Name
    )

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Required environment variable '$Name' is not set."
    }

    return $value.Trim()
}

function Get-StatusClass {
    param([int]$StatusCode)

    if ($StatusCode -lt 100) {
        return 'unknown'
    }

    return "$([int][Math]::Floor($StatusCode / 100))xx"
}

function Get-SafeTypeName {
    param([object]$Value)

    if ($null -eq $Value) {
        return 'none'
    }

    $typeProperty = $Value.PSObject.Properties['@odata.type']
    if ($null -ne $typeProperty -and -not [string]::IsNullOrWhiteSpace([string]$typeProperty.Value)) {
        return ([string]$typeProperty.Value).TrimStart('#')
    }

    return 'unknown'
}

function Invoke-GraphJson {
    param(
        [string]$Url,
        [string]$AccessToken
    )

    $handler = [System.Net.Http.HttpClientHandler]::new()
    $client = [System.Net.Http.HttpClient]::new($handler)
    $request = $null
    $response = $null

    try {
        $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Get, $Url)
        $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $AccessToken)
        $request.Headers.Accept.ParseAdd('application/json')

        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        $statusCode = [int]$response.StatusCode
        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $body = $null

        if (-not [string]::IsNullOrWhiteSpace($content)) {
            try {
                $body = $content | ConvertFrom-Json
            } catch {
                $body = $null
            }
        }

        return [pscustomobject]@{
            StatusCode = $statusCode
            StatusClass = Get-StatusClass $statusCode
            Body = $body
        }
    } finally {
        if ($null -ne $response) {
            $response.Dispose()
        }
        if ($null -ne $request) {
            $request.Dispose()
        }
        $client.Dispose()
        $handler.Dispose()
    }
}

function Get-GraphValues {
    param([object]$Body)

    if ($null -eq $Body) {
        return @()
    }

    $valueProperty = $Body.PSObject.Properties['value']
    if ($null -eq $valueProperty -or $null -eq $valueProperty.Value) {
        return @()
    }

    return @($valueProperty.Value)
}

function Test-AnyPropertyValue {
    param(
        [object[]]$Items,
        [string]$Name
    )

    foreach ($item in $Items) {
        $property = $item.PSObject.Properties[$Name]
        if ($null -ne $property -and -not [string]::IsNullOrWhiteSpace([string]$property.Value)) {
            return $true
        }
    }

    return $false
}

function Write-EndpointSummary {
    param(
        [string]$Name,
        [object]$Result,
        [object[]]$Items,
        [hashtable]$PropertyPresence = @{}
    )

    $parts = @(
        "$Name",
        "status=$($Result.StatusClass)",
        "count=$($Items.Count)"
    )

    foreach ($key in ($PropertyPresence.Keys | Sort-Object)) {
        $parts += "$key=$($PropertyPresence[$key])"
    }

    Write-Output ($parts -join ' ')
}

$tenantId = Require-EnvValue 'GRAPH_TENANT_ID'
$clientId = Require-EnvValue 'GRAPH_CLIENT_ID'
$clientSecret = Require-EnvValue 'GRAPH_CLIENT_SECRET'

$tokenEndpoint = "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token"
$tokenBody = @{
    client_id = $clientId
    client_secret = $clientSecret
    grant_type = 'client_credentials'
    scope = 'https://graph.microsoft.com/.default'
}

try {
    $tokenResponse = Invoke-RestMethod -Method Post -Uri $tokenEndpoint -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
} catch {
    throw 'Microsoft Graph token request failed. Check tenant id, client id, secret value, application permissions, and admin consent.'
}

$accessToken = [string]$tokenResponse.access_token
if ([string]::IsNullOrWhiteSpace($accessToken)) {
    throw 'Microsoft Graph token response did not include an access token.'
}

$usersUrl = 'https://graph.microsoft.com/v1.0/users?$select=id,displayName,mail,userPrincipalName,department,jobTitle,employeeType,officeLocation,preferredLanguage,accountEnabled,userType&$top=25'
$groupsUrl = 'https://graph.microsoft.com/v1.0/groups?$select=id,displayName,mailEnabled,securityEnabled,groupTypes&$top=25'

Write-Output 'Microsoft Graph directory probe starting.'

$usersResult = Invoke-GraphJson -Url $usersUrl -AccessToken $accessToken
$users = Get-GraphValues $usersResult.Body
Write-EndpointSummary -Name 'users' -Result $usersResult -Items $users -PropertyPresence @{
    hasMail = Test-AnyPropertyValue -Items $users -Name 'mail'
    hasUserPrincipalName = Test-AnyPropertyValue -Items $users -Name 'userPrincipalName'
    hasDepartment = Test-AnyPropertyValue -Items $users -Name 'department'
    hasJobTitle = Test-AnyPropertyValue -Items $users -Name 'jobTitle'
    hasEmployeeType = Test-AnyPropertyValue -Items $users -Name 'employeeType'
    hasOfficeLocation = Test-AnyPropertyValue -Items $users -Name 'officeLocation'
    hasPreferredLanguage = Test-AnyPropertyValue -Items $users -Name 'preferredLanguage'
}

$groupsResult = Invoke-GraphJson -Url $groupsUrl -AccessToken $accessToken
$groups = Get-GraphValues $groupsResult.Body
Write-EndpointSummary -Name 'groups' -Result $groupsResult -Items $groups -PropertyPresence @{
    hasMailEnabled = Test-AnyPropertyValue -Items $groups -Name 'mailEnabled'
    hasSecurityEnabled = Test-AnyPropertyValue -Items $groups -Name 'securityEnabled'
    hasGroupTypes = Test-AnyPropertyValue -Items $groups -Name 'groupTypes'
}

$firstGroup = $groups | Select-Object -First 1
if ($null -ne $firstGroup -and -not [string]::IsNullOrWhiteSpace([string]$firstGroup.id)) {
    $groupMembersUrl = "https://graph.microsoft.com/v1.0/groups/$($firstGroup.id)/members/microsoft.graph.user?`$select=id,displayName,mail,userPrincipalName&`$top=25"
    $groupMembersResult = Invoke-GraphJson -Url $groupMembersUrl -AccessToken $accessToken
    $groupMembers = Get-GraphValues $groupMembersResult.Body
    Write-EndpointSummary -Name 'groupMembers' -Result $groupMembersResult -Items $groupMembers -PropertyPresence @{
        objectType = Get-SafeTypeName ($groupMembers | Select-Object -First 1)
        hasMail = Test-AnyPropertyValue -Items $groupMembers -Name 'mail'
        hasUserPrincipalName = Test-AnyPropertyValue -Items $groupMembers -Name 'userPrincipalName'
    }
} else {
    Write-Output 'groupMembers status=skipped count=0 reason=no_sample_group'
}

$firstUser = $users | Select-Object -First 1
if ($null -ne $firstUser -and -not [string]::IsNullOrWhiteSpace([string]$firstUser.id)) {
    $managerUrl = "https://graph.microsoft.com/v1.0/users/$($firstUser.id)/manager?`$select=id,displayName,mail,userPrincipalName"
    $managerResult = Invoke-GraphJson -Url $managerUrl -AccessToken $accessToken
    $managerItems = @()
    if ($null -ne $managerResult.Body -and $managerResult.StatusCode -ge 200 -and $managerResult.StatusCode -lt 300) {
        $managerItems = @($managerResult.Body)
    }
    Write-EndpointSummary -Name 'manager' -Result $managerResult -Items $managerItems -PropertyPresence @{
        objectType = Get-SafeTypeName $managerResult.Body
        hasMail = Test-AnyPropertyValue -Items $managerItems -Name 'mail'
        hasUserPrincipalName = Test-AnyPropertyValue -Items $managerItems -Name 'userPrincipalName'
    }
} else {
    Write-Output 'manager status=skipped count=0 reason=no_sample_user'
}

if ($SkipDelta) {
    Write-Output 'delta status=skipped count=0 reason=skip_requested'
} else {
    $deltaUrl = 'https://graph.microsoft.com/v1.0/users/delta?$select=id,displayName,mail,userPrincipalName,department,jobTitle&$top=25'
    $deltaResult = Invoke-GraphJson -Url $deltaUrl -AccessToken $accessToken
    $deltaUsers = Get-GraphValues $deltaResult.Body
    Write-EndpointSummary -Name 'delta' -Result $deltaResult -Items $deltaUsers -PropertyPresence @{
        hasDeltaLink = $null -ne $deltaResult.Body -and $null -ne $deltaResult.Body.PSObject.Properties['@odata.deltaLink']
        hasNextLink = $null -ne $deltaResult.Body -and $null -ne $deltaResult.Body.PSObject.Properties['@odata.nextLink']
    }
}

Write-Output 'Microsoft Graph directory probe completed.'
