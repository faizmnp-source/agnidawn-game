# AGNIDAWN — Unity Batch Compile Checker
# Runs Unity in headless mode, parses log for errors/warnings
# Usage: .\Tools\compile_check.ps1

param(
    [string]$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.4.2f1\Editor\Unity.exe",
    [string]$ProjectPath = "C:\Users\Micro\30 Minute\30 Minutes\agnidawn-game",
    [string]$LogFile = "C:\Users\Micro\30 Minute\30 Minutes\agnidawn-game\Tools\compile_log.txt"
)

Write-Host "=== AGNIDAWN Compile Check ===" -ForegroundColor Cyan
Write-Host "Unity: $UnityExe"
Write-Host "Project: $ProjectPath"

# Run Unity in batch mode — compile only, then quit
$args = @(
    "-batchmode",
    "-projectPath", $ProjectPath,
    "-logFile", $LogFile,
    "-quit"
)

Write-Host "Running Unity batch compile..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $UnityExe -ArgumentList $args -Wait -PassThru
Write-Host "Unity exited with code: $($proc.ExitCode)"

if (-not (Test-Path $LogFile)) {
    Write-Host "ERROR: No log file generated." -ForegroundColor Red
    exit 1
}

$log = Get-Content $LogFile -Raw

# Parse errors
$errorLines   = $log -split "`n" | Where-Object { $_ -match "error CS|Error|compile error" }
$warningLines = $log -split "`n" | Where-Object { $_ -match "warning CS" }

Write-Host "`n=== ERRORS ($($errorLines.Count)) ===" -ForegroundColor Red
if ($errorLines.Count -eq 0) {
    Write-Host "No compile errors!" -ForegroundColor Green
} else {
    $errorLines | ForEach-Object { Write-Host $_ -ForegroundColor Red }
}

Write-Host "`n=== WARNINGS ($($warningLines.Count)) ===" -ForegroundColor Yellow
$warningLines | Select-Object -First 10 | ForEach-Object { Write-Host $_ -ForegroundColor Yellow }

# Return exit code based on errors
if ($errorLines.Count -gt 0) { exit 1 } else { exit 0 }
