param(
    [string]$OutFile,
    [string]$ContainerName = "prompttasks-postgres",
    [string]$Database = "prompttasks",
    [string]$Username = "prompttasks"
)

$ErrorActionPreference = "Stop"

function Resolve-OutputPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $Path = Join-Path "backup" ("prompttasks-{0}.dump" -f $timestamp)
    }

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

$resolvedOutFile = Resolve-OutputPath -Path $OutFile
$outDir = Split-Path -Parent $resolvedOutFile
if (-not (Test-Path -LiteralPath $outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

$containerDumpPath = "/tmp/prompttasks.dump"

try {
    Invoke-External `
        -Command "docker" `
        -Arguments @("exec", $ContainerName, "pg_dump", "-U", $Username, "-F", "c", "-d", $Database, "-f", $containerDumpPath) `
        -FailureMessage "Falha ao exportar o banco dentro do container $ContainerName."

    if (Test-Path -LiteralPath $resolvedOutFile) {
        Remove-Item -LiteralPath $resolvedOutFile -Force
    }

    Invoke-External `
        -Command "docker" `
        -Arguments @("cp", "${ContainerName}:$containerDumpPath", $resolvedOutFile) `
        -FailureMessage "Falha ao copiar dump para $resolvedOutFile."
}
finally {
    & docker exec $ContainerName rm -f $containerDumpPath 2>$null | Out-Null
}

Write-Host "Backup exportado: $resolvedOutFile" -ForegroundColor Green
