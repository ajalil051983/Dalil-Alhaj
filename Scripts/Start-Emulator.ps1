# Start Android Emulator
# This script starts the specified Android Virtual Device (AVD)

Write-Host "Setting up Android SDK environment..." -ForegroundColor Green

$env:ANDROID_HOME = "C:\Program Files (x86)\Android\android-sdk"
$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-11.0.16.101-hotspot"

# Add paths if they don't exist
if ($env:Path -notlike "*$env:ANDROID_HOME\platform-tools*") {
    $env:Path += ";$env:ANDROID_HOME\platform-tools"
}
if ($env:Path -notlike "*$env:ANDROID_HOME\emulator*") {
    $env:Path += ";$env:ANDROID_HOME\emulator"
}

$AVD_NAME = "pixel_7_-_api_36_0"

Write-Host "ANDROID_HOME: $env:ANDROID_HOME" -ForegroundColor Cyan
Write-Host "AVD Name: $AVD_NAME" -ForegroundColor Cyan

# Check if emulator is available
$emulatorPath = Join-Path $env:ANDROID_HOME "emulator\emulator.exe"
if (-not (Test-Path $emulatorPath)) {
    Write-Host "ERROR: Emulator not found at: $emulatorPath" -ForegroundColor Red
    exit 1
}

# Check if AVD exists
Write-Host "`nChecking available AVDs..." -ForegroundColor Yellow
& $emulatorPath -list-avds

# Check if any emulator is already running
Write-Host "`nChecking for running emulators..." -ForegroundColor Yellow
adb devices

Write-Host "`nStarting emulator: $AVD_NAME" -ForegroundColor Green
Write-Host "This will open the emulator window..." -ForegroundColor Yellow
Write-Host "=" * 80 -ForegroundColor DarkGray

# Start the emulator
& $emulatorPath -avd $AVD_NAME -no-snapshot-load
