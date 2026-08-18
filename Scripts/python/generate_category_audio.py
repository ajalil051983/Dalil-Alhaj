"""
Generate audio for a single category (or all) from categories.json.

For each subcategory with hasAudioAr=true the script will:
  * CREATE the .ogg file if it does not exist yet
  * UPDATE the .ogg file if the narration text changed since last generation
    (tracked via a SHA-256 manifest, so unchanged files are skipped)
  * COPY the result to KhayratAlhaj/Resources/Audio  (as requested)
    and to KhayratAlhaj/Resources/Raw/audio          (what the app packages -
    ContentDetailPage loads "audio/{catId}_{subId}.ogg" from Raw assets)

Usage:
    python generate_category_audio.py --category 1
    python generate_category_audio.py --category 1 --subcategory 102
    python generate_category_audio.py --all
    python generate_category_audio.py --category 1 --force   # ignore manifest

Requirements:
    pip install edge-tts
    ffmpeg on PATH  (winget install ffmpeg)
"""

import argparse
import asyncio
import hashlib
import html
import json
import re
import shutil
import subprocess
import sys
from pathlib import Path

# ── Paths (resolved relative to this file: Scripts/python/ -> workspace root)
ROOT = Path(__file__).resolve().parents[2]
CATEGORIES_JSON = ROOT / "AIAudioGenerationFromText" / "categories.json"
APP_AUDIO_DIR = ROOT / "KhayratAlhaj" / "Resources" / "Audio"        # requested target
RAW_AUDIO_DIR = ROOT / "KhayratAlhaj" / "Resources" / "Raw" / "audio"  # packaged by the app
MANIFEST_PATH = APP_AUDIO_DIR / "audio_manifest.json"

# Available voices:
#   ar-SA-HamedNeural   (male, Saudi)   ar-SA-ZariyahNeural (female, Saudi)
#   ar-EG-ShakirNeural  (male, Egypt)   ar-EG-SalmaNeural   (female, Egypt)
VOICE = "ar-SA-HamedNeural"


def html_to_plain_text(content: str) -> str:
    content = re.sub(r"<\s*br\s*/?\s*>", "\n", content, flags=re.IGNORECASE)
    content = re.sub(r"</\s*(p|h[1-6]|li|aside|div)\s*>", ". ", content, flags=re.IGNORECASE)
    content = re.sub(r"<[^>]+>", "", content)
    content = html.unescape(content)
    return re.sub(r"\s+", " ", content).strip()


def text_hash(text: str) -> str:
    return hashlib.sha256(f"{VOICE}|{text}".encode("utf-8")).hexdigest()


def load_manifest() -> dict:
    if MANIFEST_PATH.exists():
        try:
            return json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            return {}
    return {}


def save_manifest(manifest: dict) -> None:
    MANIFEST_PATH.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2, sort_keys=True),
        encoding="utf-8",
    )


def require_ffmpeg() -> None:
    if shutil.which("ffmpeg") is None:
        sys.exit("ERROR: ffmpeg not found. Install it: winget install ffmpeg")


def mp3_to_ogg(mp3_path: Path, ogg_path: Path) -> None:
    subprocess.run(
        ["ffmpeg", "-y", "-i", str(mp3_path),
         "-c:a", "libvorbis", "-q:a", "4", str(ogg_path)],
        check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
    )
    mp3_path.unlink()


async def generate_one(text: str, ogg_path: Path) -> None:
    import edge_tts
    tmp_mp3 = ogg_path.with_name(ogg_path.stem + "_tmp.mp3")
    try:
        await edge_tts.Communicate(text, VOICE).save(str(tmp_mp3))
        mp3_to_ogg(tmp_mp3, ogg_path)
    finally:
        if tmp_mp3.exists():
            tmp_mp3.unlink()


async def run(category_id: int | None, subcategory_id: int | None,
              do_all: bool, force: bool) -> None:
    require_ffmpeg()

    categories = json.loads(CATEGORIES_JSON.read_text(encoding="utf-8"))

    if do_all:
        selected = categories
    else:
        selected = [c for c in categories if c.get("id") == category_id]
        if not selected:
            available = ", ".join(str(c.get("id")) for c in categories)
            sys.exit(f"ERROR: category {category_id} not found in categories.json. "
                     f"Available ids: {available}")

    APP_AUDIO_DIR.mkdir(parents=True, exist_ok=True)
    RAW_AUDIO_DIR.mkdir(parents=True, exist_ok=True)
    manifest = load_manifest()

    created = updated = skipped = failed = 0
    for cat in selected:
        cat_id = cat["id"]
        for sub in cat.get("subcategories", []):
            sub_id = sub["id"]
            if subcategory_id is not None and sub_id != subcategory_id:
                continue
            if not sub.get("hasAudioAr", False):
                continue

            key = f"{cat_id}_{sub_id}"
            title = sub.get("nameAr", "")
            content = html_to_plain_text(sub.get("contentAr", "") or sub.get("content", ""))
            text = f"{title}. {content}"
            digest = text_hash(text)

            app_ogg = APP_AUDIO_DIR / f"{key}.ogg"
            raw_ogg = RAW_AUDIO_DIR / f"{key}.ogg"

            existed = app_ogg.exists() or raw_ogg.exists()
            if not force and manifest.get(key) == digest and app_ogg.exists() and raw_ogg.exists():
                skipped += 1
                continue

            action = "update" if existed else "create"
            print(f"[{action}] {key}.ogg ...", end=" ", flush=True)
            try:
                # Generate into Raw/audio (packaged by the app), then copy to Resources/Audio
                await generate_one(text, raw_ogg)
                shutil.copy2(raw_ogg, app_ogg)
                manifest[key] = digest
                size_kb = raw_ogg.stat().st_size // 1024
                print(f"OK ({size_kb} KB)")
                if existed:
                    updated += 1
                else:
                    created += 1
            except Exception as exc:  # noqa: BLE001 - keep going on TTS failures
                print(f"FAILED: {exc}")
                failed += 1

    save_manifest(manifest)
    print()
    print(f"Created: {created}  Updated: {updated}  Skipped (unchanged): {skipped}  Failed: {failed}")
    print(f"Audio copied to: {APP_AUDIO_DIR}")
    print(f"App-packaged copy: {RAW_AUDIO_DIR}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate TTS audio per category.")
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--category", type=int, help="Category id from categories.json")
    group.add_argument("--all", action="store_true", help="Process every category")
    parser.add_argument("--subcategory", type=int, help="Only this subcategory id")
    parser.add_argument("--force", action="store_true", help="Regenerate even if unchanged")
    args = parser.parse_args()

    asyncio.run(run(args.category, args.subcategory, args.all, args.force))


if __name__ == "__main__":
    main()
