# Steps to Fix and Run the Appium Test

## Root Cause
The test was failing because the Android app crashes on launch due to **MAUI Fast Deployment**. When Appium installs the APK (outside Visual Studio), the assemblies are missing, causing this error:
```
No assemblies found in '/data/user/0/com.companyname.zadalhaj/files/.__override__/x86_64'
```

## Solution Already Applied ?
1. **AndroidManifest.xml** - Added queries for Appium server packages
2. **ZadAlhaj.csproj** - Already has `<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>` 
3. **AppiumSetup.cs** - Fixed syntax error and configured for Debug APK
4. All necessary Appium configurations and timeouts are in place

## Steps to Run the Test

### Step 1: Build the Android App
You **MUST** build the Android app first. In Visual Studio:

**Option A - Using Visual Studio UI:**
1. In Solution Explorer, **right-click** on the `ZadAlhaj` project
2. Select **Build** (or press Ctrl+Shift+B with the project selected)
3. Make sure it's building for **net10.0-android** (check the dropdown at the top)

**Option B - Using Developer Command Prompt:**
```powershell
msbuild "D:\Ai workspace\Dalil Alhaj\ZadAlhaj\ZadAlhaj.csproj" /t:Build /p:TargetFramework=net10.0-android /p:Configuration=Debug
```

**Option C - Using dotnet CLI (if above don't work):**
```powershell
dotnet publish "D:\Ai workspace\Dalil Alhaj\ZadAlhaj\ZadAlhaj.csproj" -f net10.0-android -c Debug
```

### Step 2: Verify the APK was Created
Run this command to check:
```powershell
Test-Path "D:\Ai workspace\Dalil Alhaj\ZadAlhaj\bin\Debug\net10.0-android\com.companyname.zadalhaj-Signed.apk"
```

Should return: `True`

### Step 3: Run the Appium Test
```powershell
dotnet test "D:\Ai workspace\Dalil Alhaj\ZadAlhaj.UITests\ZadAlhaj.UITests.csproj" --filter "FullyQualifiedName~Categories_ShouldLoadAndDisplay"
```

## Why This Should Work Now

1. ? `EmbedAssembliesIntoApk=true` forces all assemblies to be packaged in the APK
2. ? AndroidManifest.xml has the required `<queries>` element for Appium interaction  
3. ? AppiumSetup.cs is configured with proper timeouts for MAUI apps
4. ? Syntax error in AppiumSetup.cs has been fixed

## If Test Still Fails

Check the Appium server output for specific errors, or run:
```powershell
adb logcat -c  # Clear logs
# Run the test
adb shell "cat /data/tombstones/tombstone_*" | Select-Object -Last 100  # Check for crashes
```

## Files Modified
- ? `ZadAlhaj\Platforms\Android\AndroidManifest.xml`
- ? `ZadAlhaj.UITests\AppiumSetup.cs`
- ? `ZadAlhaj\ZadAlhaj.csproj` (already had the fix)
