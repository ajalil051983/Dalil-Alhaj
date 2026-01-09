# Dalil AlHaj — Hajj Guide (English)

![App Icon](DalilAlHaj/Resources/Images/kaaba.svg)

A comprehensive mobile guide to performing the rituals of Hajj according to the Maliki school of thought. The app content is authored in Moroccan Arabic (Darija) and includes English and French translations.

---

## Overview

Dalil AlHaj is a cross-platform mobile application built with .NET MAUI that helps pilgrims perform Hajj correctly following the Maliki madhhab. It organizes the ritual instructions into categories and subcategories, provides audio narration, an interactive map, a checklist, search, and localization.

---

## Key Features

- Educational content organized into 6 main categories and 24 subcategories.
- Interactive map showing major Hajj sites and routes.
- Fast search with instant results while typing.
- Hajj checklist with progress tracking and autosave.
- Audio narration (600+ MP3 files) with integrated playback controls.
- Favorites system for saving frequently used items.
- Copy/share content to other apps.
- Settings: language selection (Arabic, English, Français), font size, and dark mode.
- Modern, accessible UI with full RTL support for Arabic.

---

## Technical Architecture

**Technologies used**

- .NET MAUI 10.0
- C# 12
- XAML
- Mapsui (maps)
- CommunityToolkit.Maui
- CommunityToolkit.Maui.MediaElement (audio playback)

**Supported platforms**

- Android (API 21+)
- iOS (iOS 11+)
- Windows (Windows 10+)
- macOS (work in progress)

**Project layout**

See the `DalilAlHaj/` folder for pages, models, services, resources and platform-specific code.

---

## Main Categories (Summary)

- Preparation for Hajj
- Ihram & Miqat
- Pillars of Hajj
- Obligations of Hajj
- Common Mistakes
- Recommended Supplications

Total: 6 main categories × 4 subcategories each = 24 topics

---

## Data & Audio

- JSON categories are in `DalilAlHaj/Resources/Raw/` (`categories.json`, `categories-en.json`, `categories-fr.json`).
- Audio files are stored under `DalilAlHaj/Resources/Raw/audio/` and follow the naming pattern `audio/{CategoryId}_{SubCategoryId}.mp3` (600+ files).

JSON example:

```json
{
  "Id": "1",
  "Name": "Preparation for Hajj",
  "NameEn": "Hajj Preparation",
  "NameFr": "Préparation du Hajj",
  "Icon": "✈️",
  "SubCategories": [ ... ]
}
```

---

## Getting Started

### Prerequisites

- Visual Studio 2022 (17.8+) or later with .NET MAUI workload
- .NET 10.0 SDK
- Android SDK for Android builds
- Xcode for iOS builds (macOS)

### Build & Run

Clone the repository, restore packages and build:

```bash
git clone https://github.com/yourusername/dalil-alhaj.git
cd dalil-alhaj
dotnet restore
dotnet build
```

Run on a specific target:

```bash
# Android
dotnet build -t:Run -f net10.0-android

# iOS
dotnet build -t:Run -f net10.0-ios

# Windows
dotnet build -t:Run -f net10.0-windows10.0.26100.0
```

---

## Screenshots

Screenshots are available in `docs/screenshots/` (main, subcategory, content, map, search, checklist).

---

## Contributing

Contributions are welcome. Suggested workflow:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/YourFeature`
3. Commit changes: `git commit -m "Add some feature"
4. Push and open a Pull Request

Please follow the existing project style and test on at least one supported platform.

---

## License

This project is licensed under the MIT License. See the LICENSE file for details.

---

## Developer

Abdeljalil El Yasni

- GitHub: https://github.com/AbdeljalilElYasni
- Email: your.email@example.com

---

## Acknowledgments

- Content based on the Maliki school
- Icons: Emoji
- Fonts: Google Fonts (Tajawal, Open Sans)
- Thanks to the .NET MAUI community

---

## Support

- Email: support@dalilalhaj.com
- Issues: https://github.com/yourusername/dalil-alhaj/issues
- Discussions: https://github.com/yourusername/dalil-alhaj/discussions

---

## Roadmap

- v1.0.0 released
- Planned: more languages (Urdu, Indonesian), video tutorials, offline-first mode, cross-device sync, AR features, notifications

---

May Allah accept your Hajj and efforts.

© 2025 AbdeljalilElYasni. All rights reserved.
