import json
import sqlite3
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
DB_PATH = ROOT / "KhayratAlhaj" / "Resources" / "Data" / "appdata.bin"
OUT_DIR = ROOT / "Docs" / "generated"
OUT_PATH = OUT_DIR / "hajj_categories_multilang_payload.json"

TARGET_CATEGORY_IDS = (7, 8)


def main():
    con = sqlite3.connect(DB_PATH)
    cur = con.cursor()

    categories = cur.execute(
        """
        SELECT Id, NameAr, NameEn, NameFr, Icon, Color
        FROM Categories
        WHERE Id IN (?, ?)
        ORDER BY Id
        """,
        TARGET_CATEGORY_IDS,
    ).fetchall()

    payload = []
    for cat_id, name_ar, name_en, name_fr, icon, color in categories:
        subs = cur.execute(
            """
            SELECT Id, NameAr, NameEn, NameFr, Icon, ContentAr, ContentEn, ContentFr, HasAudioAr, HasAudioEn, HasAudioFr
            FROM SubCategories
            WHERE CategoryId = ?
            ORDER BY Id
            """,
            (cat_id,),
        ).fetchall()

        payload.append(
            {
                "id": cat_id,
                "nameAr": name_ar,
                "nameEn": name_en,
                "nameFr": name_fr,
                "icon": icon,
                "color": color,
                "subcategories": [
                    {
                        "id": sub_id,
                        "nameAr": sub_name_ar,
                        "nameEn": sub_name_en,
                        "nameFr": sub_name_fr,
                        "icon": sub_icon,
                        "contentAr": content_ar,
                        "contentEn": content_en,
                        "contentFr": content_fr,
                        "hasAudioAr": bool(has_audio_ar),
                        "hasAudioEn": bool(has_audio_en),
                        "hasAudioFr": bool(has_audio_fr),
                    }
                    for (
                        sub_id,
                        sub_name_ar,
                        sub_name_en,
                        sub_name_fr,
                        sub_icon,
                        content_ar,
                        content_en,
                        content_fr,
                        has_audio_ar,
                        has_audio_en,
                        has_audio_fr,
                    ) in subs
                ],
            }
        )

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    OUT_PATH.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")

    con.close()

    print(f"Exported categories: {[c['id'] for c in payload]}")
    print(f"Subcategory counts: {[len(c['subcategories']) for c in payload]}")
    print(f"Output: {OUT_PATH}")


if __name__ == "__main__":
    main()
