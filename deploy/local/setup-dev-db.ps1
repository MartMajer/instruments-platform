$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location $repoRoot

function Invoke-Compose {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,
        [Parameter(ValueFromPipeline)]
        [string]$PipelineInput
    )

    begin {
        $stdinLines = [System.Collections.Generic.List[string]]::new()
    }

    process {
        if ($PSBoundParameters.ContainsKey('PipelineInput')) {
            $stdinLines.Add($PipelineInput)
        }
    }

    end {
        if (-not $script:ComposeCommand) {
            throw 'docker compose command has not been configured.'
        }

        $command = [string[]]$script:ComposeCommand
        if ($command.Count -eq 0 -or $command.Count -gt 2) {
            throw "Unexpected compose command shape: $($command -join ' ')"
        }

        $composeArgs = if ($command.Count -eq 2) {
            @($command[0], $command[1]) + $Arguments
        } else {
            @($command[0]) + $Arguments
        }

        if ($stdinLines.Count -eq 0) {
            if ($composeArgs.Count -gt 1) {
                & $composeArgs[0] @($composeArgs[1..($composeArgs.Count - 1)])
            } else {
                & $composeArgs[0]
            }
            return
        }

        $psi = [System.Diagnostics.ProcessStartInfo]::new($composeArgs[0])
        $psi.UseShellExecute = $false
        $psi.RedirectStandardInput = $true
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true

        if ($composeArgs.Count -gt 1) {
            foreach ($arg in $composeArgs[1..($composeArgs.Count - 1)]) {
                $psi.ArgumentList.Add($arg)
            }
        }

        $proc = [System.Diagnostics.Process]::new()
        $proc.StartInfo = $psi
        [void]$proc.Start()

        $stdinWriter = $proc.StandardInput
        foreach ($line in $stdinLines) {
            $stdinWriter.WriteLine($line)
        }
        $stdinWriter.Close()

        $stdout = $proc.StandardOutput.ReadToEnd()
        $stderr = $proc.StandardError.ReadToEnd()
        $proc.WaitForExit()

        if ($stdout) {
            Write-Output $stdout
        }

        if ($proc.ExitCode -ne 0) {
            if ($stderr) {
                throw $stderr
            }

            throw "compose command failed with exit code $($proc.ExitCode)."
        }

        if ($stderr) {
            Write-Error -ErrorAction Continue $stderr
        }
    }
}

function Resolve-DockerComposeCommand {
    if (Test-ComposePlugin) {
        return [string[]]@('docker', 'compose')
    }

    $legacyCandidates = @('/usr/local/bin/docker-compose', '/usr/bin/docker-compose')
    $pathCandidates = Get-Command -All docker-compose -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source | Where-Object {
        $_ -ne '/usr/bin/docker-compose' -and
        $_ -ne '/usr/local/bin/docker-compose' -and
        $_ -notlike '*/tools/frameworks/bin/*'
    }

    if ($pathCandidates) {
        $legacyCandidates = @($legacyCandidates + $pathCandidates)
    }

    foreach ($candidatePath in $legacyCandidates) {
        if (Test-Path $candidatePath) {
            try {
                if (Test-ComposeCommand -Arguments @($candidatePath)) {
                    return [string[]]@($candidatePath)
                }
            } catch {
                # Continue to next candidate.
            }
        }
    }

    return $null
}

function Test-ComposePlugin {
    try {
        return Test-ComposeCommand -Arguments @('docker', 'compose')
    } catch {
        return $false
    }
}

function Test-ComposeCommand {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $commandPath = $Arguments[0]
    $tmpDir = $env:TMP
    if ([string]::IsNullOrWhiteSpace($tmpDir)) {
        $tmpDir = '/tmp'
    }

    if ($Arguments.Count -eq 2) {
        $versionOutput = & $Arguments[0] $Arguments[1] --version 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) {
            return $false
        }
    } else {
        $versionOutput = & $commandPath --version 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) {
            return $false
        }
    }

    if ([string]::IsNullOrWhiteSpace($versionOutput.Trim())) {
        return $false
    }

    $probePath = Join-Path $tmpDir ("dc-probe-" + [guid]::NewGuid().ToString('N') + '.yml')
    if ($Arguments.Count -eq 2) {
        $probeOutput = & $Arguments[0] $Arguments[1] -f $probePath config --services 2>&1 | Out-String
    } else {
        $probeOutput = & $commandPath -f $probePath config --services 2>&1 | Out-String
    }
    if ($probeOutput -match "unknown shorthand flag:\s*'f'") {
        return $false
    }

    return $true
}

function Get-PrefixedComposeCommand {
    param(
        [Parameter(Mandatory)]
        [bool]$HasPlugin
    )

    if ($HasPlugin -and (Test-ComposeCommand -Arguments @('docker', 'compose'))) {
        return [string[]]@('docker', 'compose')
    }

    $legacy = Resolve-DockerComposeCommand
    if (-not $legacy) {
        throw 'Neither `docker compose` nor `docker-compose` was found. Install Docker Compose and retry.'
    }

    return [string[]]$legacy
}

function Wait-PostgresReady {
    param(
        [Parameter(Mandatory)]
        [string]$ComposeFile
    )

    $dbUp = $false
    for ($i = 0; $i -lt 120; $i++) {
        try {
            if ((Invoke-Compose -Arguments @('-f', $ComposeFile, 'exec', '-T', 'postgres', 'pg_isready', '-U', 'platform_app', '-d', 'instruments_platform_dev') | Out-String) -match 'accepting connections') {
                $dbUp = $true
                break
            }
        } catch {
            # Compose service may not be ready yet.
        }

        Start-Sleep -Milliseconds 500
    }

    if (-not $dbUp) {
        throw 'Postgres did not become ready in time on localhost:5432.'
    }
}

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

$composeWithPlugin = $false
try {
    $composeWithPlugin = Test-ComposePlugin
} catch {
    $composeWithPlugin = $false
}

$script:ComposeCommand = [string[]](Get-PrefixedComposeCommand -HasPlugin $composeWithPlugin)

Invoke-Compose -Arguments @('-f', 'deploy/local/docker-compose.yml', 'up', '-d', 'postgres')

Wait-PostgresReady -ComposeFile 'deploy/local/docker-compose.yml'

dotnet tool restore

$env:PLATFORM_DESIGN_TIME_CONNECTION = 'Host=localhost;Port=5432;Database=instruments_platform_dev;Username=platform_app;Password=platform_app_dev'
dotnet ef database update --project src/Platform.Infrastructure/Platform.Infrastructure.csproj --startup-project src/Platform.Infrastructure/Platform.Infrastructure.csproj

Get-Content deploy/local/seed-dev.sql |
    Invoke-Compose -Arguments @('-f', 'deploy/local/docker-compose.yml', 'exec', '-T', 'postgres', 'psql', '-v', 'ON_ERROR_STOP=1', '-U', 'platform_app', '-d', 'instruments_platform_dev')

# The API/Workers dev connection strings use non-superuser roles so RLS is
# actually enforced locally (the compose POSTGRES_USER is a superuser, which
# silently bypasses every tenant-isolation policy). Same script as staging.
Get-Content deploy/staging/runtime-role.sql |
    Invoke-Compose -Arguments @('-f', 'deploy/local/docker-compose.yml', 'exec', '-T', 'postgres', 'psql', '-v', 'ON_ERROR_STOP=1', '-v', 'runtime_user=platform_app_runtime', '-v', 'runtime_password=platform_app_runtime_dev', '-v', 'worker_user=platform_app_worker', '-v', 'worker_password=platform_app_worker_dev', '-U', 'platform_app', '-d', 'instruments_platform_dev')

Write-Host 'Local development database is ready.'
