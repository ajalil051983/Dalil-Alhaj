import argparse
import json
import shutil
import sqlite3
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

WARSH_EDITION = "quran-uthmani-quran-academy"
API_BASE = "https://api.alquran.cloud/v1"


def fetch_surah(surah_number: int) -> dict:
    url = f"{API_BASE}/surah/{surah_number}/{WARSH_EDITION}"
    with urllib.request.urlopen(url, timeout=60) as response:
        payload = json.loads(response.read().decode("utf-8"))

    if payload.get("code") != 200 or not payload.get("data"):
        raise RuntimeError(f"API response invalid for surah {surah_number}")

    return payload["data"]


def ensure_tables(conn: sqlite3.Connection) -> None:
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS QuranPackageMetadata (
            Key TEXT PRIMARY KEY,
            Value TEXT NOT NULL
        )
        """
    )
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS QuranSurahMetadata (
            SurahNumber INTEGER PRIMARY KEY,
            NameAr TEXT NOT NULL,
            AyahCount INTEGER NOT NULL,
            StartPage INTEGER NOT NULL,
            EndPage INTEGER NOT NULL
        )
        """
    )
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS QuranAyahPageMap (
            SurahNumber INTEGER NOT NULL,
            AyahNumber INTEGER NOT NULL,
            MushafPage INTEGER NOT NULL,
            PRIMARY KEY (SurahNumber, AyahNumber)
        )
        """
    )


def upsert_metadata(conn: sqlite3.Connection, zip_file_name: str, pdf_start_page: int) -> None:
    now = datetime.now(timezone.utc).isoformat()
    entries = {
        "quran_package_version": now,
        "quran_package_file": zip_file_name,
        "quran_package_pages": "604",
        "quran_pdf_start_page": str(pdf_start_page),
        "quran_edition": WARSH_EDITION,
        "quran_metadata_updated_utc": now,
    }

    for key, value in entries.items():
        conn.execute(
            "INSERT OR REPLACE INTO QuranPackageMetadata(Key, Value) VALUES(?, ?)",
            (key, value),
        )


def rebuild_quran_maps(conn: sqlite3.Connection) -> None:
    conn.execute("DELETE FROM QuranSurahMetadata")
    conn.execute("DELETE FROM QuranAyahPageMap")

    for surah_number in range(1, 115):
        data = fetch_surah(surah_number)
        ayahs = data.get("ayahs", [])
        if not ayahs:
            raise RuntimeError(f"No ayahs for surah {surah_number}")

        pages = [ayah.get("page", 0) for ayah in ayahs if int(ayah.get("page", 0)) > 0]
        if not pages:
            raise RuntimeError(f"No valid page values for surah {surah_number}")

        start_page = min(pages)
        end_page = max(pages)

        conn.execute(
            """
            INSERT INTO QuranSurahMetadata(SurahNumber, NameAr, AyahCount, StartPage, EndPage)
            VALUES(?, ?, ?, ?, ?)
            """,
            (
                int(data.get("number", surah_number)),
                data.get("name") or "",
                int(data.get("numberOfAyahs", len(ayahs))),
                int(start_page),
                int(end_page),
            ),
        )

        for ayah in ayahs:
            conn.execute(
                """
                INSERT INTO QuranAyahPageMap(SurahNumber, AyahNumber, MushafPage)
                VALUES(?, ?, ?)
                """,
                (
                    surah_number,
                    int(ayah.get("numberInSurah", 0)),
                    int(ayah.get("page", 0)),
                ),
            )

        if surah_number % 10 == 0:
            print(f"Fetched and inserted up to surah {surah_number}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Update Quran metadata tables in appdata.bin")
    parser.add_argument("--db", required=True, help="Path to appdata.bin")
    parser.add_argument("--zip-file-name", required=True, help="Hosted ZIP file name")
    parser.add_argument("--pdf-start-page", type=int, default=7, help="PDF page where mushaf page 1 starts")
    args = parser.parse_args()

    db_path = Path(args.db).resolve()
    if not db_path.exists():
        raise FileNotFoundError(f"Database not found: {db_path}")

    backup_path = db_path.with_suffix(db_path.suffix + ".pre_quran_meta.bak")
    shutil.copy2(db_path, backup_path)
    print(f"Backup created: {backup_path}")

    conn = sqlite3.connect(db_path)
    try:
        ensure_tables(conn)
        upsert_metadata(conn, args.zip_file_name, args.pdf_start_page)
        rebuild_quran_maps(conn)
        conn.commit()
    finally:
        conn.close()

    print("Quran metadata update completed.")


if __name__ == "__main__":
    main()
