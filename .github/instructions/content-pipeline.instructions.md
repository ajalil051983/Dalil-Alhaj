---
description: "Use when editing categories.json, generating audio, syncing appdata.bin, or writing content/database scripts. Covers the content pipeline order, safety rules, and script conventions."
applyTo: "AIAudioGenerationFromText/**, Scripts/**"
---

# Content & Database Pipeline Rules

## Order of operations (never skip steps)

1. Edit `AIAudioGenerationFromText/categories.json` — the only source of truth.
2. `python Scripts/python/sync_categories_db.py --dry-run` → review diff → run without `--dry-run`.
3. `python Scripts/python/generate_category_audio.py --category <id>` for categories whose Arabic text changed.
4. Verify: `python Scripts/python/inspect_db.py --subcategory <id>`.

## Safety rules

- **Never** edit `KhayratAlhaj/Resources/Data/appdata.bin` with hand-written SQL scripts for content changes — extend `sync_categories_db.py` instead. It backs up to `DbBackup/` and writes only diffs.
- One-off content fixes that were already applied live in `Scripts/python/archive/` — do not re-run or "improve" them.
- Don't delete DB rows missing from JSON without explicit user confirmation.

## Script conventions

- Resolve repo root from the script location: `Path(__file__).resolve().parents[2]` in `Scripts/python/`; `$PSScriptRoot` + two levels up in `Scripts/ps/`.
- No hard-coded drive paths — several old scripts broke because of `D:\Ai workspace\...` literals.
- Print Arabic safely: `sys.stdout.reconfigure(encoding="utf-8", errors="replace")` at the top.
- New subcategory JSON entries must include `hasAudioAr`, `hasAudioEn`, `hasAudioFr` explicitly.
- Audio output is OGG Vorbis (`libvorbis -q:a 4`) named `{catId}_{subId}.ogg`, voice `ar-SA-HamedNeural`, text = `{nameAr}. {plain-text contentAr}`.
