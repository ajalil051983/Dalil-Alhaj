import json

file_path = "AIAudioGenerationFromText/categories.json"

# Read the JSON file
with open(file_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Find subcategories 101 and 102
for cat in data:
    if cat.get('id') == 1:  # Main category 1 "التحضير للحج"
        for subcat in cat.get('subcategories', []):
            # Update subcategory 101 - Arabic translation of "Conditions of Hajj"
            if subcat.get('id') == 101:
                subcat['contentAr'] = '<h3>شروط الحج</h3><ul><li><strong>شرط الصحة:</strong> الإسلام.</li><li><strong>شروط الوجوب:</strong> المسؤولية الشرعية والحرية والقدرة.</li><li>إذا أدى طفل أو مجنون الحج مع وليه، فالحج صحيح لكن لا يسقط فرض الحج عنه؛ يجب إعادته بعد البلوغ أو العقل.</li><li><strong>القدرة:</strong> الاستطاعة للوصول إلى بيت الله الحرام بأمان على النفس والمال والدين، بلا مشقة غير عادية، سيراً أو ركوباً.</li><li><strong>النساء:</strong> المرأة لا تسافر للحج أو العمرة إلا مع زوج أو محرم. إن لم تجد أحداً لكن وجدت مجموعة موثوقة، يصبح الحج فرضاً عندما تستطيع.</li></ul>'
                print("✓ Updated subcategory 101 contentAr")
                
            # Update subcategory 102 - ensure Arabic matches the 7-section comprehensive pre-flight guide
            elif subcat.get('id') == 102:
                # Update Arabic to align with English/French comprehensive 7-section format
                subcat['contentAr'] = '<h3>التحضيرات قبل السفر بالطائرة للحج</h3><h4>1. الوثائق المطلوبة</h4><ul><li><strong>جواز السفر:</strong> تأكد من صلاحيته لمدة 6 أشهر على الأقل. احتفظ بنسخة ضوئية.</li><li><strong>تأشيرة الحج:</strong> تحقق من المتطلبات مع وكالة موثوقة.</li><li><strong>بطاقة الحاج:</strong> احصل عليها من الوكالة المعتمدة واحفظها آمنة.</li><li><strong>شهادات التطعيم:</strong> لقاحات مطلوبة (الحمى الصفراء، التهاب السحايا) مع الإثبات.</li></ul><h4>2. الصحة والتحضير الطبي</h4><ul><li><strong>فحص طبي شامل:</strong> استشر طبيبك خاصة للأمراض المزمنة.</li><li><strong>أدوية:</strong> احضر في العبوات الأصلية مع نسخة الوصفة والأسماء الإنجليزية.</li><li><strong>إمدادات طبية:</strong> ضمادات، مراهم، فيتامينات، مسكنات، أدوية معوية.</li><li><strong>لياقة بدنية:</strong> مارس تمارين خفيفة للتحضير للمشي الطويل.</li></ul><h4>3. الملابس والأمتعة الذكية</h4><ul><li><strong>ملابس مناسبة:</strong> قطن خفيف للطقس الحار والجاف.</li><li><strong>أحذية مريحة:</strong> سهلة الخلع للطواف والسعي. عدة أزواج.</li><li><strong>جوارب قطنية:</strong> كمية كافية لامتصاص العرق.</li><li><strong>ملابس داخلية:</strong> كمية كافية لمدة إقامتك.</li><li><strong>حقيبة اليد:</strong> ضع الهاتف والأدوية والوثائق والمال والجواز.</li><li><strong>حقائب مسجلة:</strong> لا تتجاوز 23 كجم لكل حقيبة.</li></ul><h4>4. النظافة الشخصية والعناية</h4><ul><li><strong>منتجات العناية:</strong> فرشاة أسنان، معجون، شامبو، صابون سائل، مناديل مبللة.</li><li><strong>واقي شمس قوي:</strong> SPF 50+ ضد الشمس الحارة.</li><li><strong>مرطب ومرهم:</strong> لترطيب الجلد الجاف والعناية بالجروح الصغيرة.</li><li><strong>منتجات نسائية:</strong> النساء يحضرن ما يلزمهن.</li></ul><h4>5. المال والترتيبات المالية</h4><ul><li><strong>عملات:</strong> الريال السعودي وعملة بلدك.</li><li><strong>بطاقات:</strong> ائتمان وخصم؛ أخبر بنكك.</li><li><strong>ميزانية مخطط:</strong> مشتريات، هدايا، طوارئ، تبرعات.</li></ul><h4>6. النقل واللوجستيات</h4><ul><li><strong>التذاكر والحجوزات:</strong> أكد الرحلات والفنادق. أرقام التأكيد.</li><li><strong>نقل المطار:</strong> رتب عبر وكالتك.</li><li><strong>هاتف:</strong> بطاقة SIM سعودية أو التجوال الدولي.</li><li><strong>تأمين شامل:</strong> يشمل الإجلاء الطبي.</li></ul><h4>7. التحضير الروحي والعقلي</h4><ul><li><strong>نية خالصة:</strong> جدد نيتك بصدق لإتمام الحج.</li><li><strong>دعاء:</strong> بسم الله توكلت على الله، لا حول ولا قوة إلا بالله.</li><li><strong>تعلم المناسك:</strong> اقرأ عن الحج للاستعداد.</li><li><strong>التوكل:</strong> توكل على الله؛ سيسهل عليك.</li></ul>'
                print("✓ Updated subcategory 102 contentAr")

# Write back to file
with open(file_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=2)

print("✓ All content updated successfully")
