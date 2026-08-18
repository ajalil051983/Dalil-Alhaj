# Scripts

All helper scripts for the Khayrat Alhaj project live here, split by language.

## Layout

| Folder | Purpose |
|---|---|
| [python/](python/) | Python utilities (content, database, audio, Quran assets) |
| [python/archive/](python/archive/) | One-off fix scripts that were already applied — kept for history only, do not re-run |
| [ps/](ps/) | PowerShell scripts (build/deploy, emulator, keystores, audio) |

## Most-used scripts

### Audio generation — [python/generate_category_audio.py](python/generate_category_audio.py)

Generates Arabic TTS (Edge-TTS, OGG Vorbis) for a category. Creates missing
files, updates files whose text changed (SHA-256 manifest), skips the rest,
then copies the result to `KhayratAlhaj/Resources/Audio` and
`KhayratAlhaj/Resources/Raw/audio` (the folder the app packages).

```powershell
# PowerShell wrapper (checks dependencies first)
.\Scripts\ps\Generate-Audio.ps1 -Category 1
.\Scripts\ps\Generate-Audio.ps1 -Category 1 -Subcategory 102 -Force
.\Scripts\ps\Generate-Audio.ps1 -All

# or call Python directly
python Scripts\python\generate_category_audio.py --category 1
```

Requirements: `pip install edge-tts`, `winget install ffmpeg`.

### Database sync — [python/sync_categories_db.py](python/sync_categories_db.py)

Syncs `AIAudioGenerationFromText/categories.json` into
`KhayratAlhaj/Resources/Data/appdata.bin`. Compares field-by-field and writes
only changed or added rows. Makes a timestamped backup in `DbBackup/` first.

```powershell
python Scripts\python\sync_categories_db.py --dry-run   # preview
python Scripts\python\sync_categories_db.py --verbose   # apply, list changes
```

### Database inspection — [python/inspect_db.py](python/inspect_db.py)

```powershell
python Scripts\python\inspect_db.py                     # summary of prod DB
python Scripts\python\inspect_db.py --db backup         # inspect backup
python Scripts\python\inspect_db.py --subcategory 101   # dump one row
python Scripts\python\inspect_db.py --schema            # table definitions
```

## Other Python scripts

| Script | Purpose |
|---|---|
| [python/Encrypt-Database.py](python/Encrypt-Database.py) | Pre-encrypt appdata.bin with SQLCipher |
| [python/Decrypt-Database.py](python/Decrypt-Database.py) | Decrypt appdata.bin back to plain SQLite |
| [python/Merge-Databases.py](python/Merge-Databases.py) | Merge Salaat First locations into the app DB |
| [python/format_all_categories_pipeline.py](python/format_all_categories_pipeline.py) | Reformat/rebuild all category content |
| [python/export_new_categories_multilang_payload.py](python/export_new_categories_multilang_payload.py) | Export category payload for review |
| [python/validate_new_hajj_entries.py](python/validate_new_hajj_entries.py) | Sanity-check DB category counts |
| [python/update_quran_metadata.py](python/update_quran_metadata.py) | Update Quran metadata from alquran.cloud |
| [python/build_quran_pages_zip.py](python/build_quran_pages_zip.py) | Build Quran page image packages (WebP) |

## PowerShell scripts

| Script | Purpose |
|---|---|
| [ps/Generate-Audio.ps1](ps/Generate-Audio.ps1) | Audio generation wrapper (see above) |
| [ps/Deploy-GooglePlay.ps1](ps/Deploy-GooglePlay.ps1) | Build + sign AAB for Google Play |
| [ps/Start-Emulator.ps1](ps/Start-Emulator.ps1) | Start Android emulator |
| [ps/Start-Appium.ps1](ps/Start-Appium.ps1) | Start Appium server for UI tests |
| [ps/generate_keystore.ps1](ps/generate_keystore.ps1) | Create release keystore |
| [ps/generate_keystore_jks.ps1](ps/generate_keystore_jks.ps1) | Create release keystore (JKS format) |
