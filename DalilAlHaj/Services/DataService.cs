using System.Text.Json;
using DalilAlHaj.Models;

namespace DalilAlHaj.Services
{
    /// <summary>
    /// Provides data access services for managing categories and subcategories.
    /// Handles loading, caching, and retrieving category data from JSON files based on the current language.
    /// </summary>
    public class DataService
    {
        /// <summary>
        /// Cache dictionary to store categories by language code to avoid repeated file I/O operations.
        /// Key: language code (e.g., "en", "fr", "ar"), Value: list of categories for that language.
        /// </summary>
        private Dictionary<string, List<Category>> categoriesCache = new();

        /// <summary>
        /// Retrieves the list of categories for the current language.
        /// Uses cached data if available, otherwise loads from the appropriate JSON file.
        /// </summary>
        /// <returns>A list of categories with all language properties populated.</returns>
        public async Task<List<Category>> GetCategoriesAsync()
        {
            var currentLanguage = LocalizationService.GetCurrentLanguage();
            
            // Return cached data if available for the current language
            if (categoriesCache.ContainsKey(currentLanguage))
                return categoriesCache[currentLanguage];

            try
            {
                // Determine which JSON file to load based on current language
                string fileName = currentLanguage switch
                {
                    "en" => "categories-en.json",
                    "fr" => "categories-fr.json",
                    _ => "categories.json" // Arabic is default
                };

                // Load and deserialize the JSON file
                using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                
                var categories = JsonSerializer.Deserialize<List<Category>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Category>();

                // Normalize language properties to ensure all translations are available
                // This prevents missing translations by copying available values to empty properties
                foreach (var category in categories)
                {
                    if (category == null) continue;
                    
                    // Populate missing category name translations
                    if (string.IsNullOrEmpty(category.NameAr) && !string.IsNullOrEmpty(category.NameEn))
                    {
                        category.NameAr = category.NameEn;
                        category.NameFr = category.NameEn;
                    }
                    else if (string.IsNullOrEmpty(category.NameEn) && !string.IsNullOrEmpty(category.NameFr))
                    {
                        category.NameAr = category.NameFr;
                        category.NameEn = category.NameFr;
                    }
                    else if (string.IsNullOrEmpty(category.NameEn) && !string.IsNullOrEmpty(category.NameAr))
                    {
                        category.NameEn = category.NameAr;
                        category.NameFr = category.NameAr;
                    }

                    // Apply the same normalization logic to subcategories
                    if (category.Subcategories != null)
                    {
                        foreach (var subCategory in category.Subcategories)
                        {
                            if (subCategory == null) continue;
                            
                            // Populate missing subcategory name translations
                            if (string.IsNullOrEmpty(subCategory.NameAr) && !string.IsNullOrEmpty(subCategory.NameEn))
                            {
                                subCategory.NameAr = subCategory.NameEn;
                                subCategory.NameFr = subCategory.NameEn;
                            }
                            else if (string.IsNullOrEmpty(subCategory.NameEn) && !string.IsNullOrEmpty(subCategory.NameFr))
                            {
                                subCategory.NameAr = subCategory.NameFr;
                                subCategory.NameEn = subCategory.NameFr;
                            }
                            else if (string.IsNullOrEmpty(subCategory.NameEn) && !string.IsNullOrEmpty(subCategory.NameAr))
                            {
                                subCategory.NameEn = subCategory.NameAr;
                                subCategory.NameFr = subCategory.NameAr;
                            }
                        }
                    }
                }

                // Cache the processed categories for future requests
                categoriesCache[currentLanguage] = categories;
                return categories;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
                return new List<Category>();
            }
        }

        /// <summary>
        /// Clears the categories cache, forcing fresh data to be loaded on the next request.
        /// Useful when language changes or when data needs to be refreshed.
        /// </summary>
        public void ClearCache()
        {
            categoriesCache.Clear();
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
