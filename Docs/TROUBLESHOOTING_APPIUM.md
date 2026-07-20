# Appium Troubleshooting Guide

All issues listed here have been **resolved**. This document records what was found and how it was fixed so the same problems can be diagnosed quickly if they recur.

---

## Issue 1: `spawn EINVAL` on Appium startup ✅ Fixed

### Symptom
```
[Appium] spawn EINVAL
```
Appium crashes immediately after printing compatibility warnings — no port is opened.

### Root Cause
Node.js on Windows cannot spawn child processes when the parent's environment contains paths with parentheses (e.g. `C:\Program Files (x86)\...`). The `ANDROID_HOME` variable was set to `C:\Program Files (x86)\Android\android-sdk`, which triggered this bug.

### Fix
Use Windows 8.3 short paths that remove the parentheses:
```powershell
$env:ANDROID_HOME    = "C:\PROGRA~2\Android\ANDROI~1"
$env:ANDROID_SDK_ROOT = "C:\PROGRA~2\Android\ANDROI~1"
```
`Start-Appium.ps1` has been updated to use these short paths.

Additionally, invoke Appium directly via `node index.js` instead of the `appium` wrapper script:
```powershell
node "$env:APPDATA\fnm\node-versions\v22.15.1\installation\node_modules\appium\index.js" --allow-cors
```

---

## Issue 2: Appium driver/plugin incompatibility warnings ✅ Fixed

### Symptom
```
WARN Driver "uiautomator2" may be incompatible with the current version of Appium (v2.0.1)
WARN Plugin "images" may be incompatible ...
WARN Plugin "relaxed-caps" may be incompatible ...
```

### Root Cause
Appium server was at v2.0.1 while the installed drivers/plugins required Appium 3.x.

### Fix
Upgraded all components globally:
```powershell
npm install -g appium@latest                             # → 3.2.0
appium driver update uiautomator2                        # → 6.9.3
appium plugin update images                              # → 4.1.0
appium plugin update relaxed-caps                        # → 2.0.2
```

---

## Issue 3: App crashes with "No assemblies found" (MAUI Fast Deployment) ✅ Fixed

### Symptom
```
Abort message: 'No assemblies found in '/data/user/0/com.companyname.khayratalhaj/files/.__override__/x86_64'
or '<unavailable>'. Assuming this is part of Fast Deployment. Exiting...'
```

### Root Cause
Debug builds use MAUI Fast Deployment — assemblies are pushed separately by Visual Studio. When Appium installs the APK directly (without VS running), the assemblies are missing and the app crashes.

### Fix
Build and test with the **Release** APK, which embeds all assemblies:
```powershell
dotnet build KhayratAlhaj/KhayratAlhaj.csproj -f net10.0-android -c Release
```
`AppiumSetup.cs` points to the Release APK path.

---

## Issue 4: `APK file not found` — wrong package name in path ✅ Fixed

### Symptom
```
APK file not found at: ...\com.companyname.khayratalhaj-Signed.apk
```

### Root Cause
`AppiumSetup.cs` was using the placeholder package ID `com.companyname.khayratalhaj`. The actual `ApplicationId` in `KhayratAlhaj.csproj` is `com.ilafalkhayr.zadalhaj`.

### Fix
Updated `AppiumSetup.cs`:
```csharp
// Before
"com.companyname.khayratalhaj-Signed.apk"
driverOptions.AddAdditionalAppiumOption("appPackage", "com.companyname.khayratalhaj");

// After
"com.ilafalkhayr.zadalhaj-Signed.apk"
driverOptions.AddAdditionalAppiumOption("appPackage", "com.ilafalkhayr.zadalhaj");
```

---

## Issue 5: `Activity class does not exist` ✅ Fixed

### Symptom
```
Error type 3
Error: Activity class {com.ilafalkhayr.zadalhaj/crc64c1fe6d0abd199a4b.MainActivity} does not exist.
```

### Root Cause
The `appActivity` hash in `AppiumSetup.cs` (`crc64c1fe6d0abd199a4b`) did not match the hash compiled into the installed APK.

### Diagnosis
Find the real activity hash from the installed package:
```powershell
adb -s emulator-5554 shell pm dump com.ilafalkhayr.zadalhaj | Select-String "Activity"
# Output: crc640e514d85339b6ec1.MainActivity
```

### Fix
Updated `AppiumSetup.cs`:
```csharp
// Before
driverOptions.AddAdditionalAppiumOption("appActivity", "crc64c1fe6d0abd199a4b.MainActivity");

// After
driverOptions.AddAdditionalAppiumOption("appActivity", "crc640e514d85339b6ec1.MainActivity");
```

> **Note:** This hash is derived from the assembly/namespace and can change if the project
> is renamed or refactored. Re-run the `pm dump` command above to get the current value.

---

## Issue 6: `NoSuchElementException` in all 6 resource-id based tests ✅ Fixed

### Symptom
```
NoSuchElementException: An element could not be located on the page using the given search parameters.
```
All tests using `By.Id(Pkg + "...")` failed.

### Root Cause
The `Pkg` constant in every test file was set to `"com.companyname.KhayratAlhaj:id/"`. Android resource IDs include the package name, so the correct prefix must match the installed package.

### Fix
Updated all 6 test files:
```csharp
// Before (in MapTests, ChecklistTests, FavoritesTests, SearchTests, SubCategoryTests, SettingsTests)
private const string Pkg = "com.companyname.KhayratAlhaj:id/";

// After
private const string Pkg = "com.ilafalkhayr.zadalhaj:id/";
```

---

## Quick Diagnostics Cheatsheet

```powershell
# Is Appium running?
Invoke-WebRequest http://127.0.0.1:4723/status -UseBasicParsing

# Is the emulator connected?
adb devices

# Is the app installed?
adb shell pm list packages | Select-String "khayratalhaj"

# What is the real MainActivity hash?
adb shell pm dump com.ilafalkhayr.zadalhaj | Select-String "Activity"

# Clear app data (if state is dirty between test runs)
adb shell pm clear com.ilafalkhayr.zadalhaj

# View live logs from the app
adb logcat -s MonoDroid:* AndroidRuntime:E *:S
```
