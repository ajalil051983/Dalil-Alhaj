import sqlite3
import json

PROD_DB = r'D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\Resources\Data\appdata.bin'
JSON_FILE = r'd:\Ai workspace\Khayrat Alhaj\AIAudioGenerationFromText\categories.json'

with open(JSON_FILE, 'r', encoding='utf-8') as f:
    data = json.load(f)

json_subs = {}
for cat in data:
    for sub in cat.get('subcategories', []):
        json_subs[sub['id']] = sub

conn = sqlite3.connect(PROD_DB)
cur = conn.cursor()

for sub_id in (101, 102):
    cur.execute("SELECT Id, NameAr, NameEn, NameFr, ContentAr, ContentEn, ContentFr, HasAudioAr FROM SubCategories WHERE Id=?", (sub_id,))
    row = cur.fetchone()
    js = json_subs.get(sub_id, {})

    print(f"=== SubCategory {sub_id} ===")
    print(f"  DB  NameAr:    {row[1]!r}")
    print(f"  JSON NameAr:   {js.get('nameAr')!r}")
    print(f"  Match Name:    {row[1] == js.get('nameAr')}")
    print(f"  DB  ContentAr: {row[4][:80]!r}...")
    print(f"  JSON ContentAr:{str(js.get('contentAr',''))[:80]!r}...")
    print(f"  Match ContentAr: {row[4] == js.get('contentAr','')}")
    print(f"  DB  ContentEn: {row[5][:80]!r}...")
    print(f"  Match ContentEn: {row[5] == js.get('contentEn','')}")
    print(f"  DB  ContentFr: {row[6][:80]!r}...")
    print(f"  Match ContentFr: {row[6] == js.get('contentFr','')}")
    print()

conn.close()
