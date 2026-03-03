using ZadAlhaj.Models;

namespace ZadAlhaj.Services
{
    /// <summary>
    /// Provides data access services for managing categories and subcategories.
    /// Delegates persistence to <see cref="DatabaseService"/> (SQLite) and keeps an
    /// in-memory cache per language to avoid redundant database round-trips.
    /// </summary>
    public class DataService
    {
        private readonly DatabaseService _db;

        /// <summary>
        /// Cache dictionary to store categories by language code.
        /// Key: language code (e.g., "en", "fr", "ar"), Value: list of categories for that language.
        /// </summary>
        private readonly Dictionary<string, List<Category>> _categoriesCache = new();

        public DataService()
        {
            _db = new DatabaseService();
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
    }
}
