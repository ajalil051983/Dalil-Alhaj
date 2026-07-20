namespace KhayratAlhaj.Services
{
    public class QuranPageAssetService
    {
        private static readonly string[] SupportedExtensions = [".webp", ".png", ".jpg", ".jpeg"];
        private static readonly object CacheLock = new();
        private static Dictionary<int, string> pagePathCache = new();
        private static string cachedFolderPath = string.Empty;
        private static DateTime cachedFolderWriteTimeUtc = DateTime.MinValue;

        // Expected folder structure for downloaded package:
        // {AppData}/quran/warsh/pages/001.webp ... 604.webp
        public string GetPageImagePath(int mushafPageNumber)
        {
            if (mushafPageNumber <= 0)
            {
                return string.Empty;
            }

            var folder = GetPagesDirectoryPath();
            if (!Directory.Exists(folder))
            {
                return string.Empty;
            }

            EnsureCache(folder);
            return pagePathCache.TryGetValue(mushafPageNumber, out var path)
                ? path
                : string.Empty;
        }

        public string GetPagesDirectoryPath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, "quran", "warsh", "pages");
        }

        public int GetInstalledPageCount()
        {
            var folder = GetPagesDirectoryPath();
            if (!Directory.Exists(folder))
            {
                return 0;
            }

            EnsureCache(folder);
            return pagePathCache.Count;
        }

        public bool HasInstalledPages(int minimumPages = 200)
        {
            return GetInstalledPageCount() >= minimumPages;
        }

        public void InvalidateCache()
        {
            lock (CacheLock)
            {
                pagePathCache = new Dictionary<int, string>();
                cachedFolderPath = string.Empty;
                cachedFolderWriteTimeUtc = DateTime.MinValue;
            }
        }

        private static void EnsureCache(string folder)
        {
            var folderWriteTimeUtc = Directory.GetLastWriteTimeUtc(folder);

            lock (CacheLock)
            {
                if (cachedFolderPath == folder
                    && cachedFolderWriteTimeUtc == folderWriteTimeUtc
                    && pagePathCache.Count > 0)
                {
                    return;
                }

                var map = new Dictionary<int, string>();
                foreach (var extension in SupportedExtensions)
                {
                    foreach (var path in Directory.EnumerateFiles(folder, "*" + extension, SearchOption.TopDirectoryOnly))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(path);
                        if (!int.TryParse(fileName, out var pageNumber) || pageNumber <= 0)
                        {
                            continue;
                        }

                        if (!map.ContainsKey(pageNumber))
                        {
                            map[pageNumber] = path;
                        }
                    }
                }

                pagePathCache = map;
                cachedFolderPath = folder;
                cachedFolderWriteTimeUtc = folderWriteTimeUtc;
            }
        }
    }
}
