"""
Sync categories.json -> KhayratAlhaj/Resources/Data/appdata.bin (SQLite).

Unlike the old sync_database.py (which rewrote every row), this script
compares each category/subcategory field-by-field and only writes rows that
were CHANGED or ADDED. Unchanged rows are left untouched.

Safety:
  * A timestamped backup of appdata.bin is written to DbBackup/ first
    (unless nothing needs to change, or --no-backup is given).
  * Use --dry-run to preview changes without writing anything.
  * Rows present in the DB but missing from categories.json are only
    reported, never deleted.

Usage:
    python sync_categories_db.py              # sync, writing only diffs
    python sync_categories_db.py --dry-run    # show what would change
    python sync_categories_db.py --verbose    # list every changed row
"""

import argparse
import json
import shutil
import sqlite3
import sys
from datetime import datetime
from pathlib import Path

# ── Paths (resolved relative to this file: Scripts/python/ -> workspace root)
ROOT = Path(__file__).resolve().parents[2]
CATEGORIES_JSON = ROOT / "AIAudioGenerationFromText" / "categories.json"
DB_PATH = ROOT / "KhayratAlhaj" / "Resources" / "Data" / "appdata.bin"
BACKUP_DIR = ROOT / "DbBackup"

CATEGORY_FIELDS = {  # json key -> db column
    "nameAr": "NameAr",
    "nameEn": "NameEn",
    "nameFr": "NameFr",
    "icon": "Icon",
    "color": "Color",
}
SUBCATEGORY_FIELDS = {
    "nameAr": "NameAr",
    "nameEn": "NameEn",
    "nameFr": "NameFr",
    "icon": "Icon",
    "contentAr": "ContentAr",
    "contentEn": "ContentEn",
    "contentFr": "ContentFr",
}
AUDIO_FLAGS = ("hasAudioAr", "hasAudioEn", "hasAudioFr")


def norm(value) -> str:
    """Normalize a value for comparison (None -> '', ints kept as-is)."""
    return "" if value is None else value


def db_columns(cur, table: str) -> set:
    cur.execute(f"PRAGMA table_info({table})")
    return {row[1] for row in cur.fetchall()}


def category_payload(cat: dict) -> dict:
    return {col: norm(cat.get(key, "")) for key, col in CATEGORY_FIELDS.items()}


def subcategory_payload(sub: dict, cols: set) -> dict:
    payload = {col: norm(sub.get(key, "")) for key, col in SUBCATEGORY_FIELDS.items()}
    for flag in AUDIO_FLAGS:
        payload[flag[0].upper() + flag[1:]] = 1 if sub.get(flag) else 0
    if "SurahNumber" in cols:
        payload["SurahNumber"] = sub.get("surahNumber")
    if "ApiLookupName" in cols:
        payload["ApiLookupName"] = norm(sub.get("apiLookupName", ""))
    return payload


def diff_fields(existing: dict, desired: dict) -> dict:
    """Return {column: new_value} for fields that differ."""
    return {
        col: new
        for col, new in desired.items()
        if col not in existing or norm(existing[col]) != norm(new)
    }


def sync(dry_run: bool, verbose: bool, no_backup: bool) -> None:
    if not CATEGORIES_JSON.exists():
        sys.exit(f"ERROR: {CATEGORIES_JSON} not found")
    if not DB_PATH.exists():
        sys.exit(f"ERROR: {DB_PATH} not found")

    data = json.loads(CATEGORIES_JSON.read_text(encoding="utf-8"))

    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    cur = conn.cursor()

    sub_cols = db_columns(cur, "SubCategories")
    cat_cols = db_columns(cur, "Categories")

    cur.execute("SELECT * FROM Categories")
    db_categories = {row["Id"]: dict(row) for row in cur.fetchall()}
    cur.execute("SELECT * FROM SubCategories")
    db_subcategories = {row["Id"]: dict(row) for row in cur.fetchall()}

    updates = []   # (sql, params, label)
    inserts = []   # (sql, params, label)
    unchanged = 0

    for cat in data:
        cat_id = cat["id"]
        desired = category_payload(cat)
        existing = db_categories.get(cat_id)

        if existing is None:
            cols = ["Id", *desired.keys()]
            sql = f"INSERT INTO Categories ({', '.join(cols)}) VALUES ({', '.join('?' * len(cols))})"
            inserts.append((sql, [cat_id, *desired.values()], f"category {cat_id} ({cat.get('nameEn', '')})"))
        else:
            changed = diff_fields(existing, desired)
            if changed:
                assignments = ", ".join(f"{col}=?" for col in changed)
                updates.append((f"UPDATE Categories SET {assignments} WHERE Id=?",
                                [*changed.values(), cat_id], f"category {cat_id}: {', '.join(changed)}"))
            else:
                unchanged += 1

        for sub in cat.get("subcategories", []):
            sub_id = sub["id"]
            desired = subcategory_payload(sub, sub_cols)
            existing = db_subcategories.get(sub_id)

            if existing is None:
                cols = ["Id", "CategoryId", *desired.keys()]
                sql = f"INSERT INTO SubCategories ({', '.join(cols)}) VALUES ({', '.join('?' * len(cols))})"
                inserts.append((sql, [sub_id, cat_id, *desired.values()],
                                f"subcategory {cat_id}_{sub_id} ({sub.get('nameEn', '')})"))
            else:
                if existing.get("CategoryId") != cat_id:
                    desired["CategoryId"] = cat_id
                changed = diff_fields(existing, desired)
                if changed:
                    assignments = ", ".join(f"{col}=?" for col in changed)
                    updates.append((f"UPDATE SubCategories SET {assignments} WHERE Id=?",
                                    [*changed.values(), sub_id],
                                    f"subcategory {cat_id}_{sub_id}: {', '.join(changed)}"))
                else:
                    unchanged += 1

    # Report DB rows missing from JSON (never deleted automatically)
    json_cat_ids = {c["id"] for c in data}
    json_sub_ids = {s["id"] for c in data for s in c.get("subcategories", [])}
    orphan_cats = sorted(set(db_categories) - json_cat_ids)
    orphan_subs = sorted(set(db_subcategories) - json_sub_ids)

    print(f"Categories/SubCategories unchanged : {unchanged}")
    print(f"Rows to update                     : {len(updates)}")
    print(f"Rows to insert                     : {len(inserts)}")
    if verbose or dry_run:
        for _, _, label in updates:
            print(f"  UPDATE {label}")
        for _, _, label in inserts:
            print(f"  INSERT {label}")
    if orphan_cats or orphan_subs:
        print("In DB but NOT in categories.json (left untouched):")
        if orphan_cats:
            print(f"  categories    : {orphan_cats}")
        if orphan_subs:
            print(f"  subcategories : {orphan_subs}")

    if dry_run:
        print("\n--dry-run: no changes written.")
        conn.close()
        return

    if not updates and not inserts:
        print("\nDatabase already in sync - nothing to do.")
        conn.close()
        return

    if not no_backup:
        BACKUP_DIR.mkdir(exist_ok=True)
        stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        backup = BACKUP_DIR / f"appdata.bin.{stamp}.bak"
        shutil.copy2(DB_PATH, backup)
        print(f"\nBackup written to {backup}")

    for sql, params, _ in updates:
        cur.execute(sql, params)
    for sql, params, _ in inserts:
        cur.execute(sql, params)
    conn.commit()
    conn.close()
    print("Database sync complete.")


def main() -> None:
    parser = argparse.ArgumentParser(description="Sync categories.json into appdata.bin (diff only).")
    parser.add_argument("--dry-run", action="store_true", help="Preview changes without writing")
    parser.add_argument("--verbose", action="store_true", help="List every changed row")
    parser.add_argument("--no-backup", action="store_true", help="Skip the DbBackup backup copy")
    args = parser.parse_args()
    sync(args.dry_run, args.verbose, args.no_backup)


if __name__ == "__main__":
    main()
