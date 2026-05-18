param(
    [string]$ApiOrigin = 'https://validatedscale-api-staging.croat.dev',
    [string]$WebOrigin = 'https://validatedscale-staging.croat.dev',
    [string]$TenantId = '11111111-1111-4111-8111-111111111111',
    [string]$SessionCookie,
    [string]$SessionCookiePath,
    [switch]$RequireAuthenticatedSession
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

function Normalize-Origin {
    param([Parameter(Mandatory = $true)][string]$Origin)
    return $Origin.TrimEnd('/')
}

function Get-StatusCode {
    param([Parameter(Mandatory = $true)]$Response)

    return [int]$Response.StatusCode
}

function Get-HeaderValue {
    param(
        [Parameter(Mandatory = $true)]$Response,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($Response -is [System.Net.Http.HttpResponseMessage]) {
        foreach ($header in $Response.Headers) {
            if ($header.Key -eq $Name) {
                return ($header.Value -join ',')
            }
        }

        if ($Response.Content) {
            foreach ($header in $Response.Content.Headers) {
                if ($header.Key -eq $Name) {
                    return ($header.Value -join ',')
                }
            }
        }

        return $null
    }

    return $Response.Headers[$Name]
}

function Invoke-RemoteRequest {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [string]$Method = 'GET',
        [hashtable]$Headers = @{}
    )

    try {
        return Invoke-WebRequest -Uri $Uri -Method $Method -Headers $Headers -UseBasicParsing
    }
    catch {
        if ($_.Exception.Response) {
            return $_.Exception.Response
        }

        throw
    }
}

function Invoke-RemoteRedirectRequest {
    param([Parameter(Mandatory = $true)][string]$Uri)

    $handler = New-Object System.Net.Http.HttpClientHandler
    $handler.AllowAutoRedirect = $false
    $client = New-Object System.Net.Http.HttpClient($handler)

    try {
        return $client.GetAsync($Uri).GetAwaiter().GetResult()
    }
    finally {
        $client.Dispose()
        $handler.Dispose()
    }
}

function Assert-StatusCode {
    param(
        [Parameter(Mandatory = $true)]$Response,
        [Parameter(Mandatory = $true)][int[]]$Expected,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $actual = Get-StatusCode -Response $Response
    if ($Expected -notcontains $actual) {
        throw "$Label expected HTTP $($Expected -join '/') but got $actual."
    }

    Write-Host "$Label=$actual"
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
    }
    elseif ([string]::IsNullOrWhiteSpace($resolved) -and
        ![string]::IsNullOrWhiteSpace($env:STAGING_SESSION_COOKIE)) {
        $resolved = $env:STAGING_SESSION_COOKIE
    }

    if ([string]::IsNullOrWhiteSpace($resolved)) {
        if ($RequireAuthenticatedSession) {
            throw 'Authenticated session proof required but no SessionCookie, SessionCookiePath, or STAGING_SESSION_COOKIE was supplied.'
        }

        return $null
    }

    Write-Host 'Authenticated session cookie source resolved.'
    return $resolved.Trim()
}

$ApiOrigin = Normalize-Origin -Origin $ApiOrigin
$WebOrigin = Normalize-Origin -Origin $WebOrigin
$ResolvedSessionCookie = Resolve-SessionCookie `
    -DirectSessionCookie $SessionCookie `
    -SessionCookiePath $SessionCookiePath `
    -RequireAuthenticatedSession:$RequireAuthenticatedSession

$apiHealthResponse = Invoke-RemoteRequest -Uri "$ApiOrigin/health"
Assert-StatusCode -Response $apiHealthResponse -Expected @(200) -Label 'apiHealth'

$webRootResponse = Invoke-RemoteRequest -Uri $WebOrigin
Assert-StatusCode -Response $webRootResponse -Expected @(200) -Label 'webRoot'

$sessionHeaders = @{
    Origin = $WebOrigin
    'X-Tenant-Id' = $TenantId
}

$unauthenticatedSessionResponse = Invoke-RemoteRequest -Uri "$ApiOrigin/auth/session" -Headers $sessionHeaders
Assert-StatusCode -Response $unauthenticatedSessionResponse -Expected @(401) -Label 'unauthenticatedSession'

$corsHeaders = @{
    Origin = $WebOrigin
    'Access-Control-Request-Method' = 'GET'
    'Access-Control-Request-Headers' = 'x-tenant-id,content-type'
}

$corsResponse = Invoke-RemoteRequest -Uri "$ApiOrigin/auth/session" -Method 'OPTIONS' -Headers $corsHeaders
Assert-StatusCode -Response $corsResponse -Expected @(200, 204) -Label 'authSessionCorsPreflight'

$allowOrigin = Get-HeaderValue -Response $corsResponse -Name 'Access-Control-Allow-Origin'
if ($allowOrigin -ne $WebOrigin) {
    throw "authSessionCorsPreflight expected Access-Control-Allow-Origin '$WebOrigin' but got '$allowOrigin'."
}

$loginReturnUrl = [System.Uri]::EscapeDataString('/app')
$loginRedirectResponse = Invoke-RemoteRedirectRequest -Uri "$ApiOrigin/auth/login?returnUrl=$loginReturnUrl&tenantId=$TenantId"
Assert-StatusCode -Response $loginRedirectResponse -Expected @(302) -Label 'authLoginRedirect'

$location = Get-HeaderValue -Response $loginRedirectResponse -Name 'Location'
if ([string]::IsNullOrWhiteSpace($location)) {
    throw 'authLoginRedirect did not include a Location header.'
}

$redirectUriPattern = [regex]::Escape("redirect_uri=$([System.Uri]::EscapeDataString("$ApiOrigin/auth/callback"))")
if ($location -notmatch $redirectUriPattern) {
    throw "authLoginRedirect Location did not include redirect_uri=$ApiOrigin/auth/callback."
}

if ([string]::IsNullOrWhiteSpace($ResolvedSessionCookie)) {
    Write-Host 'No SessionCookie supplied; authenticated session proof skipped.'
    exit 0
}

$authenticatedHeaders = @{
    Origin = $WebOrigin
    'X-Tenant-Id' = $TenantId
    Cookie = $ResolvedSessionCookie
}

$authenticatedSessionResponse = Invoke-RemoteRequest -Uri "$ApiOrigin/auth/session" -Headers $authenticatedHeaders
Assert-StatusCode -Response $authenticatedSessionResponse -Expected @(200) -Label 'authenticatedSession'

$sessionJson = $authenticatedSessionResponse.Content | ConvertFrom-Json
if ($sessionJson.tenant.id -ne $TenantId) {
    throw "authenticatedSession expected tenant id '$TenantId' but got '$($sessionJson.tenant.id)'."
}

if ($sessionJson.permissions -notcontains 'setup.manage') {
    throw "authenticatedSession expected setup.manage permission."
}

Write-Host 'authenticatedSessionPermission=setup.manage'
