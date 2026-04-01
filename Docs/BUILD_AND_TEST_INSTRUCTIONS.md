# Build and Test Instructions

## Current Status ✅
All **8 UI tests pass** as of March 2026.

## Project Identity
| Property | Value |
|---|---|
| Package ID | `com.ilafalkhayr.khayratalhaj` |
| MainActivity | `crc640e514d85339b6ec1.MainActivity` |
| APK path | `KhayratAlhaj/bin/Release/net10.0-android/com.ilafalkhayr.khayratalhaj-Signed.apk` |
| Appium version | 3.2.0 |
| Node version | v22.15.1 |
| Target framework | net10.0-android |

---

## Step 1: Start the Android Emulator

Launch an AVD from Android Studio's Device Manager, or use the helper script:
```powershell
.\Start-Emulator.ps1
```
Wait until `adb devices` shows `emulator-5554   device`.

---

## Step 2: Build the Release APK

> **Must be Release** — Debug builds use MAUI Fast Deployment and will crash when
> installed by Appium (assemblies are not bundled in the APK).

```powershell
dotnet build "D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\KhayratAlhaj.csproj" `
    -f net10.0-android -c Release
```

Expected output: `La génération a réussi. 0 Erreur(s)` (or "Build succeeded. 0 Error(s)" in English).

Verify the APK exists:
```powershell
Test-Path "D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\bin\Release\net10.0-android\com.ilafalkhayr.khayratalhaj-Signed.apk"
# Should return: True
```

---

## Step 3: Install the APK on the Emulator

Appium is configured with `noReset: true` (no reinstall each session), so the APK must be
pre-installed manually once per emulator wipe:

```powershell
$env:ANDROID_HOME = "C:\PROGRA~2\Android\ANDROI~1"
$env:Path += ";$env:ANDROID_HOME\platform-tools"
adb -s emulator-5554 install -r "D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\bin\Release\net10.0-android\com.ilafalkhayr.khayratalhaj-Signed.apk"
```

---

## Step 4: Start Appium Server

Use the provided script (already patched for Windows 8.3 paths):
```powershell
.\Start-Appium.ps1
```

Or manually:
```powershell
$env:ANDROID_HOME = "C:\PROGRA~2\Android\ANDROI~1"
$env:Path += ";$env:ANDROID_HOME\platform-tools"
node "$env:APPDATA\fnm\node-versions\v22.15.1\installation\node_modules\appium\index.js" --allow-cors
```

Expected: `Appium REST http interface listener started on http://0.0.0.0:4723`

---

## Step 5: Run the Tests

Run all 8 UI tests:
```powershell
dotnet test "D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj.UITests\KhayratAlhaj.UITests.csproj"
```

Run a single test:
```powershell
dotnet test "D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj.UITests\KhayratAlhaj.UITests.csproj" `
    --filter "FullyQualifiedName~Categories_ShouldLoadAndDisplay"
```

Expected result: **8 passed, 0 failed**.

---

## Test Suite Overview

| Test File | Test Name | What It Verifies |
|---|---|---|
| `CategoriesTests.cs` | `Categories_ShouldLoadAndDisplay` | Category grid renders on MainPage |
| `CategoriesTests.cs` | `SelectingCategory_ShouldNavigateToSubCategories` | Tap category → SubCategoryPage |
| `SubCategoryTests.cs` | `SubCategory_ShouldListItems` | SubCategoriesCollection loads |
| `ChecklistTests.cs` | `Checklist_ShouldToggleItems_AndUpdateProgress` | CheckBox + ProgressBar |
| `SearchTests.cs` | `SearchBar_ShouldExistAndAcceptText` | SearchBar accepts Arabic text |
| `FavoritesTests.cs` | `AddToFavorites_ShouldPersist_AndShowInFavoritesList` | FavoriteButton on detail page |
| `MapTests.cs` | `Map_ShouldLoad` | MapControl renders |
| `SettingsTests.cs` | `Settings_ShouldAllowChangingLanguageAndTheme` | Picker, Switch, font buttons |

---

## Key Configuration (AppiumSetup.cs)

```csharp
appPackage  = "com.ilafalkhayr.khayratalhaj"
appActivity = "crc640e514d85339b6ec1.MainActivity"
noReset     = true    // app must be pre-installed (Step 3)
forceAppLaunch = true // restart app each test
```

---

## If Tests Fail

See [TROUBLESHOOTING_APPIUM.md](TROUBLESHOOTING_APPIUM.md) for known issues and fixes.
