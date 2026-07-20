import sqlite3

db = r'D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\Resources\Data\appdata.bin'
conn = sqlite3.connect(db)
cur = conn.cursor()

cur.execute("SELECT name, sql FROM sqlite_master WHERE type='table'")
for r in cur.fetchall():
    print('TABLE:', r[0])

cur.execute("SELECT Id, NameAr FROM Categories ORDER BY Id")
print('\nCategories:', cur.fetchall())

cur.execute("SELECT Id, CategoryId, NameAr, HasAudioAr FROM SubCategories ORDER BY Id LIMIT 20")
print('\nSubCategories (first 20):')
for r in cur.fetchall():
    print(r)

conn.close()
