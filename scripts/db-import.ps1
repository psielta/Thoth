param(
    [Parameter(Mandatory = $true)]
    [string]$DumpFile,
    [string]$DbHost = "localhost",
    [int]$Port = 5435,
    [string]$Superuser = "postgres",
    [string]$SuperuserPassword,
    [string]$AppUser = "prompttasks",
    [string]$AppPassword = "prompttasks",
    [string]$Database = "prompttasks",
    [string]$ServiceName = "PromptTasks"
)

$ErrorActionPreference = "Stop"

function Resolve-InputPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location).Path $Path))
}

function Invoke-External {
    param(
        [string]$Command,
        [string[]]$Arguments,
        [string]$FailureMessage
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }
}

function Invoke-Psql {
    param([string]$Sql)

    Invoke-External `
        -Command "psql" `
        -Arguments @("-h", $DbHost, "-p", [string]$Port, "-U", $Superuser, "-d", "postgres", "-v", "ON_ERROR_STOP=1", "-c", $Sql) `
        -FailureMessage "Falha ao executar SQL no PostgreSQL."
}

function Stop-ServiceIfPresent {
    param(
        [string]$Name,
        [ref]$WasRunning
    )

    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $service) {
        $WasRunning.Value = $false
        return
    }

    $WasRunning.Value = ($service.Status -ne 'Stopped')
    if ($service.Status -eq 'Stopped') {
        return
    }

    Stop-Service -Name $Name -Force
    $service.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(45))
}

function Start-ServiceIfNeeded {
    param(
        [string]$Name,
        [bool]$ShouldStart
    )

    if (-not $ShouldStart) {
        return
    }

    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if ($service -and $service.Status -eq 'Stopped') {
        Start-Service -Name $Name
    }
}

$resolvedDumpFile = Resolve-InputPath -Path $DumpFile
if (-not (Test-Path -LiteralPath $resolvedDumpFile)) {
    throw "Dump nao encontrado: $resolvedDumpFile"
}

$bootstrap = Join-Path $PSScriptRoot "db-bootstrap.ps1"
$bootstrapArgs = @{
    DbHost = $DbHost
    Port = $Port
    Superuser = $Superuser
    AppUser = $AppUser
    AppPassword = $AppPassword
    Database = $Database
}
if (-not [string]::IsNullOrEmpty($SuperuserPassword)) {
    $bootstrapArgs.SuperuserPassword = $SuperuserPassword
}
& $bootstrap @bootstrapArgs

$previousPgPassword = $env:PGPASSWORD
if (-not [string]::IsNullOrEmpty($SuperuserPassword)) {
    $env:PGPASSWORD = $SuperuserPassword
}

$serviceWasRunning = $false
try {
    Stop-ServiceIfPresent -Name $ServiceName -WasRunning ([ref]$serviceWasRunning)

    $escapedDatabase = $Database -replace "'", "''"
    Invoke-Psql -Sql "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$escapedDatabase' AND pid <> pg_backend_pid();"

    Invoke-External `
        -Command "pg_restore" `
        -Arguments @("-h", $DbHost, "-p", [string]$Port, "-U", $Superuser, "-d", $Database, "--clean", "--if-exists", "--no-owner", "--role=$AppUser", $resolvedDumpFile) `
        -FailureMessage "Falha ao importar dump $resolvedDumpFile."
}
finally {
    if ($null -eq $previousPgPassword) {
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    } else {
        $env:PGPASSWORD = $previousPgPassword
    }

    Start-ServiceIfNeeded -Name $ServiceName -ShouldStart $serviceWasRunning
}

Write-Host "Backup importado em $DbHost`:$Port/$Database" -ForegroundColor Green
