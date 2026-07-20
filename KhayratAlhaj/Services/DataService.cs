using KhayratAlhaj.Models;

namespace KhayratAlhaj.Services
{
    /// <summary>
    /// Provides data access services for managing categories and subcategories.
    /// Delegates persistence to <see cref="DatabaseService"/> (SQLite) and keeps an
    /// in-memory cache per language to avoid redundant database round-trips.
    /// </summary>
    public class DataService
    {
        private readonly DatabaseService _db;
        private readonly QuranService _quranService;

        /// <summary>
        /// Cache dictionary to store categories by language code.
        /// Key: language code (e.g., "en", "fr", "ar"), Value: list of categories for that language.
        /// </summary>
        private readonly Dictionary<string, List<Category>> _categoriesCache = new();
        private readonly Dictionary<string, List<DhikrItem>> _dhikrCache = new();

        public DataService()
        {
            _db = new DatabaseService();
            _quranService = new QuranService();
        }

        /// <summary>
        /// Retrieves the list of categories for the current language.
        /// Uses an in-memory cache per language; the underlying data comes from SQLite.
        /// </summary>
        /// <returns>A list of categories with all language properties populated.</returns>
        public async Task<List<Category>> GetCategoriesAsync()
        {
            var currentLanguage = LocalizationService.GetCurrentLanguage();

            // Return cached data if available for the current language
            if (_categoriesCache.TryGetValue(currentLanguage, out var cached))
                return cached;

            try
            {
                var categories = await _db.GetCategoriesAsync(currentLanguage);
                _categoriesCache[currentLanguage] = categories;
                return categories;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DataService] Error loading categories: {ex.Message}");
                return new List<Category>();
            }
        }

        /// <summary>
        /// Clears the in-memory cache, forcing a fresh database query on the next request.
        /// Call this after a language change.
        /// </summary>
        public void ClearCache()
        {
            _categoriesCache.Clear();
            _dhikrCache.Clear();
        }

        /// <summary>
        /// Retrieves a specific category by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the category.</param>
        /// <returns>The category with the specified ID, or null if not found.</returns>
        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            var allCategories = await GetCategoriesAsync();
            return allCategories.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Retrieves a specific subcategory by its parent category ID and subcategory ID.
        /// </summary>
        /// <param name="categoryId">The unique identifier of the parent category.</param>
        /// <param name="subCategoryId">The unique identifier of the subcategory.</param>
        /// <returns>The subcategory with the specified IDs, or null if not found.</returns>
        public async Task<SubCategory?> GetSubCategoryByIdAsync(int categoryId, int subCategoryId)
        {
            var category = await GetCategoryByIdAsync(categoryId);
            return category?.Subcategories.FirstOrDefault(sc => sc.Id == subCategoryId);
        }

        /// <summary>
        /// Returns Dhikr entries by DhikrEntity type (e.g., Morning or Evening)
        /// in the current app language.
        /// </summary>
        public async Task<List<DhikrItem>> GetDhikrsByTypeAsync(string dhikrType)
        {
            var currentLanguage = LocalizationService.GetCurrentLanguage();
            var cacheKey = $"{currentLanguage}:{dhikrType.Trim()}";

            if (_dhikrCache.TryGetValue(cacheKey, out var cachedDhikrs))
            {
                return cachedDhikrs;
            }

            try
            {
                var dhikrs = await _db.GetDhikrsByTypeAsync(dhikrType, currentLanguage);
                _dhikrCache[cacheKey] = dhikrs;
                return dhikrs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DataService] Error loading dhikr entries: {ex.Message}");
                return new List<DhikrItem>();
            }
        }

        public Task<QuranSurahData?> GetQuranSurahAsync(int surahNumber, bool forceRefresh = false, string? editionIdentifier = null)
        {
            return _quranService.GetSurahAsync(surahNumber, forceRefresh, editionIdentifier);
        }

        public Task<QuranSurahData?> GetCachedQuranSurahAsync(int surahNumber, string? editionIdentifier = null)
        {
            return _quranService.GetSurahFromCacheAsync(surahNumber, editionIdentifier);
        }

        public Task<List<QuranSurahReference>> GetQuranSurahReferencesAsync(bool forceRefresh = false)
        {
            return _quranService.GetSurahReferencesAsync(forceRefresh);
        }
    }
}
