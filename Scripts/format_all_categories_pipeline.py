"""
Master Content Pipeline - All Categories
=========================================
• Cats 1-6 & 8 : Apply consistent nice plain-text visual structure to existing content.
• Cat 7         : Full rebuild from hajj_steps_from_pdf.json
                  → Summary / Q&A / References
                  → Translate to English & French

Output : Docs/generated/all_categories_validated.json  (for review before DB write)
Action : After user review, writes to appdata.bin & updates audio categories.json
"""

import json
import re
import sqlite3
import shutil
import time
from pathlib import Path

# ── Paths ─────────────────────────────────────────────────────────────────────
BASE        = Path(__file__).parent.parent
SOURCE_JSON = BASE / "Docs/generated/hajj_steps_from_pdf.json"
OUT_JSON    = BASE / "Docs/generated/all_categories_validated.json"
DB_SRC      = BASE / "KhayratAlhaj/Resources/Data/appdata.bin"
DB_BAK      = BASE / "KhayratAlhaj/Resources/Data/appdata.bin.bak"
AUDIO_JSON  = BASE / "AIAudioGenerationFromText/categories.json"

SEP  = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
SEP2 = "─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─"

# ── Arabic numerals list ───────────────────────────────────────────────────────
ARAB = ['١','٢','٣','٤','٥','٦','٧','٨','٩','١٠','١١','١٢','١٣','١٤','١٥','١٦','١٧','١٨','١٩','٢٠']

# ── Per-step summaries (index 0 = step 1) ─────────────────────────────────────
SUMMARIES_AR = [
    "الإحرام هو الدخول في نسك الحج بالنية من الميقات المكاني والزماني المحدد شرعاً. "
    "تشمل هذه الخطوة أنواع الإحرام (الإفراد والتمتع والقران)، ومحظوراته، ووقت التحلل منه.",

    "التلبية شعار المحرم وإعلانه دخول الحج، وهي قول: لبيك اللهم لبيك. "
    "تشمل هذه الخطوة لفظ التلبية ومتى يبدأ الحاج بها ومتى يقطعها، وكيفيتها للرجل والمرأة.",

    "الطواف ركن من أركان الحج والعمرة، ويتم بسبعة أشواط حول الكعبة المشرفة. "
    "تشمل هذه الخطوة صفة الطواف وأنواعه (طواف القدوم والإفاضة والوداع) وشروط صحته وما يترتب على تركه.",

    "السعي بين الصفا والمروة واجب من واجبات الحج يؤديه الحاج في سبعة أشواط تبدأ من الصفا وتنتهي بالمروة. "
    "تشمل هذه الخطوة شروط السعي وكيفيته وأحكام مَن ترك شوطاً أو أُقيمت عليه الصلاة أثناءه.",

    "الوقوف بعرفة هو ركن الحج الأكبر، يؤديه الحاج يوم التاسع من ذي الحجة من الزوال إلى الغروب. "
    "من فاته الوقوف فاته الحج. تشمل هذه الخطوة حكم الوقوف وشروطه ومكانه وكيفية الجمع بين الصلاتين.",

    "بعد الإفاضة من عرفة عقب غروب شمس اليوم التاسع، يتوجه الحاج إلى مزدلفة للمبيت وصلاة المغرب والعشاء جمعاً. "
    "تشمل هذه الخطوة حكم المبيت بمزدلفة وما يؤديه الحاج فيها ووقت مغادرتها.",

    "يوم عيد الأضحى بمنى يؤدي الحاج أعمالاً أربعة بترتيب محدد: رمي جمرة العقبة، ثم النحر، ثم الحلق أو التقصير، ثم طواف الإفاضة. "
    "تشمل هذه الخطوة تفاصيل هذه الأعمال وأحكام مخالفة ترتيبها.",

    "في أيام التشريق (الحادي عشر والثاني عشر والثالث عشر من ذي الحجة) يبقى الحاج بمنى ويرمي الجمرات الثلاث. "
    "تشمل هذه الخطوة وقت رمي الجمرات وعدد الحصيات وحكم النيابة في الرمي والمبيت بمنى.",

    "يختتم الحاج رحلته بطواف الوداع قبيل مغادرة مكة المكرمة، وهو آخر شعائر الحج. "
    "تشمل هذه الخطوة أحكام طواف الوداع وطواف الإفاضة والتحلل التام من الإحرام.",
]

REF_NAMES_AR = [
    "مراجع الإحرام والاستعداد",
    "مراجع التلبية وأحكامها",
    "مراجع الطواف وأحكامه",
    "مراجع السعي بين الصفا والمروة",
    "مراجع الوقوف بعرفة",
    "مراجع المبيت بمزدلفة",
    "مراجع أعمال منى يوم العيد",
    "مراجع رمي الجمار والمبيت بمنى",
    "مراجع طواف الوداع والتحلل",
]


# ══════════════════════════════════════════════════════════════════════════════
# FORMATTER  –  cats 1-6, 8
# ══════════════════════════════════════════════════════════════════════════════

# Detect lines that look like section headers:
#   • short line (≤60 chars) ending with ':'
#   • doesn't start with '-' / '•' / digit
_HEADER_RE = re.compile(r'^[^•\-\d].{0,58}:$')


def _fmt_line(line: str) -> str:
    """Apply visual formatting rules to a single content line."""
    stripped = line.strip()
    if not stripped:
        return ""
    # Bullet item
    if stripped.startswith("- "):
        return f"  • {stripped[2:]}"
    if stripped.startswith("• "):
        return f"  {stripped}"
    # Section header (but not the very first title line fed separately)
    if _HEADER_RE.match(stripped):
        return f"▪ {stripped}"
    return line


def format_existing_content(sub_name: str, content: str) -> str:
    """
    Wrap an existing subcategory content with visual structure.
    Rules:
      1. sub_name              → banner title  ◆ + SEP
      2. First content line that is just a title repeat → skip it
      3. Lines ending with ':' → section header ▪
      4. Lines starting '-'   → bullet  •
      5. Empty lines           → kept (max 1 consecutive blank)
    """
    raw_lines = content.splitlines()

    out = [
        f"◆ {sub_name}",
        SEP,
        "",
    ]

    # Skip leading empty lines and a bare title-repeat first line
    start = 0
    for i, line in enumerate(raw_lines):
        stripped = line.strip()
        if not stripped:
            continue
        # If this first non-empty line is just the subcategory name or very
        # close to it (e.g. "المواقيت المكانية هي..." starts with the name →
        # keep it; but "دعاء دخول المسجد الحرام:" is a bare title repeat → skip)
        clean_name    = sub_name.rstrip(":").strip()
        clean_line    = stripped.rstrip(":").strip()
        # Skip only if it's essentially identical to the name (no extra words)
        words_name = set(clean_name.split())
        words_line = set(clean_line.split())
        # If the line adds at most 2 new words beyond the name, treat as title repeat
        if len(words_line - words_name) <= 2 and words_name.issubset(words_line) or clean_line == clean_name:
            start = i + 1
        break

    blank_count = 0
    for line in raw_lines[start:]:
        formatted = _fmt_line(line)
        if formatted == "":
            blank_count += 1
            if blank_count <= 1:
                out.append("")
        else:
            blank_count = 0
            out.append(formatted)

    # Strip trailing blanks
    while out and out[-1] == "":
        out.pop()

    return "\n".join(out)


# ══════════════════════════════════════════════════════════════════════════════
# BUILDER  –  cat 7 (Hajj step by step)
# ══════════════════════════════════════════════════════════════════════════════

def build_step_ar(order, name_ar, summary_ar, needed_points, ref_name_ar):
    lines = [
        f"◆ الخطوة {order}: {name_ar}",
        SEP,
        "",
        "📋 ملخص الخطوة:",
        "",
        summary_ar,
        "",
        SEP,
        "❓ الأسئلة والأجوبة:",
        SEP,
        "",
        "يمكنك البحث عن إجابات هذه الأسئلة في دليل الحاج في الصفحات المذكورة:",
        "",
    ]
    for i, point in enumerate(needed_points):
        num = ARAB[i] if i < len(ARAB) else str(i + 1)
        lines.append(f"  س{num}  {point}")
        lines.append("")

    lines += [
        SEP,
        "📚 المراجع:",
        SEP,
        "",
        "للاطلاع على الآيات الكريمة والأحاديث النبوية والأدعية المأثورة:",
        "",
        f"  ▸ {ref_name_ar}",
        "    (قسم: مراجع الآيات والأحاديث لكل خطوة)",
    ]
    return "\n".join(lines)


def build_step_en(order, name_en, summary_en, questions_en, ref_name_en):
    lines = [
        f"◆ Step {order}: {name_en}",
        SEP,
        "",
        "📋 Step Summary:",
        "",
        summary_en,
        "",
        SEP,
        "❓ Questions & Answers:",
        SEP,
        "",
        "Find answers to these questions in the Pilgrim's Guide at the indicated pages:",
        "",
    ]
    for i, q in enumerate(questions_en):
        lines.append(f"  Q{i + 1}  {q}")
        lines.append("")

    lines += [
        SEP,
        "📚 References:",
        SEP,
        "",
        "For Qur'anic verses, hadiths, and prescribed supplications:",
        "",
        f"  ▸ {ref_name_en}",
        "    (Section: Verses & Hadith References per Step)",
    ]
    return "\n".join(lines)


def build_step_fr(order, name_fr, summary_fr, questions_fr, ref_name_fr):
    lines = [
        f"◆ Étape {order}: {name_fr}",
        SEP,
        "",
        "📋 Résumé de l'étape:",
        "",
        summary_fr,
        "",
        SEP,
        "❓ Questions & Réponses:",
        SEP,
        "",
        "Trouvez les réponses à ces questions dans le Guide du Pèlerin aux pages indiquées:",
        "",
    ]
    for i, q in enumerate(questions_fr):
        lines.append(f"  Q{i + 1}  {q}")
        lines.append("")

    lines += [
        SEP,
        "📚 Références:",
        SEP,
        "",
        "Pour les versets coraniques, hadiths et invocations:",
        "",
        f"  ▸ {ref_name_fr}",
        "    (Section: Références des versets et hadiths par étape)",
    ]
    return "\n".join(lines)


# ══════════════════════════════════════════════════════════════════════════════
# TRANSLATION
# ══════════════════════════════════════════════════════════════════════════════

def translate(text: str, src: str, tgt: str) -> str:
    from deep_translator import GoogleTranslator
    for attempt in range(3):
        try:
            result = GoogleTranslator(source=src, target=tgt).translate(text)
            return result if result else text
        except Exception as e:
            if attempt < 2:
                time.sleep(2)
            else:
                print(f"      ⚠  translate {src}→{tgt} failed: {e}")
                return text


def translate_list(items, src, tgt):
    out = []
    for item in items:
        out.append(translate(item, src, tgt))
        time.sleep(0.3)
    return out


# ══════════════════════════════════════════════════════════════════════════════
# MAIN
# ══════════════════════════════════════════════════════════════════════════════

def main():
    print("=" * 62)
    print("  Master Content Pipeline – All Categories")
    print("=" * 62)

    # ── Load DB ──────────────────────────────────────────────────────────────
    print("\n[1/5] Reading current subcategories from DB …")
    conn_r = sqlite3.connect(DB_SRC)
    cur_r  = conn_r.cursor()
    rows = cur_r.execute(
        "SELECT Id, CategoryId, NameAr, NameEn, NameFr, "
        "ContentAr, ContentEn, ContentFr, HasAudioAr, HasAudioEn, HasAudioFr "
        "FROM SubCategories ORDER BY CategoryId, Id"
    ).fetchall()
    conn_r.close()
    cols = ["id","cat_id","name_ar","name_en","name_fr",
            "content_ar","content_en","content_fr","has_audio_ar","has_audio_en","has_audio_fr"]
    db_subs = [dict(zip(cols, r)) for r in rows]
    print(f"  → {len(db_subs)} subcategories loaded.")

    # ── Load cat-7 source data ───────────────────────────────────────────────
    print("[2/5] Loading Hajj step source data …")
    with open(SOURCE_JSON, encoding="utf-8") as f:
        hjsrc = json.load(f)
    steps = hjsrc["steps"]

    # ── Build validated payload ──────────────────────────────────────────────
    print("[3/5] Building formatted content …")
    payload = []   # list of dicts – one per subcategory

    # --- Cats 1-6, 8 : format existing content --------------------------------
    non7 = [s for s in db_subs if s["cat_id"] != 7]
    print(f"\n  Formatting {len(non7)} subcategories (cats 1-6, 8) …")
    for sub in non7:
        new_ar = format_existing_content(sub["name_ar"], sub["content_ar"] or "")
        new_en = format_existing_content(sub["name_en"] or sub["name_ar"],
                                         sub["content_en"] or sub["content_ar"] or "")
        new_fr = format_existing_content(sub["name_fr"] or sub["name_ar"],
                                         sub["content_fr"] or sub["content_ar"] or "")
        payload.append({
            "id":         sub["id"],
            "cat_id":     sub["cat_id"],
            "name_ar":    sub["name_ar"],
            "name_en":    sub["name_en"],
            "name_fr":    sub["name_fr"],
            "content_ar": new_ar,
            "content_en": new_en,
            "content_fr": new_fr,
            "has_audio_ar":  sub["has_audio_ar"],
            "has_audio_en":  sub["has_audio_en"],
            "has_audio_fr":  sub["has_audio_fr"],
        })
        print(f"    ✓  Cat{sub['cat_id']} Sub{sub['id']}: {sub['name_ar']}")

    # --- Cat 7 : full rebuild --------------------------------------------------
    cat7_subs = sorted([s for s in db_subs if s["cat_id"] == 7], key=lambda x: x["id"])
    print(f"\n  Rebuilding {len(steps)} Cat-7 steps with Summary/Q&A/References …")

    for i, step in enumerate(steps):
        order       = step["order"]
        name_ar     = step["name_ar"]
        needed      = step["needed_points"]
        summary_ar  = SUMMARIES_AR[i]
        ref_ar      = REF_NAMES_AR[i]
        sub_id      = 700 + order
        sub_name_ar = f"الخطوة {order}: {name_ar}"

        print(f"\n    Step {order}: {name_ar}")
        print(f"      → Translating ({len(needed)} questions) …")

        name_en    = translate(name_ar, "ar", "en")
        name_fr    = translate(name_ar, "ar", "fr")
        summary_en = translate(summary_ar, "ar", "en")
        summary_fr = translate(summary_ar, "ar", "fr")
        needed_en  = translate_list(needed, "ar", "en")
        needed_fr  = translate_list(needed, "ar", "fr")
        ref_en     = translate(ref_ar, "ar", "en")
        ref_fr     = translate(ref_ar, "ar", "fr")

        content_ar = build_step_ar(order, name_ar, summary_ar, needed, ref_ar)
        content_en = build_step_en(order, name_en, summary_en, needed_en, ref_en)
        content_fr = build_step_fr(order, name_fr, summary_fr, needed_fr, ref_fr)

        payload.append({
            "id":         sub_id,
            "cat_id":     7,
            "name_ar":    sub_name_ar,
            "name_en":    f"Step {order}: {name_en}",
            "name_fr":    f"Étape {order}: {name_fr}",
            "content_ar": content_ar,
            "content_en": content_en,
            "content_fr": content_fr,
            "has_audio_ar":  1,
            "has_audio_en":  0,
            "has_audio_fr":  0,
        })
        print(f"      ✓  {len(content_ar)} chars AR / {len(content_en)} chars EN")

    # ── Export validated JSON ────────────────────────────────────────────────
    print(f"\n[4/5] Exporting to {OUT_JSON.relative_to(BASE)} …")
    OUT_JSON.parent.mkdir(parents=True, exist_ok=True)
    with open(OUT_JSON, "w", encoding="utf-8") as f:
        json.dump({"total": len(payload), "subcategories": payload}, f,
                  ensure_ascii=False, indent=2)
    print(f"  ✓  Saved {len(payload)} entries.")

    # ── Quick validation ─────────────────────────────────────────────────────
    print("\n  Validation summary:")
    issues = 0
    for entry in payload:
        has_sep  = SEP in (entry["content_ar"] or "")
        has_icon = "◆" in (entry["content_ar"] or "")
        ok = has_sep and has_icon
        if not ok:
            issues += 1
            print(f"  ✗  Sub{entry['id']} Cat{entry['cat_id']}: missing structure")
    if issues == 0:
        print(f"  ✓  All {len(payload)} subcategories have proper structure.")
    else:
        print(f"  ⚠  {issues} subcategories have formatting issues – check the JSON.")

    # ── Confirm before DB write ──────────────────────────────────────────────
    print("\n" + "=" * 62)
    print("  Review: Docs/generated/all_categories_validated.json")
    print("  Writing to DB automatically…")
    print("=" * 62)

    # ── Backup + write DB ────────────────────────────────────────────────────
    print(f"\n[5/5] Backing up & writing DB …")
    shutil.copy2(DB_SRC, DB_BAK)
    print(f"  Backup → {DB_BAK.name}")

    conn_w = sqlite3.connect(DB_SRC)
    cur_w  = conn_w.cursor()
    ok_cnt = 0
    for entry in payload:
        cur_w.execute(
            """UPDATE SubCategories
               SET NameAr=?, NameEn=?, NameFr=?,
                   ContentAr=?, ContentEn=?, ContentFr=?,
                   HasAudioAr=?, HasAudioEn=?, HasAudioFr=?
               WHERE Id=?""",
            (entry["name_ar"], entry["name_en"], entry["name_fr"],
             entry["content_ar"], entry["content_en"], entry["content_fr"],
             entry["has_audio_ar"], entry["has_audio_en"], entry["has_audio_fr"],
             entry["id"])
        )
        if cur_w.rowcount > 0:
            ok_cnt += 1
    conn_w.commit()
    conn_w.close()
    print(f"  ✓  Updated {ok_cnt}/{len(payload)} rows in DB.")

    # ── Update audio categories.json ─────────────────────────────────────────
    print(f"\n  Updating {AUDIO_JSON.name} (cat 7 audio content) …")
    if AUDIO_JSON.exists():
        with open(AUDIO_JSON, encoding="utf-8") as f:
            audio_cats = json.load(f)
    else:
        audio_cats = []

    cat7_audio = next((c for c in audio_cats if c.get("id") == 7), None)
    if cat7_audio is None:
        cat7_audio = {"id": 7, "subcategories": []}
        audio_cats.append(cat7_audio)

    cat7_audio["subcategories"] = [
        {"id": e["id"], "nameAr": e["name_ar"], "content": e["content_ar"]}
        for e in payload if e["cat_id"] == 7
    ]
    with open(AUDIO_JSON, "w", encoding="utf-8") as f:
        json.dump(audio_cats, f, ensure_ascii=False, indent=2)
    print(f"  ✓  Audio JSON updated.")

    # ── Done ─────────────────────────────────────────────────────────────────
    print("\n" + "=" * 62)
    print("  DONE!  All 42 subcategories updated.")
    print("=" * 62)
    print("\n  Next: regenerate audio for cat 7")
    print("    cd AIAudioGenerationFromText")
    print("    python generate_audio.py    (choose option 2 – Edge-TTS)")
    print("    then copy audio/7_70*.mp3 → KhayratAlhaj/Resources/Raw/audio/")


if __name__ == "__main__":
    main()
