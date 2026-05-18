$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
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

docker compose -f deploy/local/docker-compose.yml up -d

dotnet tool restore

$env:PLATFORM_DESIGN_TIME_CONNECTION = 'Host=localhost;Port=5432;Database=instruments_platform_dev;Username=platform_app;Password=platform_app_dev'
dotnet ef database update --project src/Platform.Infrastructure/Platform.Infrastructure.csproj --startup-project src/Platform.Infrastructure/Platform.Infrastructure.csproj

Get-Content deploy/local/seed-dev.sql |
    docker exec -i local-postgres-1 psql -v ON_ERROR_STOP=1 -U platform_app -d instruments_platform_dev

Write-Host 'Local development database is ready.'
