param(
    [string]$Version,
    [ValidateSet('none','patch','minor','major')]
    [string]$Bump = 'none',
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$versionFile = Join-Path $repoRoot "version.json"
$stageDir = Join-Path $repoRoot "build\publish"
$publishDir = Join-Path $stageDir "PromptTasks"
$distDir = Join-Path $repoRoot "dist"

function Assert-InRepoPath {
    param(
        [string]$Path,
        [string]$Description
    )

    $repoFullPath = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\')
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not ($fullPath -eq $repoFullPath -or $fullPath.StartsWith($repoFullPath + '\', [StringComparison]::OrdinalIgnoreCase))) {
        throw "$Description fora do repositorio: $fullPath"
    }
}

function Clear-Directory {
    param([string]$Path)

    Assert-InRepoPath -Path $Path -Description "Diretorio de build"
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

function Test-SemVer {
    param([string]$Value)

    return $Value -match '^\d+\.\d+\.\d+$'
}

function Read-Version {
    if (-not (Test-Path -LiteralPath $versionFile)) {
        throw "Arquivo version.json nao encontrado: $versionFile"
    }

    $json = Get-Content -LiteralPath $versionFile -Raw | ConvertFrom-Json
    $value = [string]$json.version
    if (-not (Test-SemVer -Value $value)) {
        throw "Versao invalida em version.json: $value"
    }

    return $value
}

function Write-Version {
    param([string]$Value)

    if (-not (Test-SemVer -Value $Value)) {
        throw "Versao invalida: $Value"
    }

    $payload = [ordered]@{ version = $Value } | ConvertTo-Json
    Set-Content -LiteralPath $versionFile -Value $payload -Encoding UTF8
}

function Bump-Version {
    param(
        [string]$Value,
        [string]$Kind
    )

    $parts = $Value.Split('.') | ForEach-Object { [int]$_ }
    switch ($Kind) {
        'major' {
            $parts[0]++
            $parts[1] = 0
            $parts[2] = 0
        }
        'minor' {
            $parts[1]++
            $parts[2] = 0
        }
        'patch' {
            $parts[2]++
        }
        default {
            throw "Bump invalido: $Kind"
        }
    }

    return "{0}.{1}.{2}" -f $parts[0], $parts[1], $parts[2]
}

function Resolve-Version {
    if (-not [string]::IsNullOrWhiteSpace($Version) -and $Bump -ne 'none') {
        throw "Use -Version ou -Bump, nao ambos."
    }

    if (-not [string]::IsNullOrWhiteSpace($Version)) {
        if (-not (Test-SemVer -Value $Version)) {
            throw "Versao invalida: $Version. Use SemVer X.Y.Z."
        }

        return $Version
    }

    $current = Read-Version
    if ($Bump -ne 'none') {
        $next = Bump-Version -Value $current -Kind $Bump
        Write-Version -Value $next
        return $next
    }

    return $current
}

function Get-SourceRevision {
    try {
        $commit = (& git -C $repoRoot rev-parse --short=12 HEAD 2>$null)
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($commit)) {
            return $commit.Trim()
        }
    } catch {
        # Build continues without git metadata.
    }

    return "unknown"
}

function Invoke-Checked {
    param(
        [string]$Executable,
        [string[]]$ArgumentList,
        [string]$FailureMessage
    )

    & $Executable @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }
}

function Resolve-NpmExecutable {
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        $npmCmd = (Get-Command npm.cmd -ErrorAction SilentlyContinue | Select-Object -First 1).Source
        if ($npmCmd) {
            return $npmCmd
        }
    }

    return "npm"
}

function Find-Iscc {
    $candidates = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 5\ISCC.exe",
        "C:\Program Files\Inno Setup 5\ISCC.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Inno Setup nao encontrado. Instale com: choco install innosetup -y"
}

function Publish-Frontend {
    $frontendRoot = Join-Path $repoRoot "frontend"
    $frontendDist = Join-Path $frontendRoot "dist"
    $previousApiBaseUrl = $env:VITE_API_BASE_URL

    Push-Location $frontendRoot
    try {
        $env:VITE_API_BASE_URL = "/api"
        $npmExecutable = Resolve-NpmExecutable
        Invoke-Checked -Executable $npmExecutable -ArgumentList @("ci") -FailureMessage "Falha no npm ci do frontend"
        Invoke-Checked -Executable $npmExecutable -ArgumentList @("run", "build") -FailureMessage "Falha no npm run build do frontend"
    }
    finally {
        if ($null -eq $previousApiBaseUrl) {
            Remove-Item Env:\VITE_API_BASE_URL -ErrorAction SilentlyContinue
        } else {
            $env:VITE_API_BASE_URL = $previousApiBaseUrl
        }
        Pop-Location
    }

    $indexHtml = Join-Path $frontendDist "index.html"
    if (-not (Test-Path -LiteralPath $indexHtml)) {
        throw "Build do frontend nao gerou index.html: $indexHtml"
    }
}

function Publish-Backend {
    param(
        [string]$ResolvedVersion,
        [string]$Revision
    )

    $csproj = Join-Path $repoRoot "backend\src\PromptTasks.Api\PromptTasks.Api.csproj"
    if (-not (Test-Path -LiteralPath $csproj)) {
        throw "Csproj nao encontrado: $csproj"
    }

    $publishArgs = @(
        "publish",
        $csproj,
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained",
        "-o", $publishDir,
        "-p:Version=$ResolvedVersion",
        "-p:InformationalVersion=$ResolvedVersion+$Revision",
        "-p:SourceRevisionId=$Revision"
    )

    Invoke-Checked -Executable "dotnet" -ArgumentList $publishArgs -FailureMessage "Falha no dotnet publish do backend"
}

function Copy-FrontendToPublish {
    $frontendDist = Join-Path $repoRoot "frontend\dist"
    $wwwroot = Join-Path $publishDir "wwwroot"

    if (-not (Test-Path -LiteralPath $frontendDist)) {
        throw "Dist do frontend nao encontrado: $frontendDist"
    }

    New-Item -ItemType Directory -Force -Path $wwwroot | Out-Null
    Copy-Item -Path (Join-Path $frontendDist "*") -Destination $wwwroot -Recurse -Force
}

function Compile-Iss {
    param(
        [string]$ResolvedVersion,
        [string]$IsccPath
    )

    $iss = Join-Path $repoRoot "setup\PromptTasks.iss"
    if (-not (Test-Path -LiteralPath $iss)) {
        throw "Script Inno Setup nao encontrado: $iss"
    }

    & $IsccPath `
        "/DMyAppVersion=$ResolvedVersion" `
        "/DMyStageDir=$stageDir" `
        "/DMyOutputDir=$distDir" `
        $iss

    if ($LASTEXITCODE -ne 0) {
        throw "Falha na compilacao do instalador."
    }
}

$resolvedVersion = Resolve-Version
$sourceRevision = Get-SourceRevision

Write-Host "Version:  $resolvedVersion" -ForegroundColor DarkGray
Write-Host "Revision: $sourceRevision" -ForegroundColor DarkGray

Clear-Directory -Path $stageDir
Clear-Directory -Path $distDir

if (-not $SkipPublish) {
    Publish-Frontend
    Publish-Backend -ResolvedVersion $resolvedVersion -Revision $sourceRevision
    Copy-FrontendToPublish
}

$iscc = Find-Iscc
Write-Host "Inno Setup: $iscc" -ForegroundColor DarkGray
Compile-Iss -ResolvedVersion $resolvedVersion -IsccPath $iscc

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "Setup gerado em: $distDir" -ForegroundColor Green
Get-ChildItem -LiteralPath $distDir -Filter "*.exe" | ForEach-Object {
    Write-Host "  - $($_.Name) ($([math]::Round($_.Length / 1MB, 2)) MB)" -ForegroundColor Green
}
Write-Host "========================================" -ForegroundColor Green
