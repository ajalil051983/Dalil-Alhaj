<#
.SYNOPSIS
    Generates TTS audio for one category (or all) and copies it to Resources/Audio.

.DESCRIPTION
    Wrapper around Scripts/python/generate_category_audio.py (Edge-TTS engine).
    Creates missing .ogg files, updates files whose narration text changed,
    skips unchanged files (SHA-256 manifest), and copies the result to
    KhayratAlhaj/Resources/Audio and KhayratAlhaj/Resources/Raw/audio.

.PARAMETER Category
    Category id from categories.json to (re)generate.

.PARAMETER All
    Process every category.

.PARAMETER Subcategory
    Limit generation to a single subcategory id.

.PARAMETER Force
    Regenerate even when the narration text is unchanged.

.EXAMPLE
    .\Generate-Audio.ps1 -Category 1
    .\Generate-Audio.ps1 -Category 1 -Subcategory 102 -Force
    .\Generate-Audio.ps1 -All
#>
param(
    [int]$Category,
    [switch]$All,
    [int]$Subcategory,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

if (-not $All -and $Category -eq 0) {
    Write-Error "Provide -Category <id> or -All."
    exit 1
}

$WorkspaceRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Script = Join-Path $WorkspaceRoot "Scripts\python\generate_category_audio.py"

Write-Host "Checking dependencies..." -ForegroundColor Yellow
if (-not (python -m pip show edge-tts 2>$null)) {
    Write-Host "Installing edge-tts..." -ForegroundColor Yellow
    python -m pip install edge-tts
}
if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
    Write-Error "ffmpeg not found. Install it: winget install ffmpeg"
    exit 1
}

$pyArgs = @($Script)
if ($All) { $pyArgs += "--all" } else { $pyArgs += @("--category", $Category) }
if ($Subcategory) { $pyArgs += @("--subcategory", $Subcategory) }
if ($Force) { $pyArgs += "--force" }

Write-Host "Generating audio..." -ForegroundColor Cyan
python @pyArgs

Write-Host "`nDone. Audio is in KhayratAlhaj\Resources\Audio" -ForegroundColor Green
