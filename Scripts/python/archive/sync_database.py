"""
Sync categories.json → appdata.bin (production SQLite database)
Updates Categories and SubCategories tables.
"""
import json
import sqlite3
import shutil
from datetime import datetime

CATEGORIES_JSON = r'd:\Ai workspace\Khayrat Alhaj\AIAudioGenerationFromText\categories.json'
DB_PATH = r'D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\Resources\Data\appdata.bin'
BACKUP_PATH = r'd:\Ai workspace\Khayrat Alhaj\DbBackup\appdata.bin.bak'

# Backup first
shutil.copy2(DB_PATH, BACKUP_PATH)
print(f"✓ Backed up DB to {BACKUP_PATH}")

with open(CATEGORIES_JSON, 'r', encoding='utf-8') as f:
    data = json.load(f)

conn = sqlite3.connect(DB_PATH)
cur = conn.cursor()

# Check what columns exist in SubCategories
cur.execute("PRAGMA table_info(SubCategories)")
cols = {row[1] for row in cur.fetchall()}
has_surah = 'SurahNumber' in cols
has_api = 'ApiLookupName' in cols

cat_updated = 0
sub_updated = 0
sub_inserted = 0

for cat in data:
    cat_id = cat['id']
    cur.execute(
        "UPDATE Categories SET NameAr=?, NameEn=?, NameFr=?, Icon=?, Color=? WHERE Id=?",
        (cat['nameAr'], cat['nameEn'], cat['nameFr'], cat['icon'], cat.get('color', ''), cat_id)
    )
    if cur.rowcount > 0:
        cat_updated += 1
    else:
        cur.execute(
            "INSERT INTO Categories (Id, NameAr, NameEn, NameFr, Icon, Color) VALUES (?,?,?,?,?,?)",
            (cat_id, cat['nameAr'], cat['nameEn'], cat['nameFr'], cat['icon'], cat.get('color', ''))
        )
        cat_updated += 1

    for sub in cat.get('subcategories', []):
        sub_id = sub['id']
        has_audio_ar = 1 if sub.get('hasAudioAr') else 0
        has_audio_en = 1 if sub.get('hasAudioEn') else 0
        has_audio_fr = 1 if sub.get('hasAudioFr') else 0

        # Build update query based on existing columns
        update_sql = """UPDATE SubCategories SET
            NameAr=?, NameEn=?, NameFr=?, Icon=?,
            ContentAr=?, ContentEn=?, ContentFr=?,
            HasAudioAr=?, HasAudioEn=?, HasAudioFr=?"""
        params = [
            sub['nameAr'], sub['nameEn'], sub['nameFr'], sub['icon'],
            sub.get('contentAr', ''), sub.get('contentEn', ''), sub.get('contentFr', ''),
            has_audio_ar, has_audio_en, has_audio_fr,
        ]

        if has_surah:
            update_sql += ", SurahNumber=?"
            params.append(sub.get('surahNumber'))
        if has_api:
            update_sql += ", ApiLookupName=?"
            params.append(sub.get('apiLookupName', ''))

        update_sql += " WHERE Id=?"
        params.append(sub_id)

        cur.execute(update_sql, params)
        if cur.rowcount > 0:
            sub_updated += 1
        else:
            # Insert
            if has_surah and has_api:
                cur.execute("""INSERT INTO SubCategories
                    (Id, CategoryId, NameAr, NameEn, NameFr, Icon, ContentAr, ContentEn, ContentFr,
                     HasAudioAr, HasAudioEn, HasAudioFr, SurahNumber, ApiLookupName)
                    VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
                    (sub_id, cat_id, sub['nameAr'], sub['nameEn'], sub['nameFr'], sub['icon'],
                     sub.get('contentAr', ''), sub.get('contentEn', ''), sub.get('contentFr', ''),
                     has_audio_ar, has_audio_en, has_audio_fr,
                     sub.get('surahNumber'), sub.get('apiLookupName', '')))
            else:
                cur.execute("""INSERT INTO SubCategories
                    (Id, CategoryId, NameAr, NameEn, NameFr, Icon, ContentAr, ContentEn, ContentFr,
                     HasAudioAr, HasAudioEn, HasAudioFr)
                    VALUES (?,?,?,?,?,?,?,?,?,?,?,?)""",
                    (sub_id, cat_id, sub['nameAr'], sub['nameEn'], sub['nameFr'], sub['icon'],
                     sub.get('contentAr', ''), sub.get('contentEn', ''), sub.get('contentFr', ''),
                     has_audio_ar, has_audio_en, has_audio_fr))
            sub_inserted += 1

conn.commit()
conn.close()

print(f"✓ Categories updated: {cat_updated}")
print(f"✓ SubCategories updated: {sub_updated}, inserted: {sub_inserted}")
print("✓ Database sync complete!")
