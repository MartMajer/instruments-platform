param(
    [switch]$SkipDb
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

if (-not $SkipDb) {
    Write-Host "Starting local DB and applying migrations..."
    powershell -ExecutionPolicy Bypass -File "$repoRoot\deploy\local\setup-dev-db.ps1"
}

$originalDotnetCliHome = $env:DOTNET_CLI_HOME
$originalHome = $env:HOME
$originalSkipFirstTime = $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE
$env:DOTNET_CLI_HOME = Join-Path $repoRoot ".dotnet-home"
$env:HOME = Join-Path $repoRoot ".home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME, $env:HOME | Out-Null

$originalAspNetCoreEnv = $env:ASPNETCORE_ENVIRONMENT
$originalPublicDevAuth = $env:PUBLIC_DEV_AUTH_ENABLED
$originalPublicApiUrl = $env:PUBLIC_API_BASE_URL

$shell = Join-Path $PSHOME "pwsh.exe"
if (-not (Test-Path $shell)) {
    $shell = Join-Path $PSHOME "powershell.exe"
}

if (-not (Test-Path "$repoRoot\apps\web\node_modules\.bin\vite.cmd")) {
    Write-Host "Frontend dependencies missing (vite not found). Running npm install in apps/web..."
    & npm.cmd install --prefix "$repoRoot\apps\web"
}

Write-Host "Starting backend (http profile on 5055)..."
$env:ASPNETCORE_ENVIRONMENT = "Development"
Start-Process -FilePath $shell -ArgumentList "-NoProfile", "-NoExit", "-Command", "dotnet run --project src/Platform.Api --launch-profile http" -WorkingDirectory $repoRoot | Out-Null

Start-Sleep -Seconds 2

Write-Host "Starting frontend on 5173..."
$env:PUBLIC_DEV_AUTH_ENABLED = "true"
$env:PUBLIC_API_BASE_URL = "http://127.0.0.1:5055"

$viteJs = Join-Path $repoRoot "apps\web\node_modules\vite\bin\vite.js"
if (-not (Test-Path $viteJs)) {
    throw "Vite binary missing at $viteJs. Run: npm.cmd install --prefix `"$repoRoot\apps\web`""
}

Start-Process -FilePath "node" -ArgumentList $viteJs, "dev", "--host", "127.0.0.1" -WorkingDirectory "$repoRoot\apps\web" | Out-Null

Write-Host "Backend + frontend should be starting."
Write-Host " - API: http://127.0.0.1:5055"
Write-Host " - Frontend: http://127.0.0.1:5173"

if ($originalDotnetCliHome -ne $null) {
    $env:DOTNET_CLI_HOME = $originalDotnetCliHome
} else {
    Remove-Item Env:\DOTNET_CLI_HOME -ErrorAction SilentlyContinue
}

if ($originalHome -ne $null) {
    $env:HOME = $originalHome
} else {
    Remove-Item Env:\HOME -ErrorAction SilentlyContinue
}

if ($originalSkipFirstTime -ne $null) {
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = $originalSkipFirstTime
} else {
    Remove-Item Env:\DOTNET_SKIP_FIRST_TIME_EXPERIENCE -ErrorAction SilentlyContinue
}

if ($originalAspNetCoreEnv -ne $null) {
    $env:ASPNETCORE_ENVIRONMENT = $originalAspNetCoreEnv
} else {
    Remove-Item Env:\ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
}

if ($originalPublicDevAuth -ne $null) {
    $env:PUBLIC_DEV_AUTH_ENABLED = $originalPublicDevAuth
} else {
    Remove-Item Env:\PUBLIC_DEV_AUTH_ENABLED -ErrorAction SilentlyContinue
}

if ($originalPublicApiUrl -ne $null) {
    $env:PUBLIC_API_BASE_URL = $originalPublicApiUrl
} else {
    Remove-Item Env:\PUBLIC_API_BASE_URL -ErrorAction SilentlyContinue
}
