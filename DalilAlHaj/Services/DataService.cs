using System.Text.Json;
using DalilAlHaj.Models;

namespace DalilAlHaj.Services
{
    public class DataService
    {
        private Dictionary<string, List<Category>> categoriesCache = new();

        public async Task<List<Category>> GetCategoriesAsync()
        {
            var currentLanguage = LocalizationService.GetCurrentLanguage();
            
            if (categoriesCache.ContainsKey(currentLanguage))
                return categoriesCache[currentLanguage];

            try
            {
                string fileName = currentLanguage switch
                {
                    "en" => "categories-en.json",
                    "fr" => "categories-fr.json",
                    _ => "categories.json" // Arabic is default
                };

                using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                
                var categories = JsonSerializer.Deserialize<List<Category>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Category>();

                // Ensure all language properties are populated from the current language
                foreach (var category in categories)
                {
                    if (category == null) continue;
                    
                    // If nameAr is empty but nameEn or nameFr exists, copy to all properties
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

                    // Do the same for subcategories
                    if (category.Subcategories != null)
                    {
                        foreach (var subCategory in category.Subcategories)
                        {
                            if (subCategory == null) continue;
                            
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

                categoriesCache[currentLanguage] = categories;
                return categories;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
                return new List<Category>();
            }
        }

        public void ClearCache()
        {
            categoriesCache.Clear();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            var allCategories = await GetCategoriesAsync();
            return allCategories.FirstOrDefault(c => c.Id == id);
        }

        public async Task<SubCategory?> GetSubCategoryByIdAsync(int categoryId, int subCategoryId)
        {
            var category = await GetCategoryByIdAsync(categoryId);
            return category?.Subcategories.FirstOrDefault(sc => sc.Id == subCategoryId);
        }
    }
}
