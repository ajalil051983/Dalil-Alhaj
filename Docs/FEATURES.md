# دليل الحاج - ميزات التطبيق
## Dalil AlHaj App - Features Documentation

### 📱 نظرة عامة / Overview
تطبيق شامل لإرشاد الحجاج باللهجة المغربية (الدارجة)، يوفر دليل كامل لأداء مناسك الحج وفق المذهب المالكي.

A comprehensive Hajj guide app in Moroccan Arabic (Darija), providing a complete guide for performing Hajj rituals according to the Maliki school of thought.

---

## ✨ الميزات الرئيسية / Main Features

### 1. 📚 التنقل الهرمي / Hierarchical Navigation
- **الصفحة الرئيسية**: 6 فئات رئيسية معروضة في شبكة من عمودين
- **صفحة الفئات الفرعية**: 4 فئات فرعية لكل فئة رئيسية
- **صفحة المحتوى**: نصوص تفصيلية مع أيقونات تمثيلية

Structure:
```
MainPage (6 Categories) → SubCategoryPage (4 Subcategories) → ContentDetailPage
```

#### الفئات الرئيسية / Main Categories:
1. ✈️ **التحضير للحج** - Hajj Preparation
2. 🕋 **الإحرام والميقات** - Ihram & Miqat
3. 📿 **أركان الحج** - Pillars of Hajj
4. ✅ **واجبات الحج** - Hajj Obligations
5. ⚠️ **الأخطاء الشائعة** - Common Mistakes
6. 🤲 **الأدعية المأثورة** - Authentic Supplications

---

### 2. 🗺️ خريطة الحج / Hajj Map
- **OpenStreetMap** integration
- **5 مواقع مقدسة** / 5 Holy Sites with pins:
  - 🕋 الكعبة المشرفة (Kaaba) - 21.4225°N, 39.8262°E
  - ⛰️ جبل عرفات (Mount Arafat) - 21.3547°N, 39.9839°E
  - 🌙 مزدلفة (Muzdalifah) - 21.4069°N, 39.9375°E
  - 🏕️ منى (Mina) - 21.4219°N, 39.8926°E
  - 🚶 الصفا والمروة (Safa & Marwa) - 21.4233°N, 39.8266°E

**Features:**
- Interactive map with zoom/pan
- Location pins with Arabic labels
- Shows pilgrimage route path

---

### 3. 🔍 البحث / Search Functionality
- **Full-text search** across all categories and subcategories
- **Real-time results** as you type
- **Search scope**: 
  - Category names (أسماء الفئات)
  - Subcategory names (أسماء الفئات الفرعية)
  - Content text (نصوص المحتوى)
- **Navigation**: Tap any result to jump directly to content details

---

### 4. ✅ قائمة المراجعة / Hajj Checklist
**16 essential items** to prepare for Hajj:

#### الوثائق / Documents:
1. ✅ جواز السفر ساري المفعول (Valid passport)
2. ✅ تأشيرة الحج (Hajj visa)
3. ✅ تذاكر الطيران (Flight tickets)
4. ✅ حجز الفندق (Hotel reservation)

#### الملابس / Clothing:
5. ✅ ملابس الإحرام (Ihram clothing)
6. ✅ أحذية مريحة (Comfortable shoes)
7. ✅ ملابس خفيفة (Light clothes)

#### الأدوية / Medicine:
8. ✅ الأدوية الشخصية (Personal medications)
9. ✅ مسكنات الألم (Pain relievers)
10. ✅ واقي الشمس (Sunscreen)

#### الأغراض الشخصية / Personal Items:
11. ✅ محفظة النقود (Wallet)
12. ✅ الهاتف المحمول (Mobile phone)
13. ✅ شاحن الهاتف (Phone charger)
14. ✅ المصحف الشريف (Quran)
15. ✅ دليل الحج (Hajj guide)
16. ✅ حقيبة صغيرة (Small bag)

**Features:**
- ✅ Check/uncheck items
- 📊 Progress tracking: "X من 16 مكتمل"
- 💾 Persistent storage (saved between app sessions)

---

### 5. ❤️ المفضلة / Favorites System
- **Add/remove** content to favorites
- **Toggle button** on every content detail page
- **Visual feedback**: 
  - 🤍 "أضف للمفضلة" (Add to favorites)
  - ❤️ "المفضلة" (Already in favorites)
- **Persistent storage** using Preferences API

**Implementation:**
- `FavoritesService` manages favorites
- Stores subcategory IDs
- Quick access to frequently referenced content

---

### 6. 🔊 تشغيل الصوت / Audio Playback
**Audio integration** for selected content:

**Features:**
- 🎵 Audio player appears for content with `hasAudio: true` flag
- ▶️ Play button (تشغيل)
- ⏸️ Pause button (إيقاف)
- Clean blue UI integrated with content

**Implementation:**
- Uses `CommunityToolkit.Maui.MediaElement`
- Audio files to be placed in: `Resources/Raw/audio/`
- Naming convention: `{categoryId}_{subcategoryId}.mp3`

**Subcategories with Audio:**
All 24 subcategories are flagged for future audio content.

---

### 7. 📋 نسخ ومشاركة / Copy & Share
**Every content page** includes:

#### Copy Button (📋 انسخ النص):
- Copies full content text to clipboard
- Success notification: "تم نسخ النص بنجاح"

#### Share Button (📤 شارك):
- Shares content via system share sheet
- Includes:
  - Subcategory title
  - Full content
  - App attribution: "من تطبيق: دليل الحاج"
- Works with: WhatsApp, Email, SMS, etc.

---

### 8. ⚙️ الإعدادات / Settings

#### 🌙 الوضع الداكن / Dark Mode:
- Toggle between light and dark themes
- Affects entire app UI
- Saves preference

#### 🔤 حجم الخط / Font Size:
Three size options:
- **صغير** (Small) - 14pt
- **متوسط** (Medium) - 17pt ⭐ Default
- **كبير** (Large) - 20pt

**Features:**
- Applies globally to all content
- Improves readability
- Persistent setting

#### ℹ️ معلومات التطبيق / App Info:
- App name: دليل الحاج
- Version: 1.0.0
- Developer: تطبيق إرشادي شامل للحجاج

---

### 9. 🎨 واجهة المستخدم / User Interface

#### Quick Access Navigation (الصفحة الرئيسية):
Four prominent buttons in 2x2 grid:
- 🗺️ **الخريطة** (Map) - Orange #E67E22
- 🔍 **البحث** (Search) - Blue #3498DB
- ✅ **القائمة** (Checklist) - Green #27AE60
- ⚙️ **الإعدادات** (Settings) - Purple #9B59B6

#### Design Features:
- **Rounded corners** on all UI elements
- **Color-coded categories** for easy identification
- **Icons** for visual recognition
- **2-column grid layout** throughout
- **RTL support** for Arabic text
- **Consistent Darija** UI labels

---

## 🏗️ التقنيات المستخدمة / Technologies

### Framework:
- **.NET MAUI 9.0** (net9.0-android)
- **C#** with XAML
- **Android API 21+** (Android 5.0+)
- **Target SDK: 35** (Android 14+)

### NuGet Packages:
- `Microsoft.Maui.Controls` 9.0.60
- `Microsoft.Maui.Controls.Maps` 9.0.60
- `CommunityToolkit.Maui` 9.1.0
- `CommunityToolkit.Maui.MediaElement` 4.1.0

### Architecture:
- **MVVM-lite pattern**
- **Service layer**: DataService, FavoritesService
- **JSON data storage**: categories.json
- **Persistent settings**: Preferences API

---

## 📂 هيكل البيانات / Data Structure

### categories.json:
```json
{
  "categories": [
    {
      "id": 1,
      "nameAr": "التحضير للحج",
      "icon": "✈️",
      "color": "#3498DB",
      "subCategories": [
        {
          "id": 1,
          "nameAr": "الوثائق المطلوبة",
          "icon": "📄",
          "content": "...",
          "hasAudio": true
        },
        ...
      ]
    },
    ...
  ]
}
```

**Structure:**
- 6 categories
- 24 subcategories (4 per category)
- Each with icon, color, and detailed content
- Audio flags for future expansion

---

## 🚀 كيفية الاستخدام / How to Use

### Building:
```bash
dotnet build -f net9.0-android
```

### Running on Emulator:
```bash
dotnet build -f net9.0-android -t:Run
```

### Generating APK:
```bash
dotnet publish -f net9.0-android -c Release
```

APK Location: `bin\Release\net9.0-android\publish\`

---

## 📝 ملاحظات / Notes

### Current Status:
✅ All features implemented
✅ Build successful (21 warnings about Frame obsolescence)
✅ Deployed to Android emulator
⏳ Audio files need to be added to Resources/Raw/audio/
⏳ Splash screen can be configured in .csproj

### Known Warnings:
- **Frame obsolescence**: Frame is deprecated in .NET 9, consider migrating to Border
- **XAML binding**: x:DataType can be added for compiled bindings (performance optimization)

---

## 🎯 المستقبل / Future Enhancements

Potential additions:
- 📱 iOS support
- 🌐 Online sync for content updates
- 📖 PDF export of favorites
- 🗣️ Multiple Arabic dialects
- 📍 GPS navigation during Hajj
- 📸 Photo gallery of holy sites
- 💬 Q&A section
- 🕌 Prayer times

---

## 📧 الدعم / Support

For issues or suggestions:
- Built with ❤️ for pilgrims
- May Allah accept your Hajj

---

**حج مبرور وسعي مشكور**
**تقبل الله منا ومنكم**
