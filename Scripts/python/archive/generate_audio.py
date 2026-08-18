"""
Audio Generator for Khayrat AlHaj App
Generates Arabic audio narration for all subcategories using free TTS APIs.

Output format: OGG Vorbis (.ogg)
  - Natively supported on Android from API 1 (no extra codec needed)
  - 30-50% smaller than equivalent MP3 at same perceived quality
  - Fully supported by CommunityToolkit.Maui MediaElement on Android

Workflow (Options 1 & 2):
  edge-tts / gTTS  →  temp .mp3  →  ffmpeg convert  →  .ogg  →  delete temp

Requirements:
    pip install gtts
    pip install edge-tts
    # ffmpeg on PATH for OGG encoding:  winget install ffmpeg
"""

import json
import os
import asyncio
import html
import re
import subprocess
import shutil
from pathlib import Path

# ── Absolute paths ─────────────────────────────────────────────────────────
CATEGORIES_JSON = Path(r'd:\Ai workspace\Khayrat Alhaj\AIAudioGenerationFromText\categories.json')
AUDIO_OUTPUT_DIR = Path(r'D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\Resources\Raw\audio')
# ───────────────────────────────────────────────────────────────────────────


def _require_ffmpeg() -> bool:
    """Return True if ffmpeg is available on PATH."""
    if shutil.which("ffmpeg") is None:
        print("ffmpeg not found. Install it: winget install ffmpeg")
        return False
    return True


def _mp3_to_ogg(mp3_path: Path, ogg_path: Path) -> None:
    """Convert an MP3 file to OGG Vorbis via ffmpeg and remove the source MP3."""
    subprocess.run(
        ["ffmpeg", "-y", "-i", str(mp3_path),
         "-c:a", "libvorbis", "-q:a", "4",
         str(ogg_path)],
        check=True,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    mp3_path.unlink()


def validate_audio_flags(categories):
    """Ensure all subcategories explicitly declare per-language audio flags."""
    for category in categories:
        cat_id = category.get("id")
        for subcategory in category.get("subcategories", []):
            sub_id = subcategory.get("id")
            missing = [
                key
                for key in ("hasAudioAr", "hasAudioEn", "hasAudioFr")
                if key not in subcategory
            ]
            if missing:
                raise ValueError(
                    f"Subcategory {cat_id}_{sub_id} is missing audio flags: {', '.join(missing)}"
                )


def _html_to_plain_text(content: str) -> str:
    content = re.sub(r"<\s*br\s*/?\s*>", "\n", content, flags=re.IGNORECASE)
    content = re.sub(r"</\s*(p|h[1-6]|li|aside|div)\s*>", ". ", content, flags=re.IGNORECASE)
    content = re.sub(r"<[^>]+>", "", content)
    content = html.unescape(content)
    return re.sub(r"\s+", " ", content).strip()


# ============================================================================
# OPTION 1: Using gTTS (Google Text-to-Speech) - Simplest
# ============================================================================
def generate_audio_gtts():
    """Generate OGG audio using Google TTS - Free and simple"""
    try:
        from gtts import gTTS
    except ImportError:
        print("Please install gtts: pip install gtts")
        return

    if not _require_ffmpeg():
        return

    with open('categories.json', 'r', encoding='utf-8') as f:
        categories = json.load(f)
    validate_audio_flags(categories)

    audio_dir = Path('audio')
    audio_dir.mkdir(exist_ok=True)

    count = 0
    for category in categories:
        cat_id = category['id']
        for subcategory in category.get('subcategories', []):
            sub_id = subcategory['id']

            if not subcategory.get('hasAudioAr', False):
                continue

            title = subcategory.get('nameAr', '')
            content = _html_to_plain_text(subcategory.get('contentAr', '') or subcategory.get('content', ''))
            full_text = f"{title}. {content}"

            ogg_file = audio_dir / f'{cat_id}_{sub_id}.ogg'
            tmp_mp3  = audio_dir / f'{cat_id}_{sub_id}_tmp.mp3'

            print(f"Generating {ogg_file.name}...")
            try:
                tts = gTTS(text=full_text, lang='ar', slow=False)
                tts.save(str(tmp_mp3))
                _mp3_to_ogg(tmp_mp3, ogg_file)
                count += 1
                print(f"  ✓ Created {ogg_file.name}")
            except Exception as e:
                print(f"  ✗ Failed {ogg_file.name}: {e}")
                if tmp_mp3.exists():
                    tmp_mp3.unlink()

    print(f"\n✓ Generated {count} OGG files in '{audio_dir}' folder")
    print("Copy these files to: KhayratAlhaj/Resources/Raw/audio/")


# ============================================================================
# OPTION 2: Using Edge-TTS (Microsoft Edge) - Best Quality  [RECOMMENDED]
# ============================================================================
async def generate_audio_edge():
    """Generate OGG audio using Microsoft Edge TTS - Excellent quality"""
    try:
        import edge_tts
    except ImportError:
        print("Please install edge-tts: pip install edge-tts")
        return

    if not _require_ffmpeg():
        return

    with open(str(CATEGORIES_JSON), 'r', encoding='utf-8') as f:
        categories = json.load(f)
    validate_audio_flags(categories)

    audio_dir = AUDIO_OUTPUT_DIR
    audio_dir.mkdir(parents=True, exist_ok=True)

    # Available Arabic voices (choose one):
    # "ar-SA-HamedNeural"   - Male,   Saudi Arabia
    # "ar-SA-ZariyahNeural" - Female, Saudi Arabia
    # "ar-EG-SalmaNeural"   - Female, Egypt
    # "ar-EG-ShakirNeural"  - Male,   Egypt
    VOICE = "ar-SA-HamedNeural"  # Male Saudi voice — formal and clear

    # Collect needed files
    needed_entries = []
    for category in categories:
        cat_id = category['id']
        for subcategory in category.get('subcategories', []):
            sub_id = subcategory['id']
            if not subcategory.get('hasAudioAr', False):
                continue
            title = subcategory.get('nameAr', '')
            content = _html_to_plain_text(subcategory.get('contentAr', '') or subcategory.get('content', ''))
            needed_entries.append((cat_id, sub_id, f"{title}. {content}"))

    # Delete old/unused audio files
    needed_names = {f'{c}_{s}.ogg' for c, s, _ in needed_entries} | {'README.txt'}
    deleted = 0
    for f in audio_dir.iterdir():
        if f.is_file() and f.name not in needed_names:
            f.unlink()
            print(f"  Deleted old: {f.name}")
            deleted += 1
    if deleted:
        print(f"  Removed {deleted} old file(s)\n")

    # Generate new audio
    count = 0
    total = len(needed_entries)
    for i, (cat_id, sub_id, full_text) in enumerate(needed_entries, 1):
        ogg_file = audio_dir / f'{cat_id}_{sub_id}.ogg'
        tmp_mp3  = audio_dir / f'{cat_id}_{sub_id}_tmp.mp3'

        print(f"[{i}/{total}] {ogg_file.name} ...", end=' ', flush=True)
        try:
            communicate = edge_tts.Communicate(full_text, VOICE)
            await communicate.save(str(tmp_mp3))
            _mp3_to_ogg(tmp_mp3, ogg_file)
            size_kb = ogg_file.stat().st_size // 1024
            count += 1
            print(f"OK  ({size_kb} KB)")
        except Exception as e:
            print(f"FAILED  {e}")
            if tmp_mp3.exists():
                tmp_mp3.unlink()

    print(f"\n  Generated {count}/{total} OGG files")
    print(f"  Location: {audio_dir}")


# ============================================================================
# OPTION 3: Using pyttsx3 - Offline (requires system TTS)
# ============================================================================
def generate_audio_pyttsx3():
    """Generate OGG audio using system TTS - Offline but quality depends on system"""
    try:
        import pyttsx3
    except ImportError:
        print("Please install pyttsx3: pip install pyttsx3")
        return

    if not _require_ffmpeg():
        return

    engine = pyttsx3.init()

    voices = engine.getProperty('voices')
    for voice in voices:
        if 'arabic' in voice.name.lower() or 'ar' in voice.languages:
            engine.setProperty('voice', voice.id)
            break

    with open('categories.json', 'r', encoding='utf-8') as f:
        categories = json.load(f)
    validate_audio_flags(categories)

    audio_dir = Path('audio')
    audio_dir.mkdir(exist_ok=True)

    count = 0
    for category in categories:
        cat_id = category['id']
        for subcategory in category.get('subcategories', []):
            sub_id = subcategory['id']

            if not subcategory.get('hasAudioAr', False):
                continue

            title = subcategory.get('nameAr', '')
            content = _html_to_plain_text(subcategory.get('contentAr', '') or subcategory.get('content', ''))
            full_text = f"{title}. {content}"

            ogg_file = audio_dir / f'{cat_id}_{sub_id}.ogg'
            tmp_mp3  = audio_dir / f'{cat_id}_{sub_id}_tmp.mp3'

            print(f"Generating {ogg_file.name}...")
            try:
                engine.save_to_file(full_text, str(tmp_mp3))
                engine.runAndWait()
                _mp3_to_ogg(tmp_mp3, ogg_file)
                count += 1
                print(f"  ✓ Created {ogg_file.name}")
            except Exception as e:
                print(f"  ✗ Failed {ogg_file.name}: {e}")
                if tmp_mp3.exists():
                    tmp_mp3.unlink()

    print(f"\n✓ Generated {count} OGG files in '{audio_dir}' folder")


# ============================================================================
# Main Menu
# ============================================================================
if __name__ == "__main__":
    import sys

    # Allow --option N from command line (used by generate_audio.ps1)
    cli_choice = None
    if "--option" in sys.argv:
        idx = sys.argv.index("--option")
        if idx + 1 < len(sys.argv):
            cli_choice = sys.argv[idx + 1]

    if cli_choice:
        choice = cli_choice
        print(f"\nRunning Option {choice}...\n")
    else:
        print("=" * 70)
        print("Audio Generator for Khayrat AlHaj  (output: OGG Vorbis)")
        print("=" * 70)
        print("\nChoose TTS engine:")
        print("1. gTTS (Google)    - Simple, free, good quality")
        print("2. Edge-TTS (Microsoft) - RECOMMENDED - Best quality, multiple voices")
        print("3. pyttsx3 (Offline)    - Requires system Arabic TTS")
        print()
        choice = input("Enter choice (1/2/3) [2]: ").strip() or "2"
        print("\nStarting audio generation...\n")

    if choice == "1":
        generate_audio_gtts()
    elif choice == "2":
        asyncio.run(generate_audio_edge())
    elif choice == "3":
        generate_audio_pyttsx3()
    else:
        print("Invalid choice")

    print("\nDone! Audio files saved to:")
    print(f"  {AUDIO_OUTPUT_DIR}")
