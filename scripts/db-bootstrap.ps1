param(
    [string]$DbHost = "localhost",
    [int]$Port = 5435,
    [string]$Superuser = "postgres",
    [string]$SuperuserPassword,
    [string]$AppUser = "prompttasks",
    [string]$AppPassword = "prompttasks",
    [string]$Database = "prompttasks"
)

$ErrorActionPreference = "Stop"

function Assert-SafeIdentifier {
    param(
        [string]$Value,
        [string]$Name
    )

    if ($Value -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
        throw "$Name invalido para SQL identifier: $Value"
    }
}

function Quote-Identifier {
    param([string]$Value)
    Assert-SafeIdentifier -Value $Value -Name "Identificador"
    return '"' + $Value + '"'
}

function Quote-Literal {
    param([string]$Value)
    return "'" + ($Value -replace "'", "''") + "'"
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
    param(
        [string]$Sql,
        [string]$TargetDatabase = "postgres"
    )

    Invoke-External `
        -Command "psql" `
        -Arguments @("-h", $DbHost, "-p", [string]$Port, "-U", $Superuser, "-d", $TargetDatabase, "-v", "ON_ERROR_STOP=1", "-c", $Sql) `
        -FailureMessage "Falha ao executar SQL no banco $TargetDatabase."
}

function Invoke-PsqlScalar {
    param(
        [string]$Sql,
        [string]$TargetDatabase = "postgres"
    )

    $output = & psql -h $DbHost -p ([string]$Port) -U $Superuser -d $TargetDatabase -v ON_ERROR_STOP=1 -tAc $Sql 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao consultar PostgreSQL: $output"
    }

    return ($output | Out-String).Trim()
}

Assert-SafeIdentifier -Value $AppUser -Name "AppUser"
Assert-SafeIdentifier -Value $Database -Name "Database"

$previousPgPassword = $env:PGPASSWORD
if (-not [string]::IsNullOrEmpty($SuperuserPassword)) {
    $env:PGPASSWORD = $SuperuserPassword
}

try {
    $roleIdentifier = Quote-Identifier -Value $AppUser
    $roleLiteral = Quote-Literal -Value $AppUser
    $passwordLiteral = Quote-Literal -Value $AppPassword
    $databaseIdentifier = Quote-Identifier -Value $Database
    $databaseLiteral = Quote-Literal -Value $Database

    $roleSql = "DO `$`$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = $roleLiteral) THEN CREATE ROLE $roleIdentifier LOGIN PASSWORD $passwordLiteral; ELSE ALTER ROLE $roleIdentifier WITH LOGIN PASSWORD $passwordLiteral; END IF; END `$`$;"
    Invoke-Psql -Sql $roleSql

    $databaseExists = Invoke-PsqlScalar -Sql "SELECT 1 FROM pg_database WHERE datname = $databaseLiteral"
    if ($databaseExists -ne "1") {
        Invoke-External `
            -Command "createdb" `
            -Arguments @("-h", $DbHost, "-p", [string]$Port, "-U", $Superuser, "-O", $AppUser, $Database) `
            -FailureMessage "Falha ao criar banco $Database."
    }

    Invoke-Psql -Sql "ALTER DATABASE $databaseIdentifier OWNER TO $roleIdentifier;"
}
finally {
    if ($null -eq $previousPgPassword) {
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    } else {
        $env:PGPASSWORD = $previousPgPassword
    }
}

Write-Host "Role e banco garantidos: $AppUser / $Database" -ForegroundColor Green
