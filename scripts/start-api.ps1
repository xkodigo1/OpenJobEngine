$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$dataDir = Join-Path $repoRoot "data"
$sqlitePath = Join-Path $dataDir "openjobengine.db"

if (-not (Test-Path $dataDir)) {
    New-Item -ItemType Directory -Path $dataDir | Out-Null
}

if (-not $env:Persistence__Provider) {
    $env:Persistence__Provider = "Sqlite"
}

if (-not $env:ConnectionStrings__Sqlite) {
    $env:ConnectionStrings__Sqlite = "Data Source=$sqlitePath"
}

Write-Host "Starting OpenJobEngine API"
Write-Host "Persistence provider: $env:Persistence__Provider"
Write-Host "SQLite database: $sqlitePath"

dotnet run --project (Join-Path $repoRoot "src\OpenJobEngine.Api")
