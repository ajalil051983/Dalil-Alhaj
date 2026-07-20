# SQLite Migration Guide
## From Flat JSON Files → SQLite Database

---

## Overview

The app previously loaded category/subcategory data from three separate flat JSON assets:

| File | Language |
|---|---|
| `Resources/Raw/categories.json` | Arabic (default) |
| `Resources/Raw/categories-en.json` | English |
| `Resources/Raw/categories-fr.json` | French |

These files are now used **only on first launch** to seed a local SQLite database. All subsequent data access goes through SQLite, giving the app:

- **Faster startup** – no JSON parsing on every launch  
- **Unified multilingual data** – all three translations in one query  
- **Future-proof persistence** – easy to add new columns, queries, or data without shipping new JSON files  
- **Foundation for user data** – the same database can store notes, progress, or custom content later

---

## Architecture After Migration

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Pages                              │
│  MainPage / SubCategoryPage / SearchPage / ...              │
└────────────────────────┬────────────────────────────────────┘
                         │ uses
┌────────────────────────▼────────────────────────────────────┐
│                    DataService                               │
│  • In-memory cache per language (Dictionary<string, List>)   │
│  • Same public API as before (GetCategoriesAsync, etc.)      │
└────────────────────────┬────────────────────────────────────┘
                         │ delegates to
┌────────────────────────▼────────────────────────────────────┐
│                 DatabaseService  (NEW)                       │
│  • Owns the SQLiteAsyncConnection                            │
│  • Creates tables on first open                             │
│  • Seeds from JSON bundles when tables are empty            │
│  • Returns domain model objects (Category / SubCategory)    │
└────────────────────────┬────────────────────────────────────┘
                         │ reads / writes
┌────────────────────────▼────────────────────────────────────┐
│          SQLite DB  –  KhayratAlhaj.db3                        │
│  (FileSystem.AppDataDirectory/KhayratAlhaj.db3)               │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ Categories                                          │    │
│  │  Id | NameAr | NameEn | NameFr | Icon | Color      │    │
│  └─────────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ SubCategories                                       │    │
│  │  Id | CategoryId | NameAr | NameEn | NameFr |      │    │
│  │  Icon | ContentAr | ContentEn | ContentFr |         │    │
│  │  HasAudioAr | HasAudioEn | HasAudioFr               │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

---

## What Changed

### New Files

| File | Purpose |
|---|---|
| `Services/DatabaseService.cs` | SQLite connection, table creation, seeding, and queries |

### Modified Files

| File | Change |
|---|---|
| `KhayratAlhaj.csproj` | Added `sqlite-net-pcl` and `SQLitePCLRaw.bundle_green` NuGet packages |
| `Services/DataService.cs` | Removed JSON-loading logic; now delegates to `DatabaseService` |

### Unchanged Files (no breaking changes)

- `Models/Category.cs` – domain models are identical
- `MainPage.xaml.cs` and all other pages – public API of `DataService` is unchanged
- `Services/FavoritesService.cs` – still uses `Preferences` (unchanged)
- The three JSON asset files – still shipped in the app bundle for first-run seeding

---

## Database Schema

### `Categories` table

```sql
CREATE TABLE Categories (
    Id        INTEGER PRIMARY KEY,
    NameAr    TEXT NOT NULL DEFAULT '',
    NameEn    TEXT NOT NULL DEFAULT '',
    NameFr    TEXT NOT NULL DEFAULT '',
    Icon      TEXT NOT NULL DEFAULT '',
    Color     TEXT NOT NULL DEFAULT '#3498DB'
);
```

### `SubCategories` table

```sql
CREATE TABLE SubCategories (
    Id          INTEGER PRIMARY KEY,
    CategoryId  INTEGER NOT NULL,          -- FK → Categories.Id (indexed)
    NameAr      TEXT NOT NULL DEFAULT '',
    NameEn      TEXT NOT NULL DEFAULT '',
    NameFr      TEXT NOT NULL DEFAULT '',
    Icon        TEXT NOT NULL DEFAULT '📖',
    ContentAr   TEXT NOT NULL DEFAULT '',  -- Arabic long-form content
    ContentEn   TEXT NOT NULL DEFAULT '',  -- English long-form content
    ContentFr   TEXT NOT NULL DEFAULT '',  -- French long-form content
    HasAudioAr  INTEGER NOT NULL DEFAULT 0, -- BOOLEAN (0/1)
    HasAudioEn  INTEGER NOT NULL DEFAULT 0, -- BOOLEAN (0/1)
    HasAudioFr  INTEGER NOT NULL DEFAULT 0  -- BOOLEAN (0/1)
);
```

> The key difference from the old JSON model: `Content` has been split into three
> language-specific columns (`ContentAr`, `ContentEn`, `ContentFr`).  
> `DataService` selects the correct column at query time based on the active language
> and sets `SubCategory.Content` accordingly, so no page code needs to change.

---

## First-Run Seeding Flow

```
App starts
    │
    ▼
DatabaseService.GetDatabaseAsync()
    │
    ├─ CREATE TABLE IF NOT EXISTS Categories
    ├─ CREATE TABLE IF NOT EXISTS SubCategories
    │
    └─ COUNT(Categories) == 0 ?
          │
          YES → SeedDatabaseAsync()
          │         ├─ Load categories.json        (Arabic)
          │         ├─ Load categories-en.json     (English)
          │         ├─ Load categories-fr.json     (French)
          │         ├─ Merge by Id
          │         └─ INSERT all rows in one transaction
          │
          NO  → skip (already seeded)
```

---

## Steps to Complete the Migration

### Step 1 – Restore NuGet packages

```powershell
cd "D:\Ai workspace\Khayrat Alhaj"
dotnet restore
```

### Step 2 – Build and verify there are no compile errors

```powershell
dotnet build "KhayratAlhaj\KhayratAlhaj.csproj" -f net10.0-android -c Debug
```

> Build for any target platform you are testing on (`net10.0-windows10.0.26100.0`, etc.)

### Step 3 – Run the app

Launch the app normally. On first run:

1. `DatabaseService` detects an empty `Categories` table.  
2. The three JSON bundles are loaded and merged into SQLite.  
3. A `[DatabaseService] Database seeded from JSON bundles.` message is printed to Debug Output.  
4. All subsequent launches read directly from SQLite.

### Step 4 – Verify seeding (optional – Windows debug session)

Use the **DB Browser for SQLite** tool to inspect the database file at:

```
%LOCALAPPDATA%\Packages\<AppId>\LocalState\KhayratAlhaj.db3
```

Or on Android via `adb`:

```powershell
adb shell run-as com.companyname.khayratalhaj cat /data/data/com.companyname.khayratalhaj/files/KhayratAlhaj.db3 > KhayratAlhaj.db3
```

---

## Updating Content in the Future

### Option A – Update via JSON (re-seed)

1. Edit one or more of the three JSON asset files.  
2. Delete the database file (or uninstall/reinstall the app during development).  
3. Re-launch – the seeder runs again automatically.

### Option B – Direct SQL migration (production updates)

1. Add a `SchemaVersion` table or use a user-version pragma.  
2. In `DatabaseService.GetDatabaseAsync()`, check the version and run `ALTER TABLE` / `INSERT` statements as needed.  
3. Bump the version after each migration so it runs only once.

Example skeleton:

```csharp
var version = await _database.ExecuteScalarAsync<int>("PRAGMA user_version;");
if (version < 2)
{
    await _database.ExecuteAsync("ALTER TABLE SubCategories ADD COLUMN HasAudioAr INTEGER NOT NULL DEFAULT 0");
    await _database.ExecuteAsync("ALTER TABLE SubCategories ADD COLUMN HasAudioEn INTEGER NOT NULL DEFAULT 0");
    await _database.ExecuteAsync("ALTER TABLE SubCategories ADD COLUMN HasAudioFr INTEGER NOT NULL DEFAULT 0");
    await _database.ExecuteAsync("PRAGMA user_version = 2;");
}
```

---

## Rolling Back

If you need to revert to JSON-only loading:

1. Restore `Services/DataService.cs` from git (`git checkout HEAD~1 -- KhayratAlhaj/Services/DataService.cs`).
2. Remove `Services/DatabaseService.cs`.
3. Remove the two `sqlite-net-pcl` / `SQLitePCLRaw` `PackageReference` lines from the `.csproj`.
4. Run `dotnet restore`.

The JSON asset files were **never removed**, so the old code will work immediately.

---

## NuGet Packages Added

| Package | Version | Purpose |
|---|---|---|
| `sqlite-net-pcl` | 1.9.172 | ORM and SQLite abstraction for .NET MAUI / Xamarin |
| `SQLitePCLRaw.bundle_green` | 2.1.10 | Native SQLite provider (recommended bundle for all platforms) |
