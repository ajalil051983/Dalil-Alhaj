"""
Audio Generator for Dalil AlHaj App
Generates Arabic audio narration for all subcategories using free TTS APIs

Options:
1. Google Text-to-Speech (gTTS) - Free, unlimited, good quality
2. Edge-TTS (Microsoft Edge TTS) - Free, excellent quality, multiple Arabic voices
3. pyttsx3 - Offline, but requires system TTS engine

Requirements:
    pip install gtts
    pip install edge-tts
    pip install pydub  # Optional: for audio processing
"""

import json
import os
import asyncio
from pathlib import Path

# ============================================================================
# OPTION 1: Using gTTS (Google Text-to-Speech) - Simplest
# ============================================================================
def generate_audio_gtts():
    """Generate audio using Google TTS - Free and simple"""
    try:
        from gtts import gTTS
    except ImportError:
        print("Please install gtts: pip install gtts")
        return
    
    # Load categories
    with open('categories.json', 'r', encoding='utf-8') as f:
        categories = json.load(f)
    
    # Create audio folder
    audio_dir = Path('audio')
    audio_dir.mkdir(exist_ok=True)
    
    count = 0
    for category in categories:
        cat_id = category['id']
        for subcategory in category.get('subcategories', []):
            sub_id = subcategory['id']
            
            # Combine title and content for full narration
            title = subcategory.get('nameAr', '')
            content = subcategory.get('content', '')
            full_text = f"{title}. {content}"
            
            # Generate audio
            filename = f'{cat_id}_{sub_id}.mp3'
            filepath = audio_dir / filename
            
            print(f"Generating {filename}...")
            try:
                tts = gTTS(text=full_text, lang='ar', slow=False)
                tts.save(str(filepath))
                count += 1
                print(f"  ✓ Created {filename}")
            except Exception as e:
                print(f"  ✗ Failed {filename}: {e}")
    
    print(f"\n✓ Generated {count} audio files in '{audio_dir}' folder")
    print("Copy these files to: DalilAlHaj/Resources/Raw/audio/")


# ============================================================================
# OPTION 2: Using Edge-TTS (Microsoft Edge) - Best Quality
# ============================================================================
async def generate_audio_edge():
    """Generate audio using Microsoft Edge TTS - Excellent quality"""
    try:
        import edge_tts
    except ImportError:
        print("Please install edge-tts: pip install edge-tts")
        return
    
    # Load categories
    with open('categories.json', 'r', encoding='utf-8') as f:
        categories = json.load(f)
    
    # Create audio folder
    audio_dir = Path('audio')
    audio_dir.mkdir(exist_ok=True)
    
    # Available Arabic voices (choose one):
    # "ar-SA-HamedNeural" - Male, Saudi
    # "ar-SA-ZariyahNeural" - Female, Saudi
    # "ar-EG-SalmaNeural" - Female, Egyptian
    # "ar-EG-ShakirNeural" - Male, Egyptian
    VOICE = "ar-SA-HamedNeural"  # Male Saudi voice - formal and clear
    
    count = 0
    for category in categories:
        cat_id = category['id']
        for subcategory in category.get('subcategories', []):
            sub_id = subcategory['id']
            
            # Combine title and content
            title = subcategory.get('nameAr', '')
            content = subcategory.get('content', '')
            full_text = f"{title}. {content}"
            
            # Generate audio
            filename = f'{cat_id}_{sub_id}.mp3'
            filepath = audio_dir / filename
            
            print(f"Generating {filename}...")
            try:
                communicate = edge_tts.Communicate(full_text, VOICE)
                await communicate.save(str(filepath))
                count += 1
                print(f"  ✓ Created {filename}")
            except Exception as e:
                print(f"  ✗ Failed {filename}: {e}")
    
    print(f"\n✓ Generated {count} audio files in '{audio_dir}' folder")
    print("Copy these files to: DalilAlHaj/Resources/Raw/audio/")


# ============================================================================
# OPTION 3: Using pyttsx3 - Offline (requires system TTS)
# ============================================================================
def generate_audio_pyttsx3():
    """Generate audio using system TTS - Offline but quality depends on system"""
    try:
        import pyttsx3
    except ImportError:
        print("Please install pyttsx3: pip install pyttsx3")
        return
    
    engine = pyttsx3.init()
    
    # Try to set Arabic voice (might not work on all systems)
    voices = engine.getProperty('voices')
    for voice in voices:
        if 'arabic' in voice.name.lower() or 'ar' in voice.languages:
            engine.setProperty('voice', voice.id)
            break
    
    # Load categories
    with open('categories.json', 'r', encoding='utf-8') as f:
        categories = json.load(f)
    
    # Create audio folder
    audio_dir = Path('audio')
    audio_dir.mkdir(exist_ok=True)
    
    count = 0
    for category in categories:
        cat_id = category['id']
        for subcategory in category.get('subcategories', []):
            sub_id = subcategory['id']
            
            title = subcategory.get('nameAr', '')
            content = subcategory.get('content', '')
            full_text = f"{title}. {content}"
            
            filename = f'{cat_id}_{sub_id}.mp3'
            filepath = audio_dir / filename
            
            print(f"Generating {filename}...")
            try:
                engine.save_to_file(full_text, str(filepath))
                engine.runAndWait()
                count += 1
                print(f"  ✓ Created {filename}")
            except Exception as e:
                print(f"  ✗ Failed {filename}: {e}")
    
    print(f"\n✓ Generated {count} audio files in '{audio_dir}' folder")


# ============================================================================
# Main Menu
# ============================================================================
if __name__ == "__main__":
    print("=" * 70)
    print("Audio Generator for Dalil AlHaj")
    print("=" * 70)
    print("\nChoose TTS engine:")
    print("1. gTTS (Google) - Simple, free, good quality")
    print("2. Edge-TTS (Microsoft) - RECOMMENDED - Best quality, multiple voices")
    print("3. pyttsx3 (Offline) - Requires system Arabic TTS")
    print()
    
    choice = input("Enter choice (1/2/3) [2]: ").strip() or "2"
    
    print("\nStarting audio generation...\n")
    
    if choice == "1":
        generate_audio_gtts()
    elif choice == "2":
        # Edge-TTS requires async
        asyncio.run(generate_audio_edge())
    elif choice == "3":
        generate_audio_pyttsx3()
    else:
        print("Invalid choice")
    
    print("\nDone! Next steps:")
    print("1. Check the 'audio' folder for generated MP3 files")
    print("2. Copy files to: DalilAlHaj/Resources/Raw/audio/")
    print("3. Rebuild the app")
