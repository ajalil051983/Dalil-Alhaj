"""
Fix subcategory 101: restore comprehensive Definition & Significance content
that matches its title "الحج: التعريف والأهمية" in all three languages.
"""
import json, sqlite3

JSON_FILE = r'd:\Ai workspace\Khayrat Alhaj\AIAudioGenerationFromText\categories.json'
DB_PATH   = r'D:\Ai workspace\Khayrat Alhaj\KhayratAlhaj\Resources\Data\appdata.bin'

# ─── Comprehensive Arabic content ────────────────────────────────────────────
CONTENT_AR = (
    '<h3>الحج: التعريف والأهمية</h3>'
    '<h4>التعريف الشرعي</h4>'
    '<p><strong>الحج</strong> لغةً: القصد والتوجه. وشرعاً: قصد بيت الله الحرام لأداء مناسك '
    'مخصوصة في أوقات معينة بنية التقرب إلى الله تعالى.</p>'
    '<h4>آيات قرآنية كريمة</h4>'
    '<ul>'
    '<li><strong>قال الله تعالى:</strong> وَلِلَّهِ عَلَى النَّاسِ حِجُّ الْبَيْتِ مَنِ اسْتَطَاعَ '
    'إِلَيْهِ سَبِيلاً وَمَنْ كَفَرَ فَإِنَّ اللَّهَ غَنِيٌّ عَنِ الْعَالَمِينَ (آل عمران: 97).</li>'
    '<li><strong>وقال تعالى:</strong> وَأَذِّن فِي النَّاسِ بِالْحَجِّ يَأْتُوكَ رِجَالاً وَعَلَى '
    'كُلِّ ضَامِرٍ يَأْتِينَ مِن كُلِّ فَجٍّ عَمِيقٍ (الحج: 27).</li>'
    '<li><strong>وقال تعالى:</strong> الْحَجُّ أَشْهُرٌ مَّعْلُومَاتٌ فَمَن فَرَضَ فِيهِنَّ '
    'الْحَجَّ فَلَا رَفَثَ وَلَا فُسُوقَ وَلَا جِدَالَ فِي الْحَجِّ (البقرة: 197).</li>'
    '</ul>'
    '<h4>أحاديث صحيحة في فضل الحج</h4>'
    '<ul>'
    '<li><strong>عن أبي هريرة رضي الله عنه:</strong> قال النبي ﷺ: العمرة إلى العمرة كفارة لما '
    'بينهما، والحج المبرور ليس له جزاء إلا الجنة. (متفق عليه)</li>'
    '<li><strong>عن عائشة رضي الله عنها:</strong> قالت: يا رسول الله، نرى الجهاد أفضل العمل، '
    'أفلا نجاهد؟ قال: لكن أفضل الجهاد حج مبرور. (البخاري)</li>'
    '<li><strong>عن ابن مسعود رضي الله عنه:</strong> قال النبي ﷺ: تابعوا بين الحج والعمرة فإنهما '
    'ينفيان الفقر والذنوب. (الترمذي)</li>'
    '<li><strong>عن ابن عباس رضي الله عنهما:</strong> قال النبي ﷺ: من حج فلم يرفث ولم يفسق رجع '
    'كيوم ولدته أمه. (متفق عليه)</li>'
    '</ul>'
    '<h4>أهمية الحج وفوائده</h4>'
    '<ul>'
    '<li><strong>1. تحقيق التوحيد:</strong> الحج مظهر عظيم من مظاهر التوحيد حيث يقف المسلمون '
    'موحدين بلباس واحد أمام الله تعالى.</li>'
    '<li><strong>2. مغفرة الذنوب:</strong> الحج المبرور يكفر الخطايا ويمحو الذنوب السابقة.</li>'
    '<li><strong>3. الثواب العظيم:</strong> الحج المبرور ليس له جزاء إلا الجنة.</li>'
    '<li><strong>4. التزكية الروحية:</strong> يطهر النفس ويقربها من ربها بصدق وإخلاص.</li>'
    '<li><strong>5. التكافل الاجتماعي:</strong> يلتقي المسلمون من كل أنحاء العالم في مشهد أخوي '
    'فريد.</li>'
    '<li><strong>6. الاقتداء بإبراهيم عليه السلام:</strong> الحج استجابة لنداء إبراهيم عليه '
    'السلام وإحياء لذكراه.</li>'
    '</ul>'
    '<h4>شروط وجوب الحج</h4>'
    '<ul>'
    '<li>الإسلام.</li>'
    '<li>العقل والبلوغ.</li>'
    '<li>الاستطاعة المالية والصحية.</li>'
    '<li>الأمن على النفس والدين.</li>'
    '<li>المحرم للمرأة (الزوج أو المحرم)، أو المجموعة الموثوقة عند بعض الفقهاء.</li>'
    '</ul>'
)

# ─── Comprehensive English content ───────────────────────────────────────────
CONTENT_EN = (
    '<h3>Hajj: Definition and Significance</h3>'
    '<h4>Scholarly Definition</h4>'
    '<p><strong>Hajj</strong> literally means: intention and direction. In Islamic law: the '
    'deliberate journey to the Sacred House of Allah to perform specific rites at designated '
    'times, with the intention of drawing closer to Allah.</p>'
    '<h4>Quranic Evidence</h4>'
    '<ul>'
    '<li><strong>Allah said:</strong> And [due] to Allah from the people is a pilgrimage to the '
    'House — for whoever is able to find thereto a way. And whoever disbelieves — then indeed, '
    'Allah is free from need of the worlds. (Quran 3:97)</li>'
    '<li><strong>Allah said:</strong> And proclaim to the people the Hajj; they will come to you '
    'on foot and on every lean camel; they will come from every distant pass. (Quran 22:27)</li>'
    '<li><strong>Allah said:</strong> Hajj is [during] well-known months, so whoever has made '
    'Hajj obligatory upon himself therein — there is no sexual relations, no disobedience, and '
    'no disputing during Hajj. (Quran 2:197)</li>'
    '</ul>'
    '<h4>Authentic Hadiths on the Merit of Hajj</h4>'
    '<ul>'
    '<li><strong>Abu Hurairah (r.a.):</strong> The Prophet ﷺ said: Umrah to Umrah is expiation '
    'for whatever sins are between them, and an accepted Hajj has no reward except Paradise. '
    '(Agreed upon)</li>'
    '<li><strong>Aishah (r.a.):</strong> She asked: O Messenger of Allah, we see jihad as the '
    'best deed; shall we not fight? He replied: The best jihad is an accepted Hajj. (Al-Bukhari)</li>'
    '<li><strong>Ibn Masud (r.a.):</strong> The Prophet ﷺ said: Perform Hajj and Umrah '
    'consecutively, for they remove poverty and sins. (Al-Tirmidhi)</li>'
    '<li><strong>Ibn Abbas (r.a.):</strong> The Prophet ﷺ said: Whoever performs Hajj without '
    'committing obscenity or wrongdoing returns as on the day his mother bore him. '
    '(Agreed upon)</li>'
    '</ul>'
    '<h4>Significance and Benefits of Hajj</h4>'
    '<ul>'
    '<li><strong>1. Realizing Tawhid:</strong> Hajj is a grand manifestation of monotheism where '
    'Muslims stand equal before Allah, dressed in identical garments.</li>'
    '<li><strong>2. Forgiveness of sins:</strong> An accepted Hajj expiates all previous sins.</li>'
    '<li><strong>3. Immense reward:</strong> The only reward for an accepted Hajj is Paradise.</li>'
    '<li><strong>4. Spiritual purification:</strong> It purifies the soul and brings it closer to '
    'its Lord in truth and sincerity.</li>'
    '<li><strong>5. Social solidarity:</strong> Muslims from all corners of the world unite in a '
    'unique display of brotherhood and equality.</li>'
    '<li><strong>6. Following Ibrahim (a.s.):</strong> Hajj is a response to the call of Ibrahim '
    'and a revival of his noble legacy.</li>'
    '</ul>'
    '<h4>Conditions Making Hajj Obligatory</h4>'
    '<ul>'
    '<li>Islam.</li>'
    '<li>Sanity and puberty.</li>'
    '<li>Financial and physical capability.</li>'
    '<li>Safety for oneself and one\'s religion.</li>'
    '<li>A mahram for women (husband or male guardian), or at minimum a trustworthy group '
    'according to some scholars.</li>'
    '</ul>'
)

# ─── Comprehensive French content ────────────────────────────────────────────
CONTENT_FR = (
    '<h3>Hajj : Définition et importance</h3>'
    '<h4>Définition légale</h4>'
    '<p><strong>Le Hajj</strong> signifie littéralement : intention et direction. En droit '
    "islamique : se rendre délibérément à la Maison sacrée d'Allah pour accomplir des rites "
    'spécifiques à des moments déterminés, avec l\'intention de se rapprocher d\'Allah.</p>'
    '<h4>Preuves coraniques</h4>'
    '<ul>'
    '<li><strong>Allah dit :</strong> Et le pèlerinage à la Maison est un devoir envers Allah '
    'pour quiconque est capable d\'y trouver un chemin. Quant à celui qui ne croit pas — Allah '
    'n\'a nul besoin des mondes. (Coran 3:97)</li>'
    '<li><strong>Allah dit :</strong> Et proclame aux gens le Hajj ; ils viendront à toi à pied '
    'et sur toute monture élancée, venant de tout chemin éloigné. (Coran 22:27)</li>'
    '<li><strong>Allah dit :</strong> Le Hajj a lieu pendant des mois bien connus. Quiconque '
    's\'impose le Hajj durant ces mois s\'abstiendra de rapports sexuels, de dépravation et de '
    'disputes. (Coran 2:197)</li>'
    '</ul>'
    '<h4>Hadiths authentiques sur le mérite du Hajj</h4>'
    '<ul>'
    '<li><strong>Abou Hourayra (r.a.) :</strong> Le Prophète ﷺ a dit : La Omra après la Omra '
    'efface les péchés entre elles, et le Hajj agréé n\'a d\'autre récompense que le Paradis. '
    '(Hadith concordant)</li>'
    '<li><strong>Aïcha (r.a.) :</strong> Elle demanda : Ô Messager d\'Allah, nous considérons le '
    'jihad comme la meilleure œuvre, ne devons-nous pas combattre ? Il répondit : Le meilleur '
    'jihad est le Hajj agréé. (Al-Boukhâri)</li>'
    '<li><strong>Ibn Masoud (r.a.) :</strong> Le Prophète ﷺ a dit : Enchaînez Hajj et Omra '
    'consécutivement, car ils éloignent la pauvreté et les péchés. (Al-Tirmidhi)</li>'
    '<li><strong>Ibn Abbas (r.a.) :</strong> Le Prophète ﷺ a dit : Quiconque accomplit le Hajj '
    'sans impudicité ni dépravation revient comme au jour où sa mère l\'a mis au monde. '
    '(Hadith concordant)</li>'
    '</ul>'
    '<h4>Importance et bienfaits du Hajj</h4>'
    '<ul>'
    '<li><strong>1. Réalisation du Tawhid :</strong> Le Hajj est une grandiose manifestation du '
    'monothéisme où les musulmans se tiennent égaux devant Allah dans un même vêtement.</li>'
    '<li><strong>2. Pardon des péchés :</strong> Le Hajj agréé efface tous les péchés antérieurs.</li>'
    '<li><strong>3. Récompense immense :</strong> La seule récompense d\'un Hajj agréé est le '
    'Paradis.</li>'
    '<li><strong>4. Purification spirituelle :</strong> Il purifie l\'âme et la rapproche de son '
    'Seigneur en vérité et en sincérité.</li>'
    '<li><strong>5. Solidarité sociale :</strong> Les musulmans du monde entier se réunissent dans '
    'une fraternité et une égalité uniques.</li>'
    '<li><strong>6. Suivre Ibrahim (a.s.) :</strong> Le Hajj est une réponse à l\'appel '
    'd\'Ibrahim et une perpétuation de son noble héritage.</li>'
    '</ul>'
    '<h4>Conditions rendant le Hajj obligatoire</h4>'
    '<ul>'
    '<li>L\'islam.</li>'
    '<li>La raison et la puberté.</li>'
    '<li>La capacité financière et physique.</li>'
    '<li>La sécurité pour soi et sa religion.</li>'
    '<li>Un mahram pour la femme (mari ou tuteur masculin), ou au minimum un groupe de confiance '
    'selon certains savants.</li>'
    '</ul>'
)

# ─── Update categories.json ───────────────────────────────────────────────────
with open(JSON_FILE, 'r', encoding='utf-8') as f:
    data = json.load(f)

for cat in data:
    for sub in cat.get('subcategories', []):
        if sub['id'] == 101:
            sub['contentAr'] = CONTENT_AR
            sub['contentEn'] = CONTENT_EN
            sub['contentFr'] = CONTENT_FR
            print('✓ Updated categories.json for subcategory 101')

with open(JSON_FILE, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=2)

# ─── Update appdata.bin ───────────────────────────────────────────────────────
conn = sqlite3.connect(DB_PATH)
cur = conn.cursor()
cur.execute(
    "UPDATE SubCategories SET ContentAr=?, ContentEn=?, ContentFr=? WHERE Id=101",
    (CONTENT_AR, CONTENT_EN, CONTENT_FR)
)
conn.commit()
print(f'✓ Updated appdata.bin for subcategory 101 (rows affected: {cur.rowcount})')
conn.close()
