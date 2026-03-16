# 🕋 زاد الحاج - Zad Alhaj

<div align="center">

![App Icon](ZadAlhaj/Resources/AppIcon/kaaba.svg)

**دليل شامل لمناسك الحج حسب المذهب المالكي**  
*A Comprehensive Hajj Guide According to the Maliki School*

[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-purple)](https://dotnet.microsoft.com/apps/maui)
[![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS%20%7C%20Windows-blue)](https://github.com)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)
[![Version](https://img.shields.io/badge/Version-1.0.0-orange)](https://github.com)

</div>

---

## 📖 نظرة عامة | Overview

**زاد الحاج** تطبيق متكامل يساعد الحجاج على أداء مناسك الحج بشكل صحيح وفق المذهب المالكي. التطبيق مصمم باللهجة المغربية (الدارجة) لتسهيل الفهم والاستخدام.

**Zad Alhaj** is a comprehensive mobile application that guides pilgrims through Hajj rituals according to the Maliki school of thought. The app is designed in Moroccan Arabic (Darija) for easier understanding and usage.

---

## ✨ الميزات الرئيسية | Key Features

### 📚 المحتوى التعليمي | Educational Content
- **6 فئات رئيسية** تغطي جميع جوانب الحج
- **24 فئة فرعية** بمعلومات تفصيلية
- محتوى منظم بشكل هرمي لسهولة التنقل
- نصوص واضحة مع أيقونات تمثيلية

### 🗺️ الخريطة التفاعلية | Interactive Map
- خريطة تفاعلية لمواقع الحج المقدسة
- عرض المسار حسب المذهب المالكي
- علامات مخصصة للمواقع الرئيسية:
  - 🕋 الكعبة المشرفة
  - ⛰️ جبل عرفات
  - 🌙 مزدلفة
  - 🏕️ منى
  - 🚶 الصفا والمروة

### 🔍 البحث الذكي | Smart Search
- بحث سريع في جميع المحتويات
- نتائج فورية أثناء الكتابة
- عرض المسار الكامل للنتائج (الفئة → الفئة الفرعية → المحتوى)

### ✅ قائمة المهام | Hajj Checklist
- **16 مهمة أساسية** لمراحل الحج
- تتبع التقدم مع نسبة الإنجاز
- تنظيم المهام حسب الترتيب الزمني
- حفظ تلقائي للحالة

### 🔊 التشغيل الصوتي | Audio Playback
- ملفات صوتية لجميع المحتويات (MP3)
- مشغل صوتي متكامل
- أزرار تشغيل/إيقاف مؤقت
- تحميل تلقائي من الموارد

### ❤️ المفضلة | Favorites System
- حفظ المحتويات المفضلة
- الوصول السريع من صفحة مخصصة
- إضافة/إزالة بنقرة واحدة
- أيقونات ديناميكية (🤍/❤️)

### 📋 المشاركة والنسخ | Share & Copy
- نسخ النصوص إلى الحافظة
- مشاركة المحتوى عبر التطبيقات الأخرى
- إشعارات نجاح العمليات

### ⚙️ الإعدادات | Settings
- **اختيار اللغة**: العربية، English، Français
- **حجم الخط**: صغير، متوسط، كبير
- **الوضع الليلي**: تفعيل/تعطيل
- حفظ تلقائي للتفضيلات

### 🎨 واجهة المستخدم | User Interface
- تصميم عصري وسهل الاستخدام
- ألوان متناسقة (أخضر #2E7D32، أصفر #F57F17)
- خطوط عربية واضحة (Tajawal)
- رسوم متحركة سلسة
- دعم RTL كامل

---

## 🏗️ البنية التقنية | Technical Architecture

### تقنيات المستخدمة | Technologies Used
- **.NET MAUI 10.0**: إطار العمل الأساسي
- **C# 12**: لغة البرمجة
- **XAML**: تصميم الواجهات
- **CommunityToolkit.Maui**: مكتبات إضافية
- **Mapsui**: عرض الخرائط
- **CommunityToolkit.Maui.MediaElement**: تشغيل الصوت

### المنصات المدعومة | Supported Platforms
- ✅ **Android** (API 21+)
- ✅ **iOS** (iOS 11+)
- ✅ **Windows** (Windows 10+)
- ⚠️ **macOS** (قيد التطوير)

### هيكل المشروع | Project Structure
```
ZadAlhaj/
├── Pages/                      # صفحات التطبيق
│   ├── MainPage.xaml          # الصفحة الرئيسية
│   ├── SubCategoryPage.xaml   # صفحة الفئات الفرعية
│   ├── ContentDetailPage.xaml # صفحة المحتوى
│   ├── SearchPage.xaml        # صفحة البحث
│   ├── ChecklistPage.xaml     # قائمة المهام
│   ├── MapPage.xaml           # الخريطة
│   ├── SettingsPage.xaml      # الإعدادات
│   └── LoadingPage.xaml       # صفحة التحميل
├── Models/                     # نماذج البيانات
│   ├── Category.cs
│   ├── SubCategory.cs
│   └── ChecklistItem.cs
├── Services/                   # الخدمات
│   ├── DataService.cs         # خدمة البيانات
│   └── FavoritesService.cs    # خدمة المفضلة
├── Resources/                  # الموارد
│   ├── Raw/
│   │   ├── categories.json    # بيانات الفئات
│   │   ├── categories-en.json
│   │   ├── categories-fr.json
│   │   └── audio/            # ملفات صوتية (600+ ملف)
│   ├── Images/               # الصور والأيقونات
│   ├── Fonts/                # الخطوط
│   └── Localization/         # ملفات الترجمة
└── Platforms/                # كود خاص بكل منصة
    ├── Android/
    ├── iOS/
    └── Windows/
```

---

## 📊 الفئات الرئيسية | Main Categories

| الأيقونة | الفئة | عدد الفئات الفرعية |
|---------|------|-------------------|
| ✈️ | التحضير للحج | 4 |
| 🕋 | الإحرام والميقات | 4 |
| 📿 | أركان الحج | 4 |
| ✅ | واجبات الحج | 4 |
| ⚠️ | الأخطاء الشائعة | 4 |
| 🤲 | الأدعية المأثورة | 4 |

**المجموع**: 6 فئات رئيسية × 4 فئات فرعية = **24 موضوع**

---

## 🚀 البدء | Getting Started

### المتطلبات | Prerequisites
- Visual Studio 2022 (17.8 أو أحدث)
- .NET 10.0 SDK
- أدوات تطوير MAUI
- Android SDK (للأندرويد)
- Xcode (لـ iOS/Mac)

### التثبيت | Installation

1. **استنساخ المستودع | Clone the repository**
```bash
git clone https://github.com/yourusername/dalil-alhaj.git
cd dalil-alhaj
```

2. **استعادة الحزم | Restore packages**
```bash
dotnet restore
```

3. **البناء | Build**
```bash
dotnet build
```

4. **التشغيل | Run**
```bash
# Android
dotnet build -t:Run -f net10.0-android

# iOS
dotnet build -t:Run -f net10.0-ios

# Windows
dotnet build -t:Run -f net10.0-windows10.0.26100.0
```

---

## 📱 لقطات الشاشة | Screenshots

<div align="center">

| الصفحة الرئيسية | الفئات الفرعية | المحتوى |
|----------------|----------------|---------|
| ![Main](docs/screenshots/main.png) | ![Sub](docs/screenshots/sub.png) | ![Content](docs/screenshots/content.png) |

| الخريطة | البحث | قائمة المهام |
|---------|-------|-------------|
| ![Map](docs/screenshots/map.png) | ![Search](docs/screenshots/search.png) | ![Checklist](docs/screenshots/checklist.png) |

</div>

---

## 📂 البيانات | Data Structure

### بنية ملف JSON
```json
{
  "Id": "1",
  "Name": "التحضير للحج",
  "NameEn": "Hajj Preparation",
  "NameFr": "Préparation du Hajj",
  "Icon": "✈️",
  "SubCategories": [
    {
      "Id": "101",
      "Name": "النية والإخلاص",
      "Content": "...",
      "Icon": "🎯"
    }
  ]
}
```

### الملفات الصوتية | Audio Files
- **التنسيق**: `audio/{CategoryId}_{SubCategoryId}.mp3`
- **العدد**: 600+ ملف
- **المثال**: `audio/1_101.mp3`

---

## 🛠️ المساهمة | Contributing

نرحب بالمساهمات! يرجى اتباع الخطوات التالية:

1. Fork المشروع
2. إنشاء فرع للميزة (`git checkout -b feature/AmazingFeature`)
3. Commit التغييرات (`git commit -m 'Add some AmazingFeature'`)
4. Push إلى الفرع (`git push origin feature/AmazingFeature`)
5. فتح Pull Request

---

## 📝 الترخيص | License

هذا المشروع مرخص بموجب رخصة MIT - انظر ملف [LICENSE](LICENSE) للتفاصيل.

---

## 👨‍💻 المطور | Developer

**Abdeljalil El Yasni**
- GitHub: [@AbdeljalilElYasni](https://github.com/AbdeljalilElYasni)
- Email: your.email@example.com

---

## 🙏 شكر وتقدير | Acknowledgments

- المحتوى مستند إلى المذهب المالكي
- الأيقونات من مكتبة Emoji
- الخطوط من Google Fonts (Tajawal, Open Sans)
- المجتمع المفتوح المصدر لـ .NET MAUI

---

## 📞 الدعم | Support

للأسئلة والدعم:
- 📧 Email: support@zadalhaj.com
- 🐛 Issues: [GitHub Issues](https://github.com/yourusername/zad-alhaj/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/yourusername/zad-alhaj/discussions)

---

## 🗺️ خريطة الطريق | Roadmap

- [x] النسخة الأولى (v1.0.0)
- [ ] دعم لغات إضافية (أردية، إندونيسية)
- [ ] إضافة مقاطع فيديو تعليمية
- [ ] تطبيق الواقع المعزز للمواقع
- [ ] وضع عدم الاتصال الكامل
- [ ] مزامنة التقدم عبر الأجهزة
- [ ] إشعارات للمهام اليومية

---

<div align="center">

**تقبل الله حجكم وسعيكم**  
*May Allah accept your Hajj and efforts*

**© 2026 ilafalkhayr. All rights reserved.**

</div>
