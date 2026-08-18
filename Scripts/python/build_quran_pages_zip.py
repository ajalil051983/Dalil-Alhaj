import argparse
import shutil
from pathlib import Path

import fitz  # PyMuPDF
from PIL import Image


def extract_pages_to_webp(pdf_path: Path, out_pages_dir: Path, start_page: int, end_page: int, quality: int) -> int:
    out_pages_dir.mkdir(parents=True, exist_ok=True)

    doc = fitz.open(pdf_path)
    try:
        if start_page < 1 or end_page > doc.page_count or start_page > end_page:
            raise ValueError(f"Invalid page range {start_page}..{end_page} for PDF with {doc.page_count} pages")

        written = 0
        for pdf_page in range(start_page, end_page + 1):
            page_index = pdf_page - 1
            mushaf_page = pdf_page - (start_page - 1)

            page = doc.load_page(page_index)
            pix = page.get_pixmap(alpha=False)
            image = Image.frombytes("RGB", [pix.width, pix.height], pix.samples)

            out_path = out_pages_dir / f"{mushaf_page:03d}.webp"
            image.save(out_path, format="WEBP", quality=quality, method=6)
            written += 1

            if written % 25 == 0:
                print(f"Extracted {written} pages...")

        return written
    finally:
        doc.close()


def build_zip_from_folder(source_root: Path, zip_base_path: Path) -> Path:
    zip_base_path.parent.mkdir(parents=True, exist_ok=True)
    zip_path = Path(shutil.make_archive(str(zip_base_path), "zip", root_dir=source_root))
    return zip_path


def main() -> None:
    parser = argparse.ArgumentParser(description="Build Warsh Quran pages ZIP from PDF")
    parser.add_argument("--pdf", required=True, help="Path to source PDF")
    parser.add_argument("--start-page", type=int, default=7, help="PDF start page (1-based)")
    parser.add_argument("--end-page", type=int, default=610, help="PDF end page (1-based)")
    parser.add_argument("--quality", type=int, default=90, help="WebP quality (0-100)")
    parser.add_argument("--work-dir", default="Generated/quran_package", help="Working output folder")
    parser.add_argument("--zip-out", default="Generated/warsh-pages-604", help="ZIP output path without extension")
    args = parser.parse_args()

    pdf_path = Path(args.pdf).resolve()
    work_dir = Path(args.work_dir).resolve()
    package_root = work_dir / "warsh"
    pages_dir = package_root / "pages"

    if not pdf_path.exists():
        raise FileNotFoundError(f"PDF not found: {pdf_path}")

    if work_dir.exists():
        shutil.rmtree(work_dir)

    print(f"PDF: {pdf_path}")
    print(f"Extracting pages {args.start_page}..{args.end_page} -> {pages_dir}")
    count = extract_pages_to_webp(pdf_path, pages_dir, args.start_page, args.end_page, args.quality)
    print(f"Done. Extracted {count} pages.")

    if count != 604:
        print(f"WARNING: expected 604 pages, got {count}")

    zip_path = build_zip_from_folder(work_dir, Path(args.zip_out).resolve())
    print(f"ZIP created: {zip_path}")


if __name__ == "__main__":
    main()
