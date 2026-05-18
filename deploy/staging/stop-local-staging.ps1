param(
    [switch]$RemoveVolumes
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$composeFile = Join-Path $repoRoot 'deploy\staging\docker-compose.yml'
$envFile = Join-Path $repoRoot 'deploy\staging\.env'

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
    throw 'deploy/staging/.env does not exist. Nothing to stop for the local staging stack.'
}

if ($RemoveVolumes) {
    docker compose --env-file $envFile -f $composeFile down --volumes
} else {
    docker compose --env-file $envFile -f $composeFile down
}
