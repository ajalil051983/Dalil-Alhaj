# 🗺️ Maps Setup Guide / دليل إعداد الخرائط

## ⚠️ Current Issue / المشكلة الحالية

The app crashes when opening the Map page because Google Maps API key is missing.

التطبيق يتعطل عند فتح صفحة الخريطة لأن مفتاح Google Maps API مفقود.

---

## ✅ Solution / الحل

You have **3 options**:

### Option 1: Add Google Maps API Key (Recommended) ⭐

#### Steps / الخطوات:

1. **Get Free API Key** (مجاني):
   - Go to: https://console.cloud.google.com/google/maps-apis
   - Sign in with Google account
   - Create new project: "ZadAlhaj"
   - Enable "**Maps SDK for Android**"
   - Go to "**Credentials**" → "**Create Credentials**" → "**API Key**"
   - Copy your API key

2. **Add Key to AndroidManifest.xml**:
   
   Open: `Platforms/Android/AndroidManifest.xml`
   
   Find this line:
   ```xml
   <meta-data android:name="com.google.android.geo.API_KEY" android:value="YOUR_API_KEY_HERE"/>
   ```
   
   Replace `YOUR_API_KEY_HERE` with your actual API key:
   ```xml
   <meta-data android:name="com.google.android.geo.API_KEY" android:value="AIzaSyD_YOUR_ACTUAL_KEY_HERE"/>
   ```

3. **Rebuild and Run**:
   ```bash
   dotnet build -f net9.0-android -t:Run
   ```

#### Cost / التكلفة:
- ✅ **FREE** for most users
- Google gives $200/month free credit
- Maps API usage is very cheap for personal apps

---

### Option 2: Disable Map Feature Temporarily 🚫

If you don't want to get an API key right now:

1. **Comment out Map button** in `MainPage.xaml`:
   
   Find the Map button (around line 25) and wrap it in comments:
   ```xml
   <!-- TEMPORARILY DISABLED
   <Frame Grid.Row="0" Grid.Column="0"
          BackgroundColor="#E67E22"
          ...>
       <Label Text="🗺️ الخريطة" ... />
       <Frame.GestureRecognizers>
           <TapGestureRecognizer Clicked="OnMapClicked" />
       </Frame.GestureRecognizers>
   </Frame>
   -->
   ```

2. **Rebuild**:
   ```bash
   dotnet build -f net9.0-android -t:Run
   ```

---

### Option 3: Use Alternative Map Library 🔄

Use a community package that supports OpenStreetMap without API keys:

**Packages to consider:**
- `Mapsui` - Cross-platform map library
- `SkiaSharp` with custom map rendering

**Note:** This requires more code changes and is more complex.

---

## 🎯 Recommended Action / الإجراء الموصى به

**Get the Google Maps API key** (Option 1) because:
- ✅ Free for personal use
- ✅ Takes only 5 minutes
- ✅ Professional map experience
- ✅ No code changes needed
- ✅ Shows Hajj locations beautifully

---

## 📝 Notes / ملاحظات

### Current Map Features:
- 🕋 Al-Kaaba (الكعبة المشرفة)
- ⛰️ Arafat (عرفات)
- 🌙 Muzdalifah (مزدلفة)
- 🏕️ Mina (منى)
- 🚶 Safa & Marwa (الصفا والمروة)

### Permissions Added:
Already added to AndroidManifest.xml:
- `ACCESS_COARSE_LOCATION`
- `ACCESS_FINE_LOCATION`
- `INTERNET`
- `ACCESS_NETWORK_STATE`

---

## 🔍 Verify Setup / التحقق من الإعداد

After adding API key, check if map works:

1. Run app
2. Tap "🗺️ الخريطة" button
3. You should see:
   - Map centered on Makkah
   - 5 pins for holy sites
   - Zoom/pan working

If still crashes:
- Double-check API key is correct
- Verify "Maps SDK for Android" is enabled in Google Cloud Console
- Check AndroidManifest.xml syntax

---

## 💡 Alternative: Skip Map for Now

If you just want to test other features:
- All other features work fine without map
- Search, Checklist, Settings, Categories, Audio, Favorites all work
- You can add the map later when ready

---

## 🆘 Troubleshooting / حل المشاكل

### Error: "API key not found"
✅ Fixed by adding `<meta-data>` to AndroidManifest.xml

### Error: "API key not valid"
❌ Key is wrong or Maps SDK not enabled
→ Check key in Google Cloud Console

### Error: "Quota exceeded"
❌ Too many map requests (unlikely for personal app)
→ Check usage in Google Cloud Console

---

## 📧 Quick Start Command

After adding your API key:

```bash
cd "d:\Ai workspace\Dalil Alhaj\ZadAlhaj"
dotnet build -f net9.0-android -t:Run
```

---

**حظاً موفقاً! Good luck!** 🚀
