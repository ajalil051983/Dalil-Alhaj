# Khayrat Alhaj — Project Guidelines

Comprehensive Hajj guide app (Maliki school) built with **.NET MAUI 10** (`net10.0-android`, `net10.0-windows`). Trilingual content: Arabic (primary, RTL), English, French.

## Repository Layout

| Path | Purpose |
|---|---|
| `KhayratAlhaj/` | Main MAUI app (Pages, Models, Services, Resources) |
| `KhayratAlhaj.UITests/` | Appium UI tests |
| `KhayratAlhaj.UnitTests/` | xUnit unit tests |
| `AIAudioGenerationFromText/categories.json` | **Source of truth** for all category/subcategory content |
| `KhayratAlhaj/Resources/Data/appdata.bin` | App SQLite database (plain SQLite in this repo) |
| `KhayratAlhaj/Resources/Raw/audio/` | OGG audio packaged into the app (`{catId}_{subId}.ogg`) |
| `Scripts/python/`, `Scripts/ps/` | All helper scripts — see [Scripts/README.md](../Scripts/README.md) |
| `Docs/` | All documentation (build, features, maps, notifications, migration) |

## Build and Test

```powershell
# Android release build (Debug APKs crash under Appium — always Release for UI tests)
dotnet build KhayratAlhaj/KhayratAlhaj.csproj -f net10.0-android -c Release

# Unit tests
dotnet test KhayratAlhaj.UnitTests/KhayratAlhaj.UnitTests.csproj
```

Full UI-test workflow (emulator, Appium, APK install): [Docs/BUILD_AND_TEST_INSTRUCTIONS.md](../Docs/BUILD_AND_TEST_INSTRUCTIONS.md).
Never use `dotnet run` for this MAUI app — build + deploy to emulator/device instead.

## Git — never commit or push

**Never run `git commit`, `git push`, `git add`, or any command that commits or publishes changes.** The user does all version control themselves. Make the code/file changes only, build and test them, then stop and let the user review and commit. (History rewrite / force-push is likewise off-limits unless the user explicitly asks for it in that moment.)

## Content Pipeline (important — follow exactly)

1. Edit content **only** in `AIAudioGenerationFromText/categories.json` (never edit `appdata.bin` directly).
2. Sync to the database with `python Scripts/python/sync_categories_db.py --dry-run` first, then without `--dry-run`. It writes only changed/added rows and backs up the DB to `DbBackup/` automatically.
3. Regenerate audio with `python Scripts/python/generate_category_audio.py --category <id>` (or `.\Scripts\ps\Generate-Audio.ps1 -Category <id>`). It creates/updates only changed items and copies OGG files to `Resources/Audio` and `Resources/Raw/audio`.
4. `Scripts/python/archive/` contains one-off fix scripts already applied — never re-run them.

## Conventions

- **Never hard-code absolute paths** (e.g. `D:\Ai workspace\...`). Scripts must resolve the repo root from their own location (`Path(__file__).resolve().parents[...]` / `$PSScriptRoot`).
- Python scripts printing Arabic must call `sys.stdout.reconfigure(encoding="utf-8", errors="replace")` (Windows console is cp1252).
- Audio files are OGG Vorbis named `{categoryId}_{subcategoryId}.ogg`; the app loads them via `FileSystem.OpenAppPackageFileAsync("audio/...")` — keep this naming or playback breaks.
- Subcategories in JSON must always declare `hasAudioAr`, `hasAudioEn`, `hasAudioFr` explicitly.
- Content HTML uses a small subset: `h3`/`h4` headings, `ul/li`, `p`, `strong`, `br`. Keep it consistent across the three languages.
- Git history matters: move files with `git mv`.

## Tech Stack

CommunityToolkit.Mvvm (MVVM source generators), CommunityToolkit.Maui + MediaElement (audio), sqlite-net / SQLitePCLRaw, Mapsui (interactive Hajj map), Plugin.LocalNotification (prayer reminders). Prayer times follow the Salaat First approach — see [Docs/SalaatFirst_PrayerTimes_CSharp_Guide.md](../Docs/SalaatFirst_PrayerTimes_CSharp_Guide.md).

More docs: [Docs/FEATURES.md](../Docs/FEATURES.md), [Docs/MAPS_SETUP.md](../Docs/MAPS_SETUP.md), [Docs/NOTIFICATIONS.md](../Docs/NOTIFICATIONS.md), [Docs/SQLITE_MIGRATION.md](../Docs/SQLITE_MIGRATION.md), [Docs/TROUBLESHOOTING_APPIUM.md](../Docs/TROUBLESHOOTING_APPIUM.md).
