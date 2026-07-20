import sqlite3

db = r'D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\Resources\Data\appdata.bin'
conn = sqlite3.connect(db)
cur = conn.cursor()

for sub_id in (101, 102):
    cur.execute("SELECT Id, NameAr, NameEn, NameFr, ContentAr, ContentEn, ContentFr, HasAudioAr FROM SubCategories WHERE Id=?", (sub_id,))
    row = cur.fetchone()
    if row:
        print(f"=== SubCategory {sub_id} ===")
        print(f"  NameAr:     {row[1]}")
        print(f"  NameEn:     {row[2]}")
        print(f"  NameFr:     {row[3]}")
        print(f"  HasAudioAr: {row[7]}")
        print(f"  ContentAr:  {row[4][:120]}...")
        print(f"  ContentEn:  {row[5][:120]}...")
        print(f"  ContentFr:  {row[6][:120]}...")
        print()

conn.close()
