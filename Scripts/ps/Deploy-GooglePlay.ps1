<#
.SYNOPSIS
    Builds, signs, and packages Khayrat Alhaj for Google Play Store deployment.

.DESCRIPTION
    This script:
    1. Generates a release keystore if one doesn't exist yet
    2. Builds the .NET MAUI Android project in Release mode
    3. Produces a signed AAB (Android App Bundle) ready for Google Play Console upload

.PARAMETER KeystorePath
    Path to the .keystore file. Default: .\zadalhaj-release.keystore

.PARAMETER KeystorePassword
    Password for the keystore. Will prompt if not provided.

.PARAMETER KeyAlias
    Alias name for the signing key. Default: zadalhaj

.PARAMETER KeyPassword
    Password for the key alias. Will prompt if not provided.

.PARAMETER VersionCode
    Integer version code (must increment on each Play Store upload). Default: 1

.PARAMETER VersionName
    Display version string (e.g., "1.0.0"). Default: 1.0

.PARAMETER SkipKeystoreGen
    Skip keystore generation even if file doesn't exist.

.EXAMPLE
    .\Deploy-GooglePlay.ps1 -VersionCode 1 -VersionName "1.0"

.EXAMPLE
    .\Deploy-GooglePlay.ps1 -KeystorePath "C:\keys\zadalhaj.keystore" -KeystorePassword "mypass" -KeyAlias "zadalhaj" -KeyPassword "mypass" -VersionCode 2 -VersionName "1.1"
#>
[CmdletBinding()]
param(
    [string]$KeystorePath = ".\zadalhaj-release.keystore",
    [string]$KeystorePassword,
    [string]$KeyAlias = "zadalhaj",
    [string]$KeyPassword,
    [int]$VersionCode = 1,
    [string]$VersionName = "1.0",
    [switch]$SkipKeystoreGen
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Get the workspace root (two levels up: Scripts/ps -> Scripts -> root)
$WorkspaceRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$ProjectPath = Join-Path $WorkspaceRoot "KhayratAlhaj\KhayratAlhaj.csproj"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Khayrat Alhaj - Google Play Deployment"   -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ── Step 0: Validate prerequisites ──────────────────────────────────
Write-Host "[Step 0] Checking prerequisites..." -ForegroundColor Yellow

if (-not (Test-Path $ProjectPath)) {
    Write-Error "Project file not found at: $ProjectPath"
    exit 1
}

# Verify dotnet is available
$dotnetVersion = & dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Error ".NET SDK not found. Install from https://dot.net"
    exit 1
}
Write-Host "  .NET SDK: $dotnetVersion" -ForegroundColor Gray

# Verify keytool (from JDK) is available
$keytoolPath = Get-Command keytool -ErrorAction SilentlyContinue
if (-not $keytoolPath) {
    # Try common JDK paths
    $jdkPaths = @(
        "$env:JAVA_HOME\bin\keytool.exe",
        "$env:ProgramFiles\Microsoft\jdk-*\bin\keytool.exe",
        "$env:ProgramFiles\Java\jdk-*\bin\keytool.exe"
    )
    foreach ($p in $jdkPaths) {
        $resolved = Resolve-Path $p -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($resolved) {
            $keytoolPath = $resolved.Path
            break
        }
    }
    if (-not $keytoolPath) {
        Write-Warning "keytool not found in PATH. Keystore generation may fail."
        Write-Warning "Ensure JAVA_HOME is set or JDK is installed."
    }
}

# ── Step 1: Keystore generation ─────────────────────────────────────
$KeystorePath = [System.IO.Path]::GetFullPath($KeystorePath)

if (-not (Test-Path $KeystorePath)) {
    if ($SkipKeystoreGen) {
        Write-Error "Keystore not found at: $KeystorePath (and -SkipKeystoreGen was set)"
        exit 1
    }

    Write-Host ""
    Write-Host "[Step 1] Generating release keystore..." -ForegroundColor Yellow
    Write-Host "  Path: $KeystorePath" -ForegroundColor Gray

    # Prompt for passwords if not provided
    if (-not $KeystorePassword) {
        $secPass = Read-Host "Enter keystore password (min 6 chars)" -AsSecureString
        $KeystorePassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secPass))
    }
    if (-not $KeyPassword) {
        $secKey = Read-Host "Enter key password (min 6 chars)" -AsSecureString
        $KeyPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secKey))
    }

    # Prompt for certificate details
    $cn = Read-Host "Your full name (CN)"
    $ou = Read-Host "Organization unit (OU) [press Enter to skip]"
    $o  = Read-Host "Organization (O) [press Enter to skip]"
    $l  = Read-Host "City (L) [press Enter to skip]"
    $st = Read-Host "State/Province (ST) [press Enter to skip]"
    $c  = Read-Host "Country code (C, e.g., MA)"

    $dname = "CN=$cn"
    if ($ou) { $dname += ", OU=$ou" }
    if ($o)  { $dname += ", O=$o" }
    if ($l)  { $dname += ", L=$l" }
    if ($st) { $dname += ", ST=$st" }
    if ($c)  { $dname += ", C=$c" }

    $keytoolArgs = @(
        "-genkeypair",
        "-v",
        "-keystore", $KeystorePath,
        "-alias", $KeyAlias,
        "-keyalg", "RSA",
        "-keysize", "2048",
        "-validity", "10000",
        "-storepass", $KeystorePassword,
        "-keypass", $KeyPassword,
        "-dname", $dname
    )

    & keytool @keytoolArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "keytool failed. Ensure JDK is installed and keytool is in PATH."
        exit 1
    }

    Write-Host ""
    Write-Host "  Keystore created successfully!" -ForegroundColor Green
    Write-Host "  IMPORTANT: Back up this file securely. If you lose it," -ForegroundColor Red
    Write-Host "  you can NEVER update your app on Google Play." -ForegroundColor Red
    Write-Host "  DO NOT commit this file to Git." -ForegroundColor Red
    Write-Host ""
} else {
    Write-Host "[Step 1] Using existing keystore: $KeystorePath" -ForegroundColor Yellow

    # Still need passwords
    if (-not $KeystorePassword) {
        $secPass = Read-Host "Enter keystore password" -AsSecureString
        $KeystorePassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secPass))
    }
    if (-not $KeyPassword) {
        $secKey = Read-Host "Enter key password" -AsSecureString
        $KeyPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secKey))
    }
}

# ── Step 2: Clean previous build ────────────────────────────────────
Write-Host "[Step 2] Cleaning previous build artifacts..." -ForegroundColor Yellow
& dotnet clean $ProjectPath -f net10.0-android -c Release --verbosity quiet 2>$null
Write-Host "  Done." -ForegroundColor Gray

# ── Step 3: Build & publish signed AAB ──────────────────────────────
Write-Host ""
Write-Host "[Step 3] Building Release AAB with NativeAOT + R8 obfuscation..." -ForegroundColor Yellow
Write-Host "  Version: $VersionName (code: $VersionCode)" -ForegroundColor Gray
Write-Host "  This may take several minutes..." -ForegroundColor Gray
Write-Host ""

$publishArgs = @(
    "publish",
    $ProjectPath,
    "-f", "net10.0-android",
    "-c", "Release",
    "-p:AndroidKeyStore=true",
    "-p:AndroidSigningKeyStore=$KeystorePath",
    "-p:AndroidSigningKeyAlias=$KeyAlias",
    "-p:AndroidSigningKeyPass=$KeyPassword",
    "-p:AndroidSigningStorePass=$KeystorePassword",
    "-p:ApplicationVersion=$VersionCode",
    "-p:ApplicationDisplayVersion=$VersionName"
)

& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Error "Build FAILED. Check errors above."
    exit 1
}

# ── Step 4: Locate output AAB ───────────────────────────────────────
Write-Host ""
Write-Host "[Step 4] Locating output AAB..." -ForegroundColor Yellow

$publishDir = Join-Path $WorkspaceRoot "KhayratAlhaj\bin\Release\net10.0-android\publish"
$aabFile = Get-ChildItem -Path $publishDir -Filter "*.aab" -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $aabFile) {
    # Also check non-publish directory
    $altDir = Join-Path $WorkspaceRoot "KhayratAlhaj\bin\Release\net10.0-android"
    $aabFile = Get-ChildItem -Path $altDir -Filter "*-Signed.aab" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
}

if (-not $aabFile) {
    # Final fallback: search for any .aab
    $aabFile = Get-ChildItem -Path (Join-Path $WorkspaceRoot "KhayratAlhaj\bin\Release") -Filter "*.aab" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
}

if ($aabFile) {
    $aabSize = [math]::Round($aabFile.Length / 1MB, 2)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Output: $($aabFile.FullName)" -ForegroundColor White
    Write-Host "  Size:   $aabSize MB" -ForegroundColor White
    Write-Host "  Version: $VersionName (code: $VersionCode)" -ForegroundColor White
    Write-Host ""
    Write-Host "  Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Go to https://play.google.com/console" -ForegroundColor Gray
    Write-Host "  2. Select your app -> Release -> Production" -ForegroundColor Gray
    Write-Host "  3. Create new release -> Upload this AAB file" -ForegroundColor Gray
    Write-Host "  4. Add release notes -> Review and publish" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Warning "AAB file not found in output directory."
    Write-Warning "Check: $publishDir"
    Write-Host "Build may have produced an APK instead. Check the directory manually." -ForegroundColor Yellow
}
