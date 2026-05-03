# AGNIDAWN — Local Test Runner
# Runs all EditMode tests via Unity batch mode and prints a summary.
# Usage:  .\Tools\run_tests.ps1 [-Mode editmode|playmode|all] [-Verbose]
#
# Requirements:
#   - Unity 6000.0.35f1 (or set $UnityExe to your path)
#   - Project at one level up from Tools\ folder (i.e. this repo root)
#
# Linear: FAI-17

param(
    [ValidateSet("editmode","playmode","all")]
    [string]$Mode    = "editmode",
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# ── Locate Unity ───────────────────────────────────────────────────────────────
$UnityVersion = "6000.0.35f1"
$UnityExe = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"

if (-not (Test-Path $UnityExe)) {
    # Fallback: scan Unity Hub default install dirs
    $candidates = Get-ChildItem "C:\Program Files\Unity\Hub\Editor" -Filter "Unity.exe" -Recurse -ErrorAction SilentlyContinue
    if ($candidates.Count -gt 0) {
        $UnityExe = $candidates[0].FullName
        Write-Host "[run_tests] Using Unity at: $UnityExe" -ForegroundColor DarkGray
    } else {
        Write-Error "Unity not found. Set `$UnityExe` manually in this script."
    }
}

# ── Paths ─────────────────────────────────────────────────────────────────────
$ProjectPath  = Resolve-Path "$PSScriptRoot\.."
$ResultsPath  = "$ProjectPath\test-results"
$LogPath      = "$ResultsPath\unity-test-run.log"

New-Item -ItemType Directory -Path $ResultsPath -Force | Out-Null

Write-Host ""
Write-Host "╔══════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   AGNIDAWN Test Runner                ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host "  Project : $ProjectPath"
Write-Host "  Mode    : $Mode"
Write-Host "  Log     : $LogPath"
Write-Host ""

# ── Run a test mode ────────────────────────────────────────────────────────────
function Run-TestMode {
    param([string]$TestMode)

    $ResultXml = "$ResultsPath\${TestMode}-results.xml"
    Write-Host "▶ Running $TestMode tests..." -ForegroundColor Yellow

    $args = @(
        "-runTests",
        "-batchmode",
        "-projectPath", "`"$ProjectPath`"",
        "-testMode", $TestMode,
        "-testResults", "`"$ResultXml`"",
        "-logFile", "`"$LogPath`""
    )

    $proc = Start-Process -FilePath $UnityExe -ArgumentList $args -Wait -PassThru -NoNewWindow
    return $proc.ExitCode
}

# ── Run selected modes ────────────────────────────────────────────────────────
$exitCodes = @{}

if ($Mode -eq "editmode" -or $Mode -eq "all") {
    $exitCodes["editmode"] = Run-TestMode "editmode"
}
if ($Mode -eq "playmode" -or $Mode -eq "all") {
    $exitCodes["playmode"] = Run-TestMode "playmode"
}

# ── Parse XML results ─────────────────────────────────────────────────────────
Write-Host ""
Write-Host "── Results ──────────────────────────────────────────────" -ForegroundColor Cyan

$totalPassed = 0
$totalFailed = 0
$totalSkipped = 0

foreach ($m in $exitCodes.Keys) {
    $xml = "$ResultsPath\${m}-results.xml"
    if (Test-Path $xml) {
        [xml]$doc  = Get-Content $xml
        $suite     = $doc.'test-run'

        $passed    = [int]($suite.passed   ?? 0)
        $failed    = [int]($suite.failed   ?? 0)
        $skipped   = [int]($suite.skipped  ?? 0)
        $total     = [int]($suite.total    ?? 0)
        $duration  = [float]($suite.duration ?? 0)

        $totalPassed  += $passed
        $totalFailed  += $failed
        $totalSkipped += $skipped

        $color = if ($failed -gt 0) { "Red" } else { "Green" }
        Write-Host ("  {0,-12}  {1} passed  {2} failed  {3} skipped  ({4:F2}s)" -f `
            $m.ToUpper(), $passed, $failed, $skipped, $duration) -ForegroundColor $color

        if ($Verbose -and $failed -gt 0) {
            $doc.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
                Write-Host "    ✗ $($_.name)" -ForegroundColor Red
                Write-Host "      $($_.failure.message)" -ForegroundColor DarkRed
            }
        }
    } else {
        Write-Host "  $m — no results XML found (Unity may have crashed)" -ForegroundColor DarkYellow
    }
}

Write-Host ""
Write-Host "── Summary ──────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "  Total Passed : $totalPassed" -ForegroundColor Green
if ($totalFailed -gt 0) {
    Write-Host "  Total Failed : $totalFailed" -ForegroundColor Red
} else {
    Write-Host "  Total Failed : 0" -ForegroundColor Green
}
Write-Host "  Total Skipped: $totalSkipped"
Write-Host ""

# ── Exit code ─────────────────────────────────────────────────────────────────
$anyFailed = ($exitCodes.Values | Where-Object { $_ -ne 0 }).Count -gt 0

if ($anyFailed -or $totalFailed -gt 0) {
    Write-Host "✗ Tests FAILED" -ForegroundColor Red
    exit 1
} else {
    Write-Host "✓ All tests passed" -ForegroundColor Green
    exit 0
}
