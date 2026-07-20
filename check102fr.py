import sqlite3

db = r'D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\Resources\Data\appdata.bin'
conn = sqlite3.connect(db)
cur = conn.cursor()

cur.execute("SELECT ContentFr FROM SubCategories WHERE Id=102")
row = cur.fetchone()
print("=== 102 ContentFr full ===")
print(row[0])
conn.close()
