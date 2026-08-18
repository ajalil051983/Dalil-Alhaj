"""
Update subcategory 101 content in all languages, sync appdata.bin,
regenerate 1_101.ogg, and copy it to Resources/Audio.
"""
import asyncio
import json
import os
import re
import shutil
import sqlite3
import subprocess
import tempfile
from pathlib import Path

import edge_tts


ROOT = Path(r"d:\Ai workspace\Khayrat Alhaj")
JSON_FILE = ROOT / "AIAudioGenerationFromText" / "categories.json"
SYNC_SCRIPT = ROOT / "AIAudioGenerationFromText" / "sync_database.py"
DB_PATH = ROOT / "KhayratAlhaj" / "Resources" / "Data" / "appdata.bin"
RAW_AUDIO_DIR = ROOT / "KhayratAlhaj" / "Resources" / "Raw" / "audio"
COPY_AUDIO_DIR = ROOT / "KhayratAlhaj" / "Resources" / "Audio"
VOICE = "ar-SA-HamedNeural"


CONTENT_AR = """<h3>الحج: التعريف والأهمية</h3><h4>التعريف الشرعي</h4><p><strong>الحج</strong> لغة: القصد والنية. وشرعا: قصد بيت الله الحرام لأداء مناسك مخصوصة في أوقات معينة بنية التقرب إلى الله تعالى.</p><h4>آيات قرآنية عن الحج</h4><ul><li><strong>قال الله تعالى:</strong> وَلِلَّهِ عَلَى النَّاسِ حِجُّ الْبَيْتِ مَنِ اسْتَطَاعَ إِلَيْهِ سَبِيلاً (آل عمران: 97).</li><li><strong>وقال تعالى:</strong> وَأَذِّن فِي النَّاسِ بِالْحَجِّ يَأْتُوكَ رِجَالاً وَعَلَى كُلِّ ضَامِرٍ يَأْتِينَ مِن كُلِّ فَجٍّ عَمِيقٍ (الحج: 27).</li><li><strong>وقال تعالى:</strong> الْحَجُّ أَشْهُرٌ مَّعْلُومَاتٌ فَمَن فَرَضَ فِيهِنَّ الْحَجَّ فَلَا رَفَثَ وَلَا فُسُوقَ وَلَا جِدَالَ فِي الْحَجِّ (البقرة: 197).</li></ul><h4>أحاديث صحيحة في الحج</h4><ul><li><strong>عن أبي هريرة رضي الله عنه:</strong> قال النبي صلى الله عليه وسلم: العمرة إلى العمرة كفارة لما بينهما، والحج المبرور ليس له جزاء إلا الجنة (متفق عليه).</li><li><strong>عن عائشة رضي الله عنها:</strong> قالت: يا رسول الله، نرى الجهاد أفضل العمل. قال: لكن أفضل الجهاد حج مبرور (البخاري).</li><li><strong>عن ابن مسعود:</strong> قال النبي صلى الله عليه وسلم: تابعوا بين الحج والعمرة فإنهما ينفيان الفقر والذنوب (الترمذي).</li><li><strong>عن ابن عباس:</strong> قال النبي صلى الله عليه وسلم: من حج فلم يرفث ولم يفسق رجع كيوم ولدته أمه (متفق عليه).</li></ul><h4>أهمية الحج</h4><ul><li><strong>1. تحقيق التوحيد:</strong> الحج مظهر عظيم من مظاهر التوحيد حيث يقف المسلمون موحدون أمام الله تعالى.</li><li><strong>2. مغفرة الذنوب:</strong> الحج المبرور يكفر الخطايا ويمحو الذنوب السابقة.</li><li><strong>3. الثواب الجزيل:</strong> الحج المبرور ليس له جزاء إلا الجنة.</li><li><strong>4. التزكية الروحية:</strong> يطهر النفس ويقربها من ربها بصدق وإخلاص.</li><li><strong>5. التكافل الاجتماعي:</strong> يلتقي المسلمون من أنحاء العالم فيتبادلون المحبة والمودة.</li><li><strong>6. الاقتداء بإبراهيم:</strong> الحج استجابة لنداء إبراهيم عليه السلام.</li></ul><h4>شروط وجوب الحج</h4><ul><li>الإسلام.</li><li>العقل والبلوغ.</li><li>الاستطاعة المالية والصحية.</li><li>الأمن على النفس والمال والدين.</li></ul>"""

CONTENT_EN = """<h3>Hajj: Definition and Significance</h3><h4>Islamic Legal Definition</h4><p><strong>Hajj</strong> linguistically means: purpose and intention. In Islamic law, it means intending the Sacred House of Allah to perform specific rites at appointed times, with the intention of drawing closer to Allah the Exalted.</p><h4>Quranic Verses About Hajj</h4><ul><li><strong>Allah the Exalted says:</strong> And [due] to Allah from the people is a pilgrimage to the House, for whoever is able to find thereto a way (Aal Imran 3:97).</li><li><strong>And He says:</strong> And proclaim to the people the Hajj; they will come to you on foot and on every lean camel; they will come from every distant pass (Al-Hajj 22:27).</li><li><strong>And He says:</strong> Hajj is [during] well-known months, so whoever has made Hajj obligatory upon himself therein, there is no sexual relations, no disobedience, and no disputing during Hajj (Al-Baqarah 2:197).</li></ul><h4>Authentic Hadiths About Hajj</h4><ul><li><strong>From Abu Hurairah, may Allah be pleased with him:</strong> The Prophet, peace and blessings be upon him, said: Umrah to Umrah is an expiation for what is between them, and an accepted Hajj has no reward except Paradise (agreed upon).</li><li><strong>From Aishah, may Allah be pleased with her:</strong> She said: O Messenger of Allah, we see jihad as the best deed. He said: Rather, the best jihad is an accepted Hajj (Al-Bukhari).</li><li><strong>From Ibn Masud:</strong> The Prophet, peace and blessings be upon him, said: Follow up Hajj and Umrah, for they remove poverty and sins (Al-Tirmidhi).</li><li><strong>From Ibn Abbas:</strong> The Prophet, peace and blessings be upon him, said: Whoever performs Hajj and does not engage in obscenity or wrongdoing returns like the day his mother gave birth to him (agreed upon).</li></ul><h4>The Importance of Hajj</h4><ul><li><strong>1. Realizing Tawhid:</strong> Hajj is a great manifestation of monotheism, as Muslims stand united before Allah the Exalted.</li><li><strong>2. Forgiveness of sins:</strong> An accepted Hajj expiates mistakes and erases previous sins.</li><li><strong>3. Abundant reward:</strong> An accepted Hajj has no reward except Paradise.</li><li><strong>4. Spiritual purification:</strong> It purifies the soul and brings it closer to its Lord with sincerity and devotion.</li><li><strong>5. Social solidarity:</strong> Muslims from around the world meet and exchange love and affection.</li><li><strong>6. Following Ibrahim:</strong> Hajj is a response to the call of Ibrahim, peace be upon him.</li></ul><h4>Conditions for Hajj to Be Obligatory</h4><ul><li>Islam.</li><li>Sound mind and puberty.</li><li>Financial and physical ability.</li><li>Safety for oneself, wealth, and religion.</li></ul>"""

CONTENT_FR = """<h3>Hajj : Définition et importance</h3><h4>Définition juridique islamique</h4><p><strong>Le Hajj</strong>, dans la langue, signifie : le dessein et l'intention. Dans la loi islamique, il signifie se rendre à la Maison sacrée d'Allah pour accomplir des rites particuliers à des moments déterminés, avec l'intention de se rapprocher d'Allah le Très-Haut.</p><h4>Versets coraniques sur le Hajj</h4><ul><li><strong>Allah le Très-Haut dit :</strong> Et c'est un devoir envers Allah pour les gens qui en ont les moyens d'accomplir le pèlerinage à la Maison (Al Imran 3:97).</li><li><strong>Et Il dit :</strong> Et annonce aux gens le Hajj ; ils viendront à toi à pied et sur toute monture amaigrie, venant de tout chemin éloigné (Al-Hajj 22:27).</li><li><strong>Et Il dit :</strong> Le Hajj a lieu pendant des mois connus. Quiconque s'y engage à accomplir le Hajj doit s'abstenir de rapports intimes, de perversité et de dispute pendant le Hajj (Al-Baqara 2:197).</li></ul><h4>Hadiths authentiques sur le Hajj</h4><ul><li><strong>D'après Abou Hourayra, qu'Allah l'agrée :</strong> Le Prophète, paix et bénédictions sur lui, a dit : La Omra jusqu'à la Omra suivante expie ce qui est entre les deux, et le Hajj accepté n'a d'autre récompense que le Paradis (rapporté par Al-Bukhari et Muslim).</li><li><strong>D'après Aïcha, qu'Allah l'agrée :</strong> Elle dit : Ô Messager d'Allah, nous considérons le jihad comme la meilleure oeuvre. Il dit : Mais le meilleur jihad est un Hajj accepté (Al-Bukhari).</li><li><strong>D'après Ibn Masoud :</strong> Le Prophète, paix et bénédictions sur lui, a dit : Faites suivre le Hajj et la Omra, car ils repoussent la pauvreté et les péchés (Al-Tirmidhi).</li><li><strong>D'après Ibn Abbas :</strong> Le Prophète, paix et bénédictions sur lui, a dit : Celui qui accomplit le Hajj sans obscénité ni perversité revient comme le jour où sa mère l'a mis au monde (rapporté par Al-Bukhari et Muslim).</li></ul><h4>L'importance du Hajj</h4><ul><li><strong>1. Réaliser le Tawhid :</strong> Le Hajj est une grande manifestation du monothéisme, car les musulmans se tiennent unis devant Allah le Très-Haut.</li><li><strong>2. Le pardon des péchés :</strong> Le Hajj accepté expie les fautes et efface les péchés passés.</li><li><strong>3. Une immense récompense :</strong> Le Hajj accepté n'a d'autre récompense que le Paradis.</li><li><strong>4. La purification spirituelle :</strong> Il purifie l'âme et la rapproche de son Seigneur avec sincérité et dévouement.</li><li><strong>5. La solidarité sociale :</strong> Les musulmans venus du monde entier se rencontrent et échangent amour et affection.</li><li><strong>6. Suivre Ibrahim :</strong> Le Hajj est une réponse à l'appel d'Ibrahim, paix sur lui.</li></ul><h4>Conditions rendant le Hajj obligatoire</h4><ul><li>L'islam.</li><li>La raison et la puberté.</li><li>La capacité financière et physique.</li><li>La sécurité pour soi, ses biens et sa religion.</li></ul>"""


def load_categories() -> list[dict]:
    with JSON_FILE.open("r", encoding="utf-8") as file:
        return json.load(file)


def find_subcategory(data: list[dict], subcategory_id: int) -> dict:
    for category in data:
        for subcategory in category.get("subcategories", []):
            if subcategory.get("id") == subcategory_id:
                return subcategory
    raise LookupError(f"Subcategory {subcategory_id} was not found")


def update_json() -> dict:
    data = load_categories()
    subcategory = find_subcategory(data, 101)
    subcategory["contentAr"] = CONTENT_AR
    subcategory["contentEn"] = CONTENT_EN
    subcategory["contentFr"] = CONTENT_FR

    with JSON_FILE.open("w", encoding="utf-8") as file:
        json.dump(data, file, ensure_ascii=False, indent=2)
        file.write("\n")

    print("✓ Updated categories.json (101)")
    return subcategory


def sync_database() -> None:
    subprocess.run(["python", str(SYNC_SCRIPT)], cwd=ROOT, check=True)


def verify_database(expected: dict) -> None:
    conn = sqlite3.connect(DB_PATH)
    try:
        row = conn.execute(
            "SELECT ContentAr, ContentEn, ContentFr FROM SubCategories WHERE Id=101"
        ).fetchone()
    finally:
        conn.close()

    if row != (expected["contentAr"], expected["contentEn"], expected["contentFr"]):
        raise RuntimeError("Database row 101 does not match categories.json")

    print("✓ Verified appdata.bin row 101 matches categories.json")


def strip_html(html: str) -> str:
    html = html.replace("&nbsp;", " ").replace("&amp;", "و")
    html = re.sub(r"</(h[1-6]|p|li|tr|td|th)>", ". ", html, flags=re.IGNORECASE)
    clean = re.sub(r"<[^>]+>", " ", html)
    clean = re.sub(r"[\s.]+", " ", clean)
    return clean.strip(". ")


async def regenerate_audio(expected: dict) -> Path:
    text = f"{expected['nameAr']}. {strip_html(expected['contentAr'])}"
    ogg_path = RAW_AUDIO_DIR / "1_101.ogg"

    with tempfile.NamedTemporaryFile(suffix=".mp3", delete=False) as temp_file:
        temp_mp3 = temp_file.name

    try:
        await edge_tts.Communicate(text, VOICE).save(temp_mp3)
        subprocess.run(
            ["ffmpeg", "-y", "-i", temp_mp3, "-c:a", "libvorbis", "-q:a", "4", str(ogg_path)],
            check=True,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
        size_kb = os.path.getsize(ogg_path) // 1024
        print(f"✓ Regenerated 1_101.ogg ({size_kb} KB)")
        return ogg_path
    finally:
        if os.path.exists(temp_mp3):
            os.unlink(temp_mp3)


def copy_audio(source: Path) -> Path:
    COPY_AUDIO_DIR.mkdir(parents=True, exist_ok=True)
    destination = COPY_AUDIO_DIR / source.name
    shutil.copy2(source, destination)
    print(f"✓ Copied 1_101.ogg to {destination}")
    return destination


async def main() -> None:
    updated = update_json()
    sync_database()
    verify_database(updated)
    raw_audio = await regenerate_audio(updated)
    copied_audio = copy_audio(raw_audio)
    print(f"✓ Copied audio size: {copied_audio.stat().st_size // 1024} KB")
    print("\nDone.")


if __name__ == "__main__":
    asyncio.run(main())