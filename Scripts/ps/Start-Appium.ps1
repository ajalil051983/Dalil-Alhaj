# Use 8.3 short paths to avoid Windows 'spawn EINVAL' error caused by
# parentheses in 'C:\Program Files (x86)' when spawned as a child process by Node.js
$env:ANDROID_HOME = "C:\PROGRA~2\Android\ANDROI~1"
$env:ANDROID_SDK_ROOT = "C:\PROGRA~2\Android\ANDROI~1"
$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-11.0.16.101-hotspot"

# Add paths if they don't exist
if ($env:Path -notlike "*$env:ANDROID_HOME\platform-tools*") {
    $env:Path += ";$env:ANDROID_HOME\platform-tools"
}
if ($env:Path -notlike "*$env:ANDROID_HOME\emulator*") {
    $env:Path += ";$env:ANDROID_HOME\emulator"
}
if ($env:Path -notlike "*$env:JAVA_HOME\bin*") {
    $env:Path += ";$env:JAVA_HOME\bin"
}

Write-Host "Environment Configured:"
Write-Host "ANDROID_HOME:     $env:ANDROID_HOME"
Write-Host "ANDROID_SDK_ROOT: $env:ANDROID_SDK_ROOT"
Write-Host "JAVA_HOME:        $env:JAVA_HOME"

# Verify adb availability
if (Get-Command adb -ErrorAction SilentlyContinue) {
    Write-Host "ADB found: $(Get-Command adb | Select-Object -ExpandProperty Source)"
    
    # Warm up ADB
    Write-Host "Starting ADB server..."
    adb start-server

    # Appium 3.x: invoke via 'node index.js' directly to avoid spawn issues
    # with the appium wrapper script on Windows.
    $appiumIndex = "$env:APPDATA\fnm\node-versions\v22.15.1\installation\node_modules\appium\index.js"
    if (Test-Path $appiumIndex) {
        Write-Host "Starting Appium 3.x via node..."
        node $appiumIndex --allow-cors
    } else {
        Write-Host "Falling back to 'appium' shim..."
        appium --allow-cors
    }
} else {
    Write-Error "ADB could not be found. Please check paths."
}
