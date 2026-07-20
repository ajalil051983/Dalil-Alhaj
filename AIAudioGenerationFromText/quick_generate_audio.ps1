# Quick Audio Generator - Just run this!
# Uses Microsoft Edge TTS (free, no API key, excellent quality)
# Output: OGG Vorbis (.ogg) — 30-50% smaller than MP3, native Android support
#
# Prerequisites (run once):
#   pip install edge-tts
#   winget install ffmpeg          # needed for MP3→OGG conversion

Set-Location "$PSScriptRoot"

Write-Host "`nGenerating Arabic audio (OGG Vorbis) for all subcategories...`n" -ForegroundColor Cyan

# Install Python dependencies if needed
pip install edge-tts --quiet

python -c @"
import json, asyncio, edge_tts, subprocess, shutil, html, re
from pathlib import Path

VOICE = 'ar-SA-HamedNeural'   # Male Saudi voice - formal and clear

def html_to_plain_text(content):
    content = re.sub(r'<\s*br\s*/?\s*>', '\n', content, flags=re.IGNORECASE)
    content = re.sub(r'</\s*(p|h[1-6]|li|aside|div)\s*>', '. ', content, flags=re.IGNORECASE)
    content = re.sub(r'<[^>]+>', '', content)
    content = html.unescape(content)
    return re.sub(r'\s+', ' ', content).strip()

async def gen():
    if not shutil.which('ffmpeg'):
        print('ERROR: ffmpeg not found. Run: winget install ffmpeg')
        return
    with open('categories.json', 'r', encoding='utf-8') as f:
        cats = json.load(f)
    audio_dir = Path('audio')
    audio_dir.mkdir(exist_ok=True)
    n = 0
    for cat in cats:
        for sub in cat.get('subcategories', []):
            if not sub.get('hasAudioAr', False):
                continue
            n += 1
            tmp_mp3 = audio_dir / f'{cat["id"]}_{sub["id"]}_tmp.mp3'
            ogg_file = audio_dir / f'{cat["id"]}_{sub["id"]}.ogg'
            content = html_to_plain_text(sub.get("contentAr", "") or sub.get("content", ""))
            text = f'{sub.get("nameAr", "")}. {content}'
            print(f'{n}. {ogg_file.name}')
            try:
                await edge_tts.Communicate(text, VOICE).save(str(tmp_mp3))
                subprocess.run(['ffmpeg','-y','-i',str(tmp_mp3),'-c:a','libvorbis','-q:a','4',str(ogg_file)],
                               check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                tmp_mp3.unlink()
            except Exception as e:
                print(f'  FAILED: {e}')
                if tmp_mp3.exists():
                    tmp_mp3.unlink()
    print(f'\nDone! Generated {n} OGG files in audio/ folder')

asyncio.run(gen())
"@

Write-Host "`nAll OGG audio files generated!" -ForegroundColor Green
Write-Host "Copy the audio/ folder to: KhayratAlhaj/Resources/Raw/audio/" -ForegroundColor Yellow
