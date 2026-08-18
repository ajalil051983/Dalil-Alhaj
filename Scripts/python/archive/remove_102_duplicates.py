"""
Remove subcategory 102 details that are already covered by subcategory 104.

Sequence:
1. Update categories.json first.
2. Sync categories.json into appdata.bin.
3. Verify database row 102 matches JSON.
4. Regenerate only 1_102.ogg.
"""
import asyncio
import json
import os
import re
import sqlite3
import subprocess
import tempfile
from pathlib import Path

import edge_tts


ROOT = Path(r"d:\Ai workspace\Khayrat Alhaj")
JSON_FILE = ROOT / "AIAudioGenerationFromText" / "categories.json"
SYNC_SCRIPT = ROOT / "AIAudioGenerationFromText" / "sync_database.py"
DB_PATH = ROOT / "KhayratAlhaj" / "Resources" / "Data" / "appdata.bin"
AUDIO_DIR = ROOT / "KhayratAlhaj" / "Resources" / "Raw" / "audio"
VOICE = "ar-SA-HamedNeural"


CONTENT_AR = """<h3>التحضيرات قبل السفر بالطائرة للحج</h3><h4>1. الصحة والتحضير الطبي</h4><ul><li><strong>فحص طبي شامل:</strong> استشر طبيبك قبل السفر، خاصة إذا كنت تعاني من السكري أو أمراض القلب أو ضغط الدم أو أي مرض مزمن.</li><li><strong>الأدوية الشخصية:</strong> احمل أدويتك في عبواتها الأصلية، ومعها وصفة طبية أو قائمة بأسماء الأدوية والجرعات.</li><li><strong>إمدادات طبية خفيفة:</strong> جهز ضمادات، مراهم للجروح البسيطة، مسكنات مناسبة، وأدوية للمعدة عند الحاجة.</li><li><strong>لياقة بدنية:</strong> ابدأ بالمشي الخفيف قبل السفر لتعتاد على الحركة الطويلة والوقوف في الزحام.</li></ul><h4>2. الملابس والأمتعة العملية</h4><ul><li><strong>ملابس مناسبة:</strong> اختر ملابس قطنية خفيفة ومريحة تناسب الطقس الحار والجاف.</li><li><strong>أحذية مريحة:</strong> احمل حذاء مريحا وسهل الخلع للطواف والسعي، ويفضل وجود زوج احتياطي.</li><li><strong>جوارب قطنية:</strong> خذ كمية كافية تساعد على امتصاص العرق وتقليل الاحتكاك.</li><li><strong>ملابس داخلية:</strong> جهز كمية مناسبة لمدة الإقامة مع مراعاة سهولة الغسل والتجفيف.</li><li><strong>حقيبة صغيرة أثناء الرحلة:</strong> اجعل فيها الهاتف، الشاحن، الأدوية، المناديل، واحتياجاتك القريبة أثناء الطيران والتنقل.</li></ul><h4>3. النظافة الشخصية والعناية</h4><ul><li><strong>منتجات العناية:</strong> فرشاة أسنان، معجون، شامبو، صابون سائل، مناديل مبللة، ومناديل ورقية.</li><li><strong>واقي شمس قوي:</strong> استخدم SPF 50+ للحماية من الشمس الشديدة.</li><li><strong>مرطب ومرهم:</strong> خذ مرطبا للجلد الجاف ومرهما مناسبا للجروح أو التسلخات البسيطة.</li><li><strong>احتياجات خاصة:</strong> على النساء تجهيز ما يحتجنه من منتجات شخصية بطريقة مريحة ومنظمة.</li></ul><h4>4. المال والترتيبات المالية</h4><ul><li><strong>عملات:</strong> وفر مبلغا مناسبا من الريال السعودي مع قدر محدود من عملة بلدك.</li><li><strong>بطاقات مصرفية:</strong> احمل بطاقة ائتمان أو خصم صالحة، وأبلغ بنكك بموعد السفر لتجنب إيقاف العمليات.</li><li><strong>ميزانية مخططة:</strong> قسم المصاريف بين الطعام، التنقلات، الهدايا، الطوارئ، والصدقات.</li></ul><h4>5. النقل واللوجستيات</h4><ul><li><strong>الوصول إلى المطار:</strong> رتب وسيلة النقل إلى المطار مبكرا واترك وقتا كافيا للإجراءات والزحام.</li><li><strong>التواصل:</strong> جهز هاتفك، الشاحن، وبطارية احتياطية، وتأكد من طريقة الاتصال في السعودية.</li><li><strong>التنسيق مع المجموعة:</strong> احتفظ بأرقام المشرفين ومرافقي الرحلة، واتفق على نقاط تجمع واضحة عند الحاجة.</li><li><strong>التأمين والسفر:</strong> احتفظ بمعلومات التأمين وأرقام الطوارئ في مكان يسهل الوصول إليه.</li></ul><h4>6. التحضير الروحي والعقلي</h4><ul><li><strong>النية والتوبة:</strong> جدد نيتك للحج وأقبل على الله بتوبة صادقة.</li><li><strong>تعلم المناسك:</strong> راجع خطوات الحج الأساسية قبل السفر حتى تدخل الرحلة بطمأنينة.</li><li><strong>الصبر والرفق:</strong> هيئ نفسك للزحام والانتظار، واجعل الرفق بالناس جزءا من عبادتك.</li><li><strong>الأدعية:</strong> حضر أدعية مختصرة تحفظها أو تقرؤها أثناء السفر والمناسك.</li></ul>"""

CONTENT_EN = """<h3>Preparation Before Flying to Hajj</h3><h4>1. Health and Medical Readiness</h4><ul><li><strong>Comprehensive medical checkup:</strong> See your doctor before travel, especially if you have diabetes, heart disease, blood pressure issues, or another chronic condition.</li><li><strong>Personal medications:</strong> Bring your medicines in their original packaging, with a prescription or a clear list of names and doses.</li><li><strong>Light medical supplies:</strong> Pack bandages, ointment for minor wounds, suitable pain relievers, and stomach medicine if needed.</li><li><strong>Physical fitness:</strong> Start light walking before travel so your body is ready for long movement and standing in crowds.</li></ul><h4>2. Practical Clothing and Packing</h4><ul><li><strong>Suitable clothing:</strong> Choose light, comfortable cotton clothing for hot and dry weather.</li><li><strong>Comfortable footwear:</strong> Bring shoes that are comfortable and easy to remove for tawaf and sa'i, preferably with a spare pair.</li><li><strong>Cotton socks:</strong> Pack enough socks to absorb sweat and reduce friction.</li><li><strong>Undergarments:</strong> Prepare an adequate amount for your stay, considering easy washing and drying.</li><li><strong>Small in-flight bag:</strong> Keep your phone, charger, medicines, tissues, and close personal needs within easy reach during the flight and transfers.</li></ul><h4>3. Personal Hygiene and Care</h4><ul><li><strong>Care products:</strong> Toothbrush, toothpaste, shampoo, liquid soap, wet wipes, and tissues.</li><li><strong>Strong sunscreen:</strong> Use SPF 50+ to protect against intense sun.</li><li><strong>Moisturizer and ointment:</strong> Bring moisturizer for dry skin and suitable ointment for minor cuts or chafing.</li><li><strong>Personal needs:</strong> Women should prepare their personal-care products in a comfortable and organized way.</li></ul><h4>4. Money and Financial Arrangements</h4><ul><li><strong>Currency:</strong> Prepare a suitable amount of Saudi riyals and a limited amount of your home currency.</li><li><strong>Bank cards:</strong> Carry a valid credit or debit card and notify your bank of your travel dates to avoid declined transactions.</li><li><strong>Planned budget:</strong> Divide expenses between food, transport, gifts, emergencies, and charity.</li></ul><h4>5. Transport and Logistics</h4><ul><li><strong>Getting to the airport:</strong> Arrange airport transport early and leave enough time for procedures and crowds.</li><li><strong>Communication:</strong> Prepare your phone, charger, power bank, and confirm how you will stay connected in Saudi Arabia.</li><li><strong>Group coordination:</strong> Keep the numbers of supervisors and travel companions, and agree on clear meeting points when needed.</li><li><strong>Insurance and travel support:</strong> Keep insurance information and emergency numbers somewhere easy to access.</li></ul><h4>6. Spiritual and Mental Preparation</h4><ul><li><strong>Intention and repentance:</strong> Renew your intention for Hajj and turn to Allah with sincere repentance.</li><li><strong>Learn the rituals:</strong> Review the basic steps of Hajj before travel so you begin the journey with calm confidence.</li><li><strong>Patience and kindness:</strong> Prepare yourself for crowds and waiting, and make kindness to people part of your worship.</li><li><strong>Supplications:</strong> Prepare short duas to memorize or read during travel and the rituals.</li></ul>"""

CONTENT_FR = """<h3>Préparation avant le vol vers le Hajj</h3><h4>1. Santé et préparation médicale</h4><ul><li><strong>Examen médical complet :</strong> Consultez votre médecin avant le voyage, surtout en cas de diabète, de maladie cardiaque, de tension artérielle ou d'une autre maladie chronique.</li><li><strong>Médicaments personnels :</strong> Apportez vos médicaments dans leur emballage d'origine, avec une ordonnance ou une liste claire des noms et des doses.</li><li><strong>Petites fournitures médicales :</strong> Préparez des pansements, une pommade pour les petites plaies, des antalgiques adaptés et un médicament pour l'estomac si nécessaire.</li><li><strong>Condition physique :</strong> Commencez à marcher légèrement avant le voyage afin de vous habituer aux longs déplacements et à l'attente dans la foule.</li></ul><h4>2. Vêtements et bagages pratiques</h4><ul><li><strong>Vêtements appropriés :</strong> Choisissez des vêtements légers, confortables et en coton pour le climat chaud et sec.</li><li><strong>Chaussures confortables :</strong> Apportez des chaussures confortables et faciles à enlever pour le tawaf et le sa'i, de préférence avec une paire de rechange.</li><li><strong>Chaussettes en coton :</strong> Prévoyez une quantité suffisante pour absorber la transpiration et réduire les frottements.</li><li><strong>Sous-vêtements :</strong> Préparez une quantité adaptée à la durée du séjour, en tenant compte du lavage et du séchage faciles.</li><li><strong>Petit sac pendant le trajet :</strong> Gardez à portée de main le téléphone, le chargeur, les médicaments, les mouchoirs et vos besoins personnels proches pendant le vol et les déplacements.</li></ul><h4>3. Hygiène personnelle et soins</h4><ul><li><strong>Articles de soin :</strong> Brosse à dents, dentifrice, shampooing, savon liquide, lingettes humides et mouchoirs.</li><li><strong>Écran solaire puissant :</strong> Utilisez un SPF 50+ pour vous protéger du soleil intense.</li><li><strong>Hydratant et pommade :</strong> Apportez une crème hydratante pour la peau sèche et une pommade adaptée aux petites blessures ou irritations.</li><li><strong>Besoins personnels :</strong> Les femmes doivent préparer leurs produits personnels de manière confortable et organisée.</li></ul><h4>4. Argent et arrangements financiers</h4><ul><li><strong>Devises :</strong> Prévoyez une somme adaptée en riyals saoudiens et un montant limité dans la monnaie de votre pays.</li><li><strong>Cartes bancaires :</strong> Emportez une carte de crédit ou de débit valide et informez votre banque des dates du voyage pour éviter les blocages.</li><li><strong>Budget planifié :</strong> Répartissez les dépenses entre nourriture, transports, cadeaux, urgences et aumône.</li></ul><h4>5. Transport et logistique</h4><ul><li><strong>Arrivée à l'aéroport :</strong> Organisez tôt le transport vers l'aéroport et prévoyez assez de temps pour les procédures et l'affluence.</li><li><strong>Communication :</strong> Préparez votre téléphone, le chargeur, une batterie externe et vérifiez comment rester joignable en Arabie saoudite.</li><li><strong>Coordination avec le groupe :</strong> Gardez les numéros des encadrants et des compagnons de voyage, et convenez de points de rendez-vous clairs si nécessaire.</li><li><strong>Assurance et assistance :</strong> Conservez les informations d'assurance et les numéros d'urgence dans un endroit facile d'accès.</li></ul><h4>6. Préparation spirituelle et mentale</h4><ul><li><strong>Intention et repentir :</strong> Renouvelez votre intention pour le Hajj et tournez-vous vers Allah avec un repentir sincère.</li><li><strong>Apprendre les rites :</strong> Révisez les étapes essentielles du Hajj avant le voyage afin de commencer avec sérénité.</li><li><strong>Patience et bienveillance :</strong> Préparez-vous à la foule et à l'attente, et faites de la bienveillance envers les gens une partie de votre adoration.</li><li><strong>Invocations :</strong> Préparez de courtes invocations à mémoriser ou à lire pendant le voyage et les rites.</li></ul>"""


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
    subcategory = find_subcategory(data, 102)
    subcategory["contentAr"] = CONTENT_AR
    subcategory["contentEn"] = CONTENT_EN
    subcategory["contentFr"] = CONTENT_FR

    with JSON_FILE.open("w", encoding="utf-8") as file:
        json.dump(data, file, ensure_ascii=False, indent=2)
        file.write("\n")

    print("✓ Updated categories.json (102)")
    return subcategory


def sync_database() -> None:
    subprocess.run(["python", str(SYNC_SCRIPT)], cwd=ROOT, check=True)


def verify_database(expected: dict) -> None:
    conn = sqlite3.connect(DB_PATH)
    try:
        row = conn.execute(
            "SELECT ContentAr, ContentEn, ContentFr FROM SubCategories WHERE Id=102"
        ).fetchone()
    finally:
        conn.close()

    if row != (expected["contentAr"], expected["contentEn"], expected["contentFr"]):
        raise RuntimeError("Database row 102 does not match categories.json")

    print("✓ Verified appdata.bin row 102 matches categories.json")


def strip_html(html: str) -> str:
    html = html.replace("&nbsp;", " ").replace("&amp;", "و")
    html = re.sub(r"</(h[1-6]|p|li|tr|td|th)>", ". ", html, flags=re.IGNORECASE)
    clean = re.sub(r"<[^>]+>", " ", html)
    clean = re.sub(r"[\s.]+", " ", clean)
    return clean.strip(". ")


async def regenerate_audio(expected: dict) -> None:
    text = f"{expected['nameAr']}. {strip_html(expected['contentAr'])}"
    ogg_path = AUDIO_DIR / "1_102.ogg"

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
        print(f"✓ Regenerated 1_102.ogg ({size_kb} KB)")
    finally:
        if os.path.exists(temp_mp3):
            os.unlink(temp_mp3)


async def main() -> None:
    updated = update_json()
    sync_database()
    verify_database(updated)
    await regenerate_audio(updated)
    print("\nDone.")


if __name__ == "__main__":
    asyncio.run(main())