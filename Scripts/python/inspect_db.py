"""
Inspect the app SQLite database (merged replacement for the old
inspect_db.py / inspect_prod_db.py / check_*.py / verify_*.py one-offs).

Usage:
    python inspect_db.py                          # summary of the prod DB
    python inspect_db.py --db backup              # inspect DbBackup/appdata.bin.bak
    python inspect_db.py --db path\to\file.bin    # any SQLite file
    python inspect_db.py --schema                 # full CREATE TABLE statements
    python inspect_db.py --subcategory 101        # dump one SubCategories row
    python inspect_db.py --category 7             # list subcategories of a category
"""

import argparse
import sqlite3
import sys
from pathlib import Path

# Arabic output needs UTF-8 on the Windows console
sys.stdout.reconfigure(encoding="utf-8", errors="replace")

# ── Paths (resolved relative to this file: Scripts/python/ -> workspace root)
ROOT = Path(__file__).resolve().parents[2]
PROD_DB = ROOT / "KhayratAlhaj" / "Resources" / "Data" / "appdata.bin"
BACKUP_DB = ROOT / "DbBackup" / "appdata.bin.bak"


def resolve_db(arg: str) -> Path:
    if arg == "prod":
        return PROD_DB
    if arg == "backup":
        return BACKUP_DB
    return Path(arg)


def show_summary(cur) -> None:
    cur.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
    tables = [r[0] for r in cur.fetchall()]
    print(f"Tables: {', '.join(tables)}\n")

    cur.execute("SELECT Id, NameAr, NameEn FROM Categories ORDER BY Id")
    print("Categories:")
    for row in cur.fetchall():
        print(f"  {row[0]:>3}  {row[1]}  |  {row[2]}")

    cur.execute("SELECT COUNT(*) FROM SubCategories")
    print(f"\nSubCategories total: {cur.fetchone()[0]}")


def show_schema(cur) -> None:
    cur.execute("SELECT name, sql FROM sqlite_master WHERE type='table' ORDER BY name")
    for name, sql in cur.fetchall():
        print(f"TABLE: {name}")
        print(sql)
        print()


def show_subcategory(cur, sub_id: int) -> None:
    cur.execute("SELECT * FROM SubCategories WHERE Id=?", (sub_id,))
    row = cur.fetchone()
    if not row:
        print(f"Subcategory {sub_id} not found.")
        return
    cols = [d[0] for d in cur.description]
    print(f"=== SubCategory {sub_id} ===")
    for col, val in zip(cols, row):
        text = str(val)
        if len(text) > 300:
            text = text[:300] + f"... ({len(str(val))} chars)"
        print(f"  {col:<14}: {text}")


def show_category(cur, cat_id: int) -> None:
    cur.execute(
        "SELECT Id, NameAr, NameEn, HasAudioAr FROM SubCategories "
        "WHERE CategoryId=? ORDER BY Id", (cat_id,))
    rows = cur.fetchall()
    if not rows:
        print(f"Category {cat_id} not found or has no subcategories.")
        return
    print(f"=== Category {cat_id} subcategories ===")
    for row in rows:
        print(f"  {row[0]:>4}  audio={row[3]}  {row[1]}  |  {row[2]}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Inspect the app SQLite database.")
    parser.add_argument("--db", default="prod",
                        help="'prod' (default), 'backup', or a path to a SQLite file")
    parser.add_argument("--schema", action="store_true", help="Print CREATE TABLE statements")
    parser.add_argument("--subcategory", type=int, metavar="ID", help="Dump one SubCategories row")
    parser.add_argument("--category", type=int, metavar="ID", help="List subcategories of a category")
    args = parser.parse_args()

    db = resolve_db(args.db)
    if not db.exists():
        sys.exit(f"ERROR: database not found: {db}")
    print(f"Database: {db}\n")

    conn = sqlite3.connect(db)
    cur = conn.cursor()
    if args.schema:
        show_schema(cur)
    elif args.subcategory is not None:
        show_subcategory(cur, args.subcategory)
    elif args.category is not None:
        show_category(cur, args.category)
    else:
        show_summary(cur)
    conn.close()


if __name__ == "__main__":
    main()
