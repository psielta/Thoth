param(
    [int]$TimeoutSeconds = 45
)

$ErrorActionPreference = "SilentlyContinue"

$logFile = Join-Path $env:TEMP "PromptTasks-Setup-StopService.log"

function Write-SetupLog {
    param([string]$Message)

    $line = "[{0:yyyy-MM-dd HH:mm:ss}] {1}" -f (Get-Date), $Message
    Add-Content -Path $logFile -Value $line
}

function Stop-ServiceByName {
    param([string]$Name)

    $service = Get-CimInstance Win32_Service -Filter "Name='$Name'"
    if (-not $service) {
        Write-SetupLog "Servico $Name nao encontrado."
        return
    }

    Write-SetupLog "Parando servico $Name."
    Stop-Service -Name $Name -Force

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        Start-Sleep -Milliseconds 500
        $service = Get-CimInstance Win32_Service -Filter "Name='$Name'"
        if (-not $service -or $service.State -eq "Stopped") {
            Write-SetupLog "Servico $Name parado."
            return
        }
    } while ((Get-Date) -lt $deadline)

    $service = Get-CimInstance Win32_Service -Filter "Name='$Name'"
    if ($service -and $service.ProcessId -gt 0) {
        Write-SetupLog "Encerrando processo do servico $Name (PID $($service.ProcessId))."
        Stop-Process -Id $service.ProcessId -Force
    }
}

Write-SetupLog "Inicio da preparacao do setup do Thoth."
Stop-ServiceByName -Name "PromptTasks"
Write-SetupLog "Preparacao do setup do Thoth concluida."
exit 0
