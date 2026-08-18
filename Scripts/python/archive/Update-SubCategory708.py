"""
Update-SubCategory708.py
========================
Updates subcategory 708 (طواف الوداع) in appdata.bin (plain SQLite)
and regenerates the audio file 7_708.mp3 using Edge-TTS.

Steps:
  1. UPDATE SubCategories WHERE Id=708 from categories.json
  2. Regenerate audio/7_708.mp3
  3. Copy 7_708.mp3 → KhayratAlhaj/Resources/Raw/audio/
"""

import asyncio
import html
import json
import os
import re
import shutil
import sqlite3
import sys
from pathlib import Path

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
SCRIPT_DIR  = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT   = os.path.dirname(SCRIPT_DIR)
DB_PATH     = os.path.join(REPO_ROOT, "KhayratAlhaj", "Resources", "Data", "appdata.bin")
JSON_PATH   = os.path.join(REPO_ROOT, "AIAudioGenerationFromText", "categories.json")
AUDIO_DIR   = os.path.join(REPO_ROOT, "AIAudioGenerationFromText", "audio")
APP_AUDIO   = os.path.join(REPO_ROOT, "KhayratAlhaj", "Resources", "Raw", "audio")

CAT_ID      = 7
SUB_ID      = 708
AUDIO_FILE  = f"{CAT_ID}_{SUB_ID}.mp3"
VOICE       = "ar-SA-HamedNeural"


def html_to_plain_text(content: str) -> str:
    content = re.sub(r"<\s*br\s*/?\s*>", "\n", content, flags=re.IGNORECASE)
    content = re.sub(r"</\s*(p|h[1-6]|li|aside|div)\s*>", ". ", content, flags=re.IGNORECASE)
    content = re.sub(r"<[^>]+>", "", content)
    content = html.unescape(content)
    return re.sub(r"\s+", " ", content).strip()

# ---------------------------------------------------------------------------
# Step 1: Load subcategory 708 from categories.json
# ---------------------------------------------------------------------------
print(f"\n[1/3] Loading subcategory {SUB_ID} from categories.json …")
with open(JSON_PATH, "r", encoding="utf-8") as f:
    categories = json.load(f)

sub_data = None
for cat in categories:
    if cat["id"] == CAT_ID:
        for sub in cat.get("subcategories", []):
            if sub["id"] == SUB_ID:
                sub_data = sub
                break
    if sub_data:
        break

if not sub_data:
    print(f"ERROR: Subcategory {SUB_ID} not found in categories.json")
    sys.exit(1)

print(f"  Found: {sub_data['nameAr']}")
print(f"  hasAudioAr = {sub_data.get('hasAudioAr', False)}")

# ---------------------------------------------------------------------------
# Step 2: UPDATE SubCategories for id=708 in plain SQLite
# ---------------------------------------------------------------------------
print(f"\n[2/3] Updating database …")
shutil.copy2(DB_PATH, DB_PATH + ".bak")
print(f"  Backup saved as appdata.bin.bak")

conn = sqlite3.connect(DB_PATH)
conn.execute(
    """UPDATE SubCategories SET
        NameAr     = ?,
        NameEn     = ?,
        NameFr     = ?,
        Icon       = ?,
        ContentAr  = ?,
        ContentEn  = ?,
        ContentFr  = ?,
        HasAudioAr = ?,
        HasAudioEn = ?,
        HasAudioFr = ?
    WHERE Id = ?""",
    (
        sub_data.get("nameAr", ""),
        sub_data.get("nameEn", ""),
        sub_data.get("nameFr", ""),
        sub_data.get("icon", "📖"),
        sub_data.get("contentAr", ""),
        sub_data.get("contentEn", ""),
        sub_data.get("contentFr", ""),
        1 if sub_data.get("hasAudioAr", False) else 0,
        1 if sub_data.get("hasAudioEn", False) else 0,
        1 if sub_data.get("hasAudioFr", False) else 0,
        SUB_ID,
    ),
)
rows_updated = conn.execute("SELECT changes()").fetchone()[0]
conn.commit()
print(f"  Updated {rows_updated} row(s) in SubCategories.")

row = conn.execute(
    "SELECT Id, NameAr, HasAudioAr FROM SubCategories WHERE Id = ?", (SUB_ID,)
).fetchone()
if row:
    print(f"  Verified: Id={row[0]}, NameAr={row[1]}, HasAudioAr={row[2]}")
conn.close()

# ---------------------------------------------------------------------------
# Step 3: Generate audio 7_708.mp3 with Edge-TTS
# ---------------------------------------------------------------------------
print(f"\n[3/3] Generating audio {AUDIO_FILE} with Edge-TTS ({VOICE}) …")

try:
    import edge_tts
except ImportError:
    print("  Installing edge-tts …")
    os.system(f'"{sys.executable}" -m pip install edge-tts --quiet')
    import edge_tts

Path(AUDIO_DIR).mkdir(parents=True, exist_ok=True)
audio_out = os.path.join(AUDIO_DIR, AUDIO_FILE)

title   = sub_data.get("nameAr", "")
content = html_to_plain_text(sub_data.get("contentAr", ""))
text    = f"{title}. {content}"

async def gen():
    communicate = edge_tts.Communicate(text, VOICE)
    await communicate.save(audio_out)

asyncio.run(gen())
size_kb = os.path.getsize(audio_out) // 1024
print(f"  Generated: {audio_out} ({size_kb} KB)")

# ---------------------------------------------------------------------------
# Copy audio to app Resources
# ---------------------------------------------------------------------------
print(f"\n[3/3] Copying audio to app Resources …")
dest = os.path.join(APP_AUDIO, AUDIO_FILE)
shutil.copy2(audio_out, dest)
print(f"  Copied → {dest}")

print(f"\n✅ Done! Subcategory {SUB_ID} updated in DB and audio regenerated.\n")
