$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$composeFile = Join-Path $repoRoot 'deploy\staging\docker-compose.yml'
$envFile = Join-Path $repoRoot 'deploy\staging\.env'
$envExample = Join-Path $repoRoot 'deploy\staging\env.example'

Set-Location $repoRoot

$docker = Get-Command docker -ErrorAction SilentlyContinue
if (-not $docker) {
    $dockerBin = 'C:\Program Files\Docker\Docker\resources\bin'
    if (Test-Path (Join-Path $dockerBin 'docker.exe')) {
        $env:PATH = "$dockerBin;$env:PATH"
    }

    $docker = Get-Command docker -ErrorAction SilentlyContinue
}

if (-not $docker) {
    throw 'docker.exe was not found. Start a new shell after installing Docker Desktop, or add Docker resources\bin to PATH.'
}

if (-not (Test-Path $envFile)) {
    Copy-Item -LiteralPath $envExample -Destination $envFile
    Write-Host "Created deploy/staging/.env from env.example."
}

docker compose --env-file $envFile -f $composeFile up -d --build

Write-Host 'Local staging stack is starting.'
Write-Host 'Run deploy/staging/smoke-local-staging.ps1 to verify it.'
