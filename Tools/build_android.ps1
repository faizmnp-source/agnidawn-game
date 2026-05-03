# AGNIDAWN — Local Android APK Builder
# Builds an Android APK via Unity batch mode and optionally pushes it to a
# connected device via ADB (Samsung Z Fold7 or any connected Android device).
#
# Usage:
#   .\Tools\build_android.ps1                  # build only
#   .\Tools\build_android.ps1 -Deploy          # build + adb install
#   .\Tools\build_android.ps1 -Deploy -Launch  # build + install + launch
#
# Requirements:
#   - Unity 6000.0.35f1 at the default Hub path (or set $UnityExe)
#   - Android SDK / ADB on PATH  (install via Unity Hub > Android Build Support)
#   - Device connected via USB with USB Debugging enabled
#   - Keystore secrets in environment vars:
#       AGNIDAWN_KEYSTORE_PATH, AGNIDAWN_KEYSTORE_PASS,
#       AGNIDAWN_KEYALIAS_NAME, AGNIDAWN_KEYALIAS_PASS
#
# Linear: FAI-17 / FAI-19

param(
    [switch]$Deploy,   # push APK to device via ADB after build
    [switch]$Launch,   # launch the app on device after install (requires -Deploy)
    [switch]$Release   # build Release config (default: Development)
)

$ErrorActionPreference = "Stop"

# ── Config ────────────────────────────────────────────────────────────────────
$UnityVersion   = "6000.4.2f1"
$UnityExe       = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
$ProjectPath    = Resolve-Path "$PSScriptRoot\.."
$BuildDir       = "$ProjectPath\build\Android"
$ApkName        = "Agnidawn.apk"
$ApkPath        = "$BuildDir\$ApkName"
$LogPath        = "$BuildDir\build-android.log"
$BuildMethod    = "AGNIDAWN.Build.AndroidBuilder.Build"  # static method in BuildPipeline script

# Package name must match PlayerSettings.applicationIdentifier
$PackageName    = "com.agnidawn.game"

# ── Locate Unity ─────────────────────────────────────────────────────────────
if (-not (Test-Path $UnityExe)) {
    $candidates = Get-ChildItem "C:\Program Files\Unity\Hub\Editor" -Filter "Unity.exe" -Recurse -ErrorAction SilentlyContinue
    if ($candidates.Count -gt 0) {
        $UnityExe = $candidates[0].FullName
    } else {
        Write-Error "Unity not found. Install Unity $UnityVersion via Unity Hub."
    }
}

# ── Locate ADB ───────────────────────────────────────────────────────────────
$AdbExe = "adb"
try { adb version | Out-Null } catch {
    # Fallback: Android SDK path Unity sets up
    $sdkRoot = [System.Environment]::GetEnvironmentVariable("ANDROID_SDK_ROOT")
    if ($sdkRoot) {
        $AdbExe = "$sdkRoot\platform-tools\adb.exe"
    } else {
        Write-Warning "ADB not found on PATH. -Deploy will be skipped."
        $Deploy = $false
    }
}

# ── Banner ────────────────────────────────────────────────────────────────────
New-Item -ItemType Directory -Path $BuildDir -Force | Out-Null

Write-Host ""
Write-Host "╔══════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   AGNIDAWN Android Builder            ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host "  Project : $ProjectPath"
Write-Host "  Output  : $ApkPath"
Write-Host "  Release : $Release"
Write-Host "  Deploy  : $Deploy"
if ($Deploy) {
    Write-Host "  Launch  : $Launch"
}
Write-Host ""

# ── Keystore env vars ─────────────────────────────────────────────────────────
$keystorePath = $env:AGNIDAWN_KEYSTORE_PATH
$keystorePass = $env:AGNIDAWN_KEYSTORE_PASS
$keyaliasName = $env:AGNIDAWN_KEYALIAS_NAME
$keyaliasPass = $env:AGNIDAWN_KEYALIAS_PASS

if (-not $keystorePath) {
    Write-Warning "AGNIDAWN_KEYSTORE_PATH not set — building with debug keystore."
}

# ── Ensure PROGRAMDATA is set ────────────────────────────────────────────────
# Unity spawns UnityPackageManager.exe (Node.js) which calls:
#   path.join(process.env.PROGRAMDATA, "Unity", "config")
# If PROGRAMDATA is absent (e.g. non-interactive/MCP sessions), UPM crashes
# with exit code 101 before creating its IPC pipe, causing Unity to abort with
# "Could not connect to IPC stream Upm-{PID} after 30s".
if (-not $env:PROGRAMDATA) { $env:PROGRAMDATA = "C:\ProgramData" }

# ── Build args ────────────────────────────────────────────────────────────────
$buildArgs = @(
    "-batchmode",
    "-quit",
    "-projectPath", "`"$ProjectPath`"",
    "-executeMethod", $BuildMethod,
    "-logFile", "`"$LogPath`"",
    "-buildTarget", "Android",
    "-outputPath", "`"$ApkPath`""
)

if ($Release) { $buildArgs += "-release" }
if ($keystorePath) {
    $buildArgs += @("-keystorePath", "`"$keystorePath`"")
    $buildArgs += @("-keystorePass", $keystorePass)
    $buildArgs += @("-keyaliasName", $keyaliasName)
    $buildArgs += @("-keyaliasPass", $keyaliasPass)
}

# ── Run Unity build ───────────────────────────────────────────────────────────
Write-Host "▶ Building Android APK..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $UnityExe -ArgumentList $buildArgs -Wait -PassThru -NoNewWindow

if ($proc.ExitCode -ne 0) {
    Write-Host ""
    Write-Host "✗ Unity build FAILED (exit code $($proc.ExitCode))" -ForegroundColor Red
    Write-Host "  Log: $LogPath"
    # Show last 20 lines of log for quick diagnosis
    if (Test-Path $LogPath) {
        Write-Host ""
        Write-Host "── Last 20 log lines ──────────────────────────────────" -ForegroundColor DarkYellow
        Get-Content $LogPath | Select-Object -Last 20 | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkYellow }
    }
    exit 1
}

if (-not (Test-Path $ApkPath)) {
    Write-Host "✗ Build reported success but APK not found at: $ApkPath" -ForegroundColor Red
    exit 1
}

$apkSize = [math]::Round((Get-Item $ApkPath).Length / 1MB, 2)
Write-Host "✓ Build succeeded: $ApkName ($apkSize MB)" -ForegroundColor Green

# ── Deploy via ADB ────────────────────────────────────────────────────────────
if ($Deploy) {
    Write-Host ""
    Write-Host "▶ Checking connected devices..." -ForegroundColor Yellow
    $devices = & $AdbExe devices | Select-String -Pattern "device$"

    if (-not $devices) {
        Write-Host "✗ No Android device connected. Connect your Z Fold7 with USB Debugging on." -ForegroundColor Red
        exit 1
    }

    Write-Host "  Devices found:"
    $devices | ForEach-Object { Write-Host "    $_" }

    Write-Host ""
    Write-Host "▶ Installing APK on device..." -ForegroundColor Yellow
    & $AdbExe install -r "`"$ApkPath`""

    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ ADB install failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ APK installed" -ForegroundColor Green

    if ($Launch) {
        Write-Host ""
        Write-Host "▶ Launching app on device..." -ForegroundColor Yellow
        & $AdbExe shell am start -n "$PackageName/com.unity3d.player.UnityPlayerActivity"
        Write-Host "✓ App launched" -ForegroundColor Green

        Write-Host ""
        Write-Host "▶ Streaming logcat (Ctrl+C to stop)..." -ForegroundColor DarkGray
        & $AdbExe logcat -s Unity ActivityManager
    }
}

Write-Host ""
Write-Host "✓ Done" -ForegroundColor Green
exit 0
