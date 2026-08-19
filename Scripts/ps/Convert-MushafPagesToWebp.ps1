# Builds the mushaf page package (warsh-pages-604.zip) for the GitHub Release.
# The loose WebP files are NOT kept in the repo — this script converts the source
# PNGs to a temp folder, zips them straight to Generated/warsh-pages-604.zip, and
# cleans up the temp files. To publish: upload the zip to the quran-pages-v1
# Release (overwriting the existing asset).
# Not part of the regular content pipeline (source images live outside the repo).
param(
    [string]$SourceFolder = 'C:\shared AJ\quran\quranSurat',
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'

$cwebp = (Get-ChildItem "$env:TEMP\libwebp" -Recurse -Filter cwebp.exe -ErrorAction SilentlyContinue | Select-Object -First 1).FullName
if (-not $cwebp) { throw "cwebp.exe not found under $env:TEMP\libwebp" }

# Stage conversion in a temp folder; only the final zip is written to the repo.
$tempPages = Join-Path $env:TEMP ("warsh-pages-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempPages -Force | Out-Null
$zipPath = Join-Path $RepoRoot 'Generated\warsh-pages-604.zip'
New-Item -ItemType Directory -Path (Split-Path $zipPath) -Force | Out-Null

try {
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
        $target = Join-Path $tempPages "$num.webp"
        $ps = [powershell]::Create()
        $ps.RunspacePool = $pool
        [void]$ps.AddScript($scriptBlock).AddArgument($cwebp).AddArgument($png.FullName).AddArgument($target).AddArgument($failed)
        $handles += [pscustomobject]@{ Pipe = $ps; Handle = $ps.BeginInvoke() }
    }
    foreach ($h in $handles) { $h.Pipe.EndInvoke($h.Handle); $h.Pipe.Dispose() }
    $pool.Close(); $pool.Dispose()

    if ($failed.Count -gt 0) { throw "Conversion failed for $($failed.Count) pages: $($failed -join ', ')" }

    # Zip with the warsh/pages/ prefix the app's installer expects. Entry names MUST use
    # forward slashes: ZipFile.CreateFromDirectory on Windows writes backslashes, which
    # Android's extractor does not treat as directory separators (breaks FindPagesDirectory).
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $fs = [System.IO.File]::Open($zipPath, [System.IO.FileMode]::Create)
    try {
        $archive = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($webp in (Get-ChildItem (Join-Path $tempPages '*.webp') | Sort-Object Name)) {
                $entryName = "warsh/pages/$($webp.Name)"   # forward slash, required
                [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $webp.FullName, $entryName, [System.IO.Compression.CompressionLevel]::NoCompression) | Out-Null
            }
        } finally {
            $archive.Dispose()
        }
    } finally {
        $fs.Dispose()
    }

    $zipMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-Output "pages=604 zipMB=$zipMB zip=$zipPath"
    Write-Output "Next: gh release upload quran-pages-v1 `"$zipPath`" --repo ajalil051983/Dalil-Alhaj --clobber"
} finally {
    Remove-Item $tempPages -Recurse -Force -ErrorAction SilentlyContinue
}
