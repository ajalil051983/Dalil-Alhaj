"""
Remove the 'Conditions Making Hajj Obligatory' section from subcategory 101
(already covered by subcategory 104 - Required Documents and Vaccinations).
Updates categories.json, appdata.bin, then regenerates 1_101.ogg audio.
"""
import json, sqlite3, asyncio, tempfile, os, re, subprocess
import edge_tts
from pathlib import Path

JSON_FILE = r'd:\Ai workspace\Khayrat Alhaj\AIAudioGenerationFromText\categories.json'
DB_PATH   = r'D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\Resources\Data\appdata.bin'
AUDIO_DIR = r'D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\Resources\Raw\audio'
VOICE     = 'ar-SA-HamedNeural'

# ── Sections to strip from each language ─────────────────────────────────────
STRIP_AR = '<h4>شروط وجوب الحج</h4><ul><li>الإسلام.</li><li>العقل والبلوغ.</li><li>الاستطاعة المالية والصحية.</li><li>الأمن على النفس والدين.</li><li>المحرم للمرأة (الزوج أو المحرم)، أو المجموعة الموثوقة عند بعض الفقهاء.</li></ul>'
STRIP_EN = "<h4>Conditions Making Hajj Obligatory</h4><ul><li>Islam.</li><li>Sanity and puberty.</li><li>Financial and physical capability.</li><li>Safety for oneself and one's religion.</li><li>A mahram for women (husband or male guardian), or at minimum a trustworthy group according to some scholars.</li></ul>"
STRIP_FR = "<h4>Conditions rendant le Hajj obligatoire</h4><ul><li>L'islam.</li><li>La raison et la puberté.</li><li>La capacité financière et physique.</li><li>La sécurité pour soi et sa religion.</li><li>Un mahram pour la femme (mari ou tuteur masculin), ou au minimum un groupe de confiance selon certains savants.</li></ul>"

# ── 1. Update categories.json ─────────────────────────────────────────────────
with open(JSON_FILE, 'r', encoding='utf-8') as f:
    data = json.load(f)

new_ar = new_en = new_fr = None
for cat in data:
    for sub in cat.get('subcategories', []):
        if sub['id'] == 101:
            sub['contentAr'] = sub['contentAr'].replace(STRIP_AR, '')
            sub['contentEn'] = sub['contentEn'].replace(STRIP_EN, '')
            sub['contentFr'] = sub['contentFr'].replace(STRIP_FR, '')
            new_ar, new_en, new_fr = sub['contentAr'], sub['contentEn'], sub['contentFr']
            print('✓ Stripped conditions section from categories.json (101)')

with open(JSON_FILE, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=2)

# ── 2. Update appdata.bin ─────────────────────────────────────────────────────
conn = sqlite3.connect(DB_PATH)
conn.execute(
    "UPDATE SubCategories SET ContentAr=?, ContentEn=?, ContentFr=? WHERE Id=101",
    (new_ar, new_en, new_fr)
)
conn.commit()
conn.close()
print('✓ Updated appdata.bin (101)')

# ── 3. Regenerate 1_101.ogg ───────────────────────────────────────────────────
def strip_html(html: str) -> str:
    html = html.replace('&nbsp;', ' ').replace('&amp;', 'و')
    html = re.sub(r'</(h[1-6]|p|li|tr|td|th)>', '. ', html, flags=re.IGNORECASE)
    clean = re.sub(r'<[^>]+>', ' ', html)
    clean = re.sub(r'[\s.]+', ' ', clean)
    return clean.strip('. ')

async def regen():
    # Build title + clean content for TTS
    with open(JSON_FILE, 'r', encoding='utf-8') as f:
        d = json.load(f)
    sub101 = next(s for cat in d for s in cat.get('subcategories', []) if s['id'] == 101)
    text = f"{sub101['nameAr']}. {strip_html(sub101['contentAr'])}"

    ogg_path = str(Path(AUDIO_DIR) / '1_101.ogg')
    with tempfile.NamedTemporaryFile(suffix='.mp3', delete=False) as tmp:
        tmp_mp3 = tmp.name

    try:
        await edge_tts.Communicate(text, VOICE).save(tmp_mp3)
        subprocess.run(
            ['ffmpeg', '-y', '-i', tmp_mp3, '-c:a', 'libvorbis', '-q:a', '4', ogg_path],
            check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL
        )
        size_kb = os.path.getsize(ogg_path) // 1024
        print(f'✓ Regenerated 1_101.ogg  ({size_kb} KB)')
    finally:
        if os.path.exists(tmp_mp3):
            os.unlink(tmp_mp3)

asyncio.run(regen())
print('\nDone.')
