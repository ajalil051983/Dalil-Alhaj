# Khayrat AlHaj — Audio Generator (Edge-TTS, Microsoft)
# Generates Arabic OGG audio for all hasAudioAr=true subcategories.
# Deletes old/unused audio files automatically.

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Khayrat AlHaj Audio Generator" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check dependencies
Write-Host "Checking dependencies..." -ForegroundColor Yellow
$edgeTtsCheck = python -m pip show edge-tts 2>$null
if (-not $edgeTtsCheck) {
    Write-Host "Installing edge-tts..." -ForegroundColor Yellow
    python -m pip install edge-tts
}
$pydubCheck = python -m pip show pydub 2>$null
if (-not $pydubCheck) {
    Write-Host "Installing pydub..." -ForegroundColor Yellow
    python -m pip install pydub
}
if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: ffmpeg not found. Install it: winget install ffmpeg" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Dependencies ready" -ForegroundColor Green
Write-Host ""

# Run the audio generator — uses absolute paths defined inside generate_audio.py
Write-Host "Generating audio files..." -ForegroundColor Yellow
Write-Host ""
python "d:\Ai workspace\Khayrat Alhaj\AIAudioGenerationFromText\generate_audio.py" --option 2

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "Audio generation complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
