# Quick Audio Generator using Edge-TTS (Microsoft)
# This is the RECOMMENDED option - best quality, free, no API key needed

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Dalil AlHaj Audio Generator" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if edge-tts is installed
Write-Host "Checking dependencies..." -ForegroundColor Yellow
$edgeTtsCheck = python -m pip show edge-tts 2>$null
if (-not $edgeTtsCheck) {
    Write-Host "Installing edge-tts (Microsoft Edge Text-to-Speech)..." -ForegroundColor Yellow
    python -m pip install edge-tts
}

Write-Host "✓ Dependencies ready" -ForegroundColor Green
Write-Host ""

# Change to the directory with categories.json
Set-Location "d:\Ai workspace\Dalil Alhaj\DalilAlHaj\Resources\Raw"

Write-Host "Generating audio files..." -ForegroundColor Yellow
Write-Host ""

# Run the Python script inline
python -c @"
import json
import asyncio
import edge_tts
from pathlib import Path

async def generate_all():
    # Load categories
    with open('categories.json', 'r', encoding='utf-8') as f:
        categories = json.load(f)
    
    # Create audio folder
    audio_dir = Path('audio')
    audio_dir.mkdir(exist_ok=True)
    
    # Use Saudi male voice (clear and formal)
    VOICE = 'ar-SA-HamedNeural'
    
    total = sum(len(cat.get('subcategories', [])) for cat in categories)
    count = 0
    
    for category in categories:
        cat_id = category['id']
        for subcategory in category.get('subcategories', []):
            sub_id = subcategory['id']
            count += 1
            
            # Combine title and content
            title = subcategory.get('nameAr', '')
            content = subcategory.get('content', '')
            full_text = f'{title}. {content}'
            
            # Generate audio
            filename = f'{cat_id}_{sub_id}.mp3'
            filepath = audio_dir / filename
            
            print(f'[{count}/{total}] Generating {filename}...')
            
            try:
                communicate = edge_tts.Communicate(full_text, VOICE)
                await communicate.save(str(filepath))
                print(f'  ✓ Created {filename}')
            except Exception as e:
                print(f'  ✗ Failed: {e}')
    
    print(f'\n✓ Successfully generated {count} audio files!')
    print(f'Location: {audio_dir.absolute()}')

asyncio.run(generate_all())
"@

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "Audio generation complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Audio files are in: DalilAlHaj\Resources\Raw\audio\" -ForegroundColor Cyan
Write-Host "Now rebuild your app to include the audio files." -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
