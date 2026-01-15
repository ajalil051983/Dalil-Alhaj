$env:ANDROID_HOME = "C:\Program Files (x86)\Android\android-sdk"
$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-11.0.16.101-hotspot"

# Add paths if they don't exist
if ($env:Path -notlike "*$env:ANDROID_HOME\platform-tools*") {
    $env:Path += ";$env:ANDROID_HOME\platform-tools"
}
if ($env:Path -notlike "*$env:JAVA_HOME\bin*") {
    $env:Path += ";$env:JAVA_HOME\bin"
}

Write-Host "Environment Configured:"
Write-Host "ANDROID_HOME: $env:ANDROID_HOME"
Write-Host "JAVA_HOME:    $env:JAVA_HOME"

# Verify adb availability
if (Get-Command adb -ErrorAction SilentlyContinue) {
    Write-Host "ADB found: $(Get-Command adb | Select-Object -ExpandProperty Source)"
    
    # Warm up ADB
    Write-Host "Starting ADB server..."
    adb start-server

    Write-Host "Starting Appium..."
    appium --allow-cors
} else {
    Write-Error "ADB could not be found. Please check paths."
}
