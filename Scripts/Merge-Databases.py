"""
Merge-Databases.py
------------------
One-time utility that copies the LocationEntity table from the plain
embedded_data.db (Salaat First) into the plain khayratAlhaj.db3 (app data),
producing a single combined database ready to be encrypted and bundled as
Resources/Data/appdata.bin.

Requirements:
    pip install pysqlcipher3   # only needed if encrypting here
    or just use DB Browser for SQLCipher to apply the key afterward.

Usage:
    python Scripts/Merge-Databases.py

Output:
    Scripts/appdata_plain.db   <- combined plain database
    Copy this to Resources/Data/appdata.bin AFTER encrypting it with SQLCipher
    using the key defined in AppDb.Key ("Kh@yr@t-Alh@j-S3cur3-K3y-2024!").
"""

import sqlite3
import shutil
import os

# ── Paths (relative to repo root) ────────────────────────────────────────────
SCRIPT_DIR   = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT    = os.path.dirname(SCRIPT_DIR)

SRC_APP_DB   = os.path.join(REPO_ROOT, "KhayratAlhaj", "Resources", "Data",  "appdata.bin")

# embedded_data.db is the original Salaat First location database.
# If you no longer have it, extract it from the original Salaat First APK:
#   apktool d SalaatFirst.apk -o salaat_out
#   copy salaat_out/assets/embedded_data.db here
SRC_LOC_DB   = os.path.join(SCRIPT_DIR, "embedded_data.db")

# ── Sanity checks ─────────────────────────────────────────────────────────────
if not os.path.exists(SRC_APP_DB):
    print(f"ERROR: App database not found:\n  {SRC_APP_DB}")
    raise SystemExit(1)

if not os.path.exists(SRC_LOC_DB):
    print(f"ERROR: Location database not found:\n  {SRC_LOC_DB}")
    print()
    print("Place the original embedded_data.db (from Salaat First) next to this script.")
    raise SystemExit(1)
OUTPUT_DB    = os.path.join(SCRIPT_DIR, "appdata_plain.db")

# ── Copy app DB as starting point ─────────────────────────────────────────────
print(f"Copying {SRC_APP_DB} → {OUTPUT_DB}")
shutil.copy2(SRC_APP_DB, OUTPUT_DB)

# ── Open both connections ──────────────────────────────────────────────────────
dst = sqlite3.connect(OUTPUT_DB)
src = sqlite3.connect(SRC_LOC_DB)

# ── Read schema + data from source ────────────────────────────────────────────
src_cur = src.execute(
    "SELECT sql FROM sqlite_master WHERE type='table' AND name='LocationEntity'")
schema_row = src_cur.fetchone()
if schema_row is None:
    print("ERROR: LocationEntity table not found in source database.")
    src.close(); dst.close(); raise SystemExit(1)

create_sql = schema_row[0]
rows = src.execute("SELECT * FROM LocationEntity").fetchall()
col_count = len(rows[0]) if rows else 0
placeholders = ", ".join(["?"] * col_count)

print(f"LocationEntity: {len(rows)} rows, {col_count} columns")

# ── Insert into destination ────────────────────────────────────────────────────
dst.execute(create_sql)
dst.executemany(f"INSERT INTO LocationEntity VALUES ({placeholders})", rows)
dst.commit()

src.close()
dst.close()

print(f"\nDone → {OUTPUT_DB}")
print()
print("Next steps:")
print("  1. Open Scripts/appdata_plain.db with DB Browser for SQLCipher")
print("  2. Go to File > Set Encryption Key")
print('     Key: Kh@yr@t-Alh@j-S3cur3-K3y-2024!')
print("  3. Save and close")
print("  4. Copy the encrypted file to KhayratAlhaj/Resources/Data/appdata.bin")
