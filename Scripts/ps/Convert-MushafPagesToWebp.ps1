# One-off conversion: source mushaf PNGs -> WebP q95 for the app package zip.
# Not part of the regular content pipeline (source images live outside the repo).
param(
    [string]$SourceFolder = 'C:\shared AJ\quran\quranSurat',
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'

$cwebp = (Get-ChildItem "$env:TEMP\libwebp" -Recurse -Filter cwebp.exe -ErrorAction SilentlyContinue | Select-Object -First 1).FullName
if (-not $cwebp) { throw "cwebp.exe not found under $env:TEMP\libwebp" }

$outDir = Join-Path $RepoRoot 'Generated\quran_package\warsh\pages'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$pngs = Get-ChildItem (Join-Path $SourceFolder 'page_*.png') | Sort-Object Name
if ($pngs.Count -ne 604) { Write-Warning "Expected 604 pages, found $($pngs.Count)" }

$failed = [System.Collections.Concurrent.ConcurrentBag[string]]::new()
$pool = [runspacefactory]::CreateRunspacePool(1, 8)
$pool.Open()
$scriptBlock = {
    param($exe, $src, $dst, $bag)
    # Lossless: these mushaf pages (flat colors + paper texture) compress better
    # losslessly than with -q 95, which was both larger AND lower quality.
    & $exe -quiet -lossless -m 6 -noalpha $src -o $dst 2>$null
    if (-not (Test-Path $dst)) { $bag.Add($src) }
}
$handles = @()
foreach ($png in $pngs) {
    $num = $png.BaseName -replace 'page_', ''
    $target = Join-Path $outDir "$num.webp"
    if (Test-Path $target) { continue }  # resume support
    $ps = [powershell]::Create()
    $ps.RunspacePool = $pool
    [void]$ps.AddScript($scriptBlock).AddArgument($cwebp).AddArgument($png.FullName).AddArgument($target).AddArgument($failed)
    $handles += [pscustomobject]@{ Pipe = $ps; Handle = $ps.BeginInvoke() }
}
foreach ($h in $handles) { $h.Pipe.EndInvoke($h.Handle); $h.Pipe.Dispose() }
$pool.Close(); $pool.Dispose()

$done = Get-ChildItem (Join-Path $outDir '*.webp')
$totalMB = [math]::Round(($done | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Output "converted=$($done.Count) failed=$($failed.Count) totalMB=$totalMB outDir=$outDir"
if ($failed) { $failed | ForEach-Object { Write-Output "FAILED: $_" } }
