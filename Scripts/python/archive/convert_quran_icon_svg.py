"""One-off: convert the traced Quran SVG into the app icon.

- Removes the full-canvas background rectangle (fill #B9BBA3).
- Sets icon-friendly sizing: width/height 512 with the original viewBox.
- Writes the result to KhayratAlhaj/Resources/Images/quran_icon.svg.
"""

import re
import sys
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

ROOT = Path(__file__).resolve().parents[3]  # Scripts/python/archive/ -> repo root
SRC = Path.home() / "Downloads" / "Capture décran 2026-09-03 180813.svg"
DST = ROOT / "KhayratAlhaj" / "Resources" / "Images" / "quran_icon.svg"

svg = SRC.read_text(encoding="utf-8")

paths = re.findall(r"<path[^>]*/>", svg)
print(f"paths total: {len(paths)}")
bg = re.findall(r'<path[^>]*fill="#B9BBA3"[^>]*/>', svg)
print(f"background paths (#B9BBA3): {len(bg)}")
for b in bg:
    print("  ", b[:120])

# Remove the background rect(s)
svg = re.sub(r'<path[^>]*fill="#B9BBA3"[^>]*/>\n?', "", svg, count=len(bg))

# Icon-friendly sizing: keep the artwork's own coordinate space as viewBox,
# present at 512 on the longest side (height stays proportional).
svg = re.sub(
    r'<svg version="1\.1" xmlns="http://www\.w3\.org/2000/svg" width="498" height="348">',
    '<svg xmlns="http://www.w3.org/2000/svg" width="512" height="358" viewBox="0 0 498 348">',
    svg,
)

DST.write_text(svg, encoding="utf-8")
print(f"written: {DST} ({DST.stat().st_size} bytes)")
