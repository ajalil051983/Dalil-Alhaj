"""Temporary script: decrypt appdata.bin back to plain SQLite."""
import os, sqlite3, sqlcipher3

KEY = "Kh@yr@t-Alh@j-S3cur3-K3y-2024!"
DB  = r"d:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\Resources\Data\appdata.bin"
TMP = DB + ".plain"

if os.path.exists(TMP):
    os.remove(TMP)

# Open encrypted source
src = sqlcipher3.connect(DB)
src.execute(f"PRAGMA key = '{KEY}'")

# Get all table definitions and data
tables = src.execute("SELECT name, sql FROM sqlite_master WHERE type='table'").fetchall()

# Write to plain destination
dst = sqlite3.connect(TMP)
for name, ddl in tables:
    if ddl:
        dst.execute(ddl)
    rows = src.execute(f"SELECT * FROM [{name}]").fetchall()
    if rows:
        placeholders = ",".join(["?"] * len(rows[0]))
        dst.executemany(f"INSERT INTO [{name}] VALUES ({placeholders})", rows)
dst.commit()

n = dst.execute("SELECT count(*) FROM sqlite_master WHERE type='table'").fetchone()[0]
src.close()
dst.close()
print(f"Decrypted OK – {n} tables")

os.replace(TMP, DB)
print("Replaced appdata.bin with plain version")

