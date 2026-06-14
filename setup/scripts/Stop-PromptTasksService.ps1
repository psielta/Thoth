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

function Stop-ProcessByName {
    param([string]$Name)

    $processes = Get-Process -Name $Name -ErrorAction SilentlyContinue
    if (-not $processes) {
        Write-SetupLog "Processo $Name nao encontrado."
        return
    }

    foreach ($process in $processes) {
        Write-SetupLog "Encerrando processo $Name (PID $($process.Id))."
        Stop-Process -Id $process.Id -Force
    }
}

Write-SetupLog "Inicio da preparacao do setup do Thoth."
# Versoes antigas rodavam como servico LocalSystem; versoes novas rodam como processos
# do usuario (Thoth.Desktop inicia Thoth.Api). Encerra ambos para liberar os arquivos.
Stop-ServiceByName -Name "PromptTasks"
Stop-ProcessByName -Name "Thoth.Desktop"
Stop-ProcessByName -Name "Thoth.Api"
Write-SetupLog "Preparacao do setup do Thoth concluida."
exit 0
