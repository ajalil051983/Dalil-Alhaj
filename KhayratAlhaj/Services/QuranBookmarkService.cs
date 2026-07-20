using System.Text.Json;

namespace KhayratAlhaj.Services
{
    public class QuranBookmarkService
    {
        private const string BookmarksKey = "quran_bookmarks";
        private List<QuranBookmarkItem> bookmarks = new();

        public QuranBookmarkService()
        {
            Load();
        }

        public IReadOnlyList<QuranBookmarkItem> GetAll()
        {
            return bookmarks
                .OrderBy(b => b.SurahNumber)
                .ThenBy(b => b.AyahNumber)
                .ToList();
        }

        public bool IsBookmarked(int surahNumber, int ayahNumber)
        {
            return bookmarks.Any(b => b.SurahNumber == surahNumber && b.AyahNumber == ayahNumber);
        }

        public void Toggle(int surahNumber, int ayahNumber, string surahName, string ayahText)
        {
            var existing = bookmarks.FirstOrDefault(b => b.SurahNumber == surahNumber && b.AyahNumber == ayahNumber);
            if (existing != null)
            {
                bookmarks.Remove(existing);
            }
            else
            {
                bookmarks.Add(new QuranBookmarkItem
                {
                    SurahNumber = surahNumber,
                    AyahNumber = ayahNumber,
                    SurahName = surahName,
                    AyahText = ayahText,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            Save();
        }

        private void Load()
        {
            try
            {
                var json = Preferences.Get(BookmarksKey, string.Empty);
                if (string.IsNullOrWhiteSpace(json))
                {
                    bookmarks = new List<QuranBookmarkItem>();
                    return;
                }

                bookmarks = JsonSerializer.Deserialize<List<QuranBookmarkItem>>(json) ?? new List<QuranBookmarkItem>();
            }
            catch
            {
                bookmarks = new List<QuranBookmarkItem>();
            }
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(bookmarks);
            Preferences.Set(BookmarksKey, json);
        }
    }

    public class QuranBookmarkItem
    {
        public int SurahNumber { get; set; }
        public int AyahNumber { get; set; }
        public string SurahName { get; set; } = string.Empty;
        public string AyahText { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }

        public string DisplayTitle => $"{SurahName} ({SurahNumber}:{AyahNumber})";
    }
}
