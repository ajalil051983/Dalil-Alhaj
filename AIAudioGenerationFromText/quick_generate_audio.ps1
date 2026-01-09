# Simple Audio Generator - Just run this!
# Uses Microsoft Edge TTS (free, no API key, excellent quality)

Write-Host "`n🎤 Generating Arabic audio for all subcategories...`n" -ForegroundColor Cyan

# Install edge-tts if needed
pip install edge-tts --quiet

# Generate audio files
cd "d:\Ai workspace\Dalil Alhaj\DalilAlHaj\Resources\Raw"

python -c @"
import json, asyncio, edge_tts
from pathlib import Path

async def gen():
    with open('categories.json', 'r', encoding='utf-8') as f:
        cats = json.load(f)
    Path('audio').mkdir(exist_ok=True)
    n = 0
    for cat in cats:
        for sub in cat.get('subcategories', []):
            n += 1
            file = f'audio/{cat["id"]}_{sub["id"]}.mp3'
            text = f'{sub.get("nameAr", "")}. {sub.get("content", "")}'
            print(f'{n}. {file}')
            await edge_tts.Communicate(text, 'ar-SA-HamedNeural').save(file)
    print(f'\n✅ Done! Generated {n} files in audio/ folder')

asyncio.run(gen())
"@

Write-Host "`n✅ All audio files generated!`n" -ForegroundColor Green
