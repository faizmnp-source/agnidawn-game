# AGNIDAWN — Unity Editor Log Parser
# Reads Unity's live Editor.log after project opens and reports compile errors
# Call this after opening Unity to get error report autonomously
# Usage: .\Tools\check_errors.ps1

$logPath = "$env:LOCALAPPDATA\Unity\Editor\Editor.log"

if (-not (Test-Path $logPath)) {
    Write-Host "Unity Editor.log not found. Open Unity first." -ForegroundColor Yellow
    exit 0
}

$log = Get-Content $logPath -Raw

# Extract CS compile errors
$errors   = ($log -split "`n") | Where-Object { $_ -match "error CS\d+|): error" }
$warnings = ($log -split "`n") | Where-Object { $_ -match "warning CS\d+" }
$safeMode = $log -match "Safe Mode"

Write-Host "=== AGNIDAWN Compile Status ===" -ForegroundColor Cyan
Write-Host "Safe Mode: $safeMode"
Write-Host "Errors: $($errors.Count)  |  Warnings: $($warnings.Count)"

if ($errors.Count -gt 0) {
    Write-Host "`nERRORS:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
} else {
    Write-Host "No compile errors detected." -ForegroundColor Green
    exit 0
}
