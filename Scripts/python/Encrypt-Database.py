"""
Encrypt-Database.py
====================
Pre-encrypts Resources/Data/appdata.bin with SQLCipher so the app
can open it directly without any first-run encryption step.

Requirements:
    pip install sqlcipher3        # Windows: pip install sqlcipher3-binary
  OR
    pip install pysqlcipher3      # if sqlcipher3 is unavailable

Run from the repo root or from the Scripts folder:
    python Scripts/Encrypt-Database.py

The script:
  1. Reads the plain SQLite file at Resources/Data/appdata.bin
  2. Creates an encrypted copy next to it as appdata.bin.enc
  3. Replaces the original with the encrypted version
  4. Verifies the result by opening it with the key
"""

import os
import shutil
import sys

# ---------------------------------------------------------------------------
# Configuration – must match AppDb.Key in DatabaseService.cs
# ---------------------------------------------------------------------------
KEY = "Kh@yr@t-Alh@j-S3cur3-K3y-2024!"

SCRIPT_DIR  = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT   = os.path.dirname(SCRIPT_DIR)
DB_PATH     = os.path.join(REPO_ROOT, "KhayratAlhaj", "Resources", "Data", "appdata.bin")
ENC_PATH    = DB_PATH + ".enc"
BACKUP_PATH = DB_PATH + ".bak"

# ---------------------------------------------------------------------------
# Try importing sqlcipher3 (preferred) or pysqlcipher3
# ---------------------------------------------------------------------------
try:
    import sqlcipher3 as sqlite
    print("[Encrypt] Using sqlcipher3")
except ImportError:
    try:
        from pysqlcipher3 import dbapi2 as sqlite
        print("[Encrypt] Using pysqlcipher3")
    except ImportError:
        print("ERROR: Install sqlcipher3 or pysqlcipher3 first.")
        print("  pip install sqlcipher3-binary   (Windows, no build tools needed)")
        sys.exit(1)

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
def main():
    if not os.path.exists(DB_PATH):
        print(f"ERROR: source database not found: {DB_PATH}")
        sys.exit(1)

    # Clean up any previous partial output
    for p in (ENC_PATH, BACKUP_PATH):
        if os.path.exists(p):
            os.remove(p)

    print(f"[Encrypt] Source : {DB_PATH}")
    print(f"[Encrypt] Output : {ENC_PATH}")

    # Open the plain database using standard sqlite3 (no encryption), copy all rows
    # into a new SQLCipher-encrypted file using iterdump.
    import sqlite3 as plain_sqlite

    src = plain_sqlite.connect(DB_PATH)

    dst = sqlite.connect(ENC_PATH)
    dst.execute(f"PRAGMA key = '{KEY}'")
    dst.commit()

    dst.execute("BEGIN")
    for line in src.iterdump():
        try:
            dst.execute(line)
        except Exception:
            pass
    dst.commit()
    src.close()
    dst.close()

    print("[Encrypt] Export complete – verifying …")

    # Verify: open the encrypted file and count tables
    verify = sqlite.connect(ENC_PATH)
    verify.execute(f"PRAGMA key = '{KEY}'")
    n = verify.execute("SELECT count(*) FROM sqlite_master WHERE type='table'").fetchone()[0]
    verify.close()
    print(f"[Encrypt] Verified OK – {n} tables found in encrypted database.")

    # Swap: backup original, replace with encrypted version
    shutil.copy2(DB_PATH, BACKUP_PATH)
    shutil.move(ENC_PATH, DB_PATH)
    os.remove(BACKUP_PATH)

    print(f"[Encrypt] Done – {DB_PATH} is now SQLCipher-encrypted.")
    print("[Encrypt] Rebuild the app to include the updated asset.")

if __name__ == "__main__":
    main()
