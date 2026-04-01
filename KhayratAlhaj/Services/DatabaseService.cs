using SQLite;
using System.Text.Json;
using KhayratAlhaj.Models;

namespace KhayratAlhaj.Services
{
    // ---------------------------------------------------------------------------
    // SQLite entity – Categories table
    // ---------------------------------------------------------------------------
    [Table("Categories")]
    internal class CategoryEntity
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameFr { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = "#3498DB";
    }

    // ---------------------------------------------------------------------------
    // SQLite entity – SubCategories table
    // ---------------------------------------------------------------------------
    [Table("SubCategories")]
    internal class SubCategoryEntity
    {
        [PrimaryKey]
        public int Id { get; set; }

        [Indexed]
        public int CategoryId { get; set; }

        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameFr { get; set; } = string.Empty;
        public string Icon { get; set; } = "📖";

        /// <summary>Arabic content text.</summary>
        public string ContentAr { get; set; } = string.Empty;

        /// <summary>English content text.</summary>
        public string ContentEn { get; set; } = string.Empty;

        /// <summary>French content text.</summary>
        public string ContentFr { get; set; } = string.Empty;

        public bool HasAudio { get; set; }
    }

    // ---------------------------------------------------------------------------
    // DTOs used only for seeding from the three legacy JSON bundles
    // ---------------------------------------------------------------------------
    internal class CategoryJsonDto
    {
        public int Id { get; set; }
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public string? NameFr { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = "#3498DB";
        public List<SubCategoryJsonDto> Subcategories { get; set; } = new();
    }

    internal class SubCategoryJsonDto
    {
        public int Id { get; set; }
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public string? NameFr { get; set; }
        public string Icon { get; set; } = "📖";

        /// <summary>
        /// The content field from the source JSON – its language depends on which
        /// file is being loaded (Arabic, English or French).
        /// </summary>
        public string Content { get; set; } = string.Empty;
        public bool HasAudio { get; set; }
    }

    // ---------------------------------------------------------------------------
    // DatabaseService
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Provides SQLite-backed persistence for category and subcategory data.
    /// <para>
    /// On first run the service seeds the database from the three bundled JSON assets:
    /// <c>categories.json</c> (Arabic), <c>categories-en.json</c> (English) and
    /// <c>categories-fr.json</c> (French).  Subsequent launches read directly from
    /// SQLite – no JSON parsing overhead.
    /// </para>
    /// </summary>
    public class DatabaseService
    {
        private const string DbFileName = "KhayratAlhaj.db3";

        private SQLiteAsyncConnection? _database;
        private bool _initialized;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        // -----------------------------------------------------------------------
        // Initialisation & seeding
        // -----------------------------------------------------------------------

        private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
        {
            if (_database != null && _initialized)
                return _database;

            await _initLock.WaitAsync();
            try
            {
                if (_database != null && _initialized)
                    return _database;

                var dbPath = Path.Combine(FileSystem.AppDataDirectory, DbFileName);
               System.Diagnostics.Debug.WriteLine(
                $"[DB PATH] {dbPath} (exists: {File.Exists(dbPath)})");
                _database = new SQLiteAsyncConnection(
                    dbPath,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

                // Create tables (no-op if they already exist)
                await _database.CreateTableAsync<CategoryEntity>();
                await _database.CreateTableAsync<SubCategoryEntity>();

                // Seed only when the tables are empty (i.e. fresh install / upgrade)
                var count = await _database.Table<CategoryEntity>().CountAsync();
                if (count == 0)
                    await SeedDatabaseAsync(_database);

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }

            return _database;
        }

        /// <summary>
        /// Merges the three language JSON bundles and inserts all rows.
        /// </summary>
        private static async Task SeedDatabaseAsync(SQLiteAsyncConnection db)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var arCategories = await LoadJsonAsync("categories.json", options);
            var enCategories = await LoadJsonAsync("categories-en.json", options);
            var frCategories = await LoadJsonAsync("categories-fr.json", options);

            var enIndex = enCategories.ToDictionary(c => c.Id);
            var frIndex = frCategories.ToDictionary(c => c.Id);

            await db.RunInTransactionAsync(conn =>
            {
                foreach (var arCat in arCategories)
                {
                    enIndex.TryGetValue(arCat.Id, out var enCat);
                    frIndex.TryGetValue(arCat.Id, out var frCat);

                    conn.InsertOrReplace(new CategoryEntity
                    {
                        Id = arCat.Id,
                        NameAr = arCat.NameAr ?? string.Empty,
                        NameEn = enCat?.NameEn ?? string.Empty,
                        NameFr = frCat?.NameFr ?? string.Empty,
                        Icon = arCat.Icon,
                        Color = arCat.Color
                    });

                    var enSubIndex = (enCat?.Subcategories ?? new()).ToDictionary(s => s.Id);
                    var frSubIndex = (frCat?.Subcategories ?? new()).ToDictionary(s => s.Id);

                    foreach (var arSub in arCat.Subcategories)
                    {
                        enSubIndex.TryGetValue(arSub.Id, out var enSub);
                        frSubIndex.TryGetValue(arSub.Id, out var frSub);

                        conn.InsertOrReplace(new SubCategoryEntity
                        {
                            Id = arSub.Id,
                            CategoryId = arCat.Id,
                            NameAr = arSub.NameAr ?? string.Empty,
                            NameEn = enSub?.NameEn ?? string.Empty,
                            NameFr = frSub?.NameFr ?? string.Empty,
                            Icon = arSub.Icon,
                            ContentAr = arSub.Content,
                            ContentEn = enSub?.Content ?? string.Empty,
                            ContentFr = frSub?.Content ?? string.Empty,
                            HasAudio = arSub.HasAudio
                        });
                    }
                }
            });

            System.Diagnostics.Debug.WriteLine("[DatabaseService] Database seeded from JSON bundles.");
        }

        private static async Task<List<CategoryJsonDto>> LoadJsonAsync(string fileName, JsonSerializerOptions options)
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                return JsonSerializer.Deserialize<List<CategoryJsonDto>>(json, options) ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] Could not load '{fileName}': {ex.Message}");
                return new();
            }
        }

        // -----------------------------------------------------------------------
        // Public query API (mirrors the old DataService API)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns all categories with their subcategories.
        /// Content text is set to <paramref name="language"/> (ar / en / fr).
        /// </summary>
        public async Task<List<Category>> GetCategoriesAsync(string language)
        {
            var db = await GetDatabaseAsync();

            var categoryEntities = await db.Table<CategoryEntity>().ToListAsync();
            var subEntities = await db.Table<SubCategoryEntity>().ToListAsync();

            var subsByCategory = subEntities
                .GroupBy(s => s.CategoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return categoryEntities
                .Select(ce => MapCategory(ce, subsByCategory, language))
                .ToList();
        }

        /// <summary>Returns a single category (with subcategories) by id.</summary>
        public async Task<Category?> GetCategoryByIdAsync(int id, string language)
        {
            var db = await GetDatabaseAsync();

            var ce = await db.Table<CategoryEntity>()
                             .Where(c => c.Id == id)
                             .FirstOrDefaultAsync();
            if (ce == null) return null;

            var subs = await db.Table<SubCategoryEntity>()
                               .Where(s => s.CategoryId == id)
                               .ToListAsync();

            return new Category
            {
                Id = ce.Id,
                NameAr = ce.NameAr,
                NameEn = ce.NameEn,
                NameFr = ce.NameFr,
                Icon = ce.Icon,
                Color = ce.Color,
                Subcategories = subs.Select(se => MapSubCategory(se, language)).ToList()
            };
        }

        /// <summary>Returns a single subcategory by its id.</summary>
        public async Task<SubCategory?> GetSubCategoryByIdAsync(int subCategoryId, string language)
        {
            var db = await GetDatabaseAsync();
            var se = await db.Table<SubCategoryEntity>()
                             .Where(s => s.Id == subCategoryId)
                             .FirstOrDefaultAsync();
            return se == null ? null : MapSubCategory(se, language);
        }

        // -----------------------------------------------------------------------
        // Mapping helpers
        // -----------------------------------------------------------------------

        private static Category MapCategory(
            CategoryEntity ce,
            Dictionary<int, List<SubCategoryEntity>> subsByCategory,
            string language)
        {
            return new Category
            {
                Id = ce.Id,
                NameAr = ce.NameAr,
                NameEn = ce.NameEn,
                NameFr = ce.NameFr,
                Icon = ce.Icon,
                Color = ce.Color,
                Subcategories = subsByCategory.TryGetValue(ce.Id, out var subs)
                    ? subs.Select(se => MapSubCategory(se, language)).ToList()
                    : new List<SubCategory>()
            };
        }

        private static SubCategory MapSubCategory(SubCategoryEntity se, string language)
        {
            var content = language switch
            {
                "en" => !string.IsNullOrEmpty(se.ContentEn) ? se.ContentEn : se.ContentAr,
                "fr" => !string.IsNullOrEmpty(se.ContentFr) ? se.ContentFr : se.ContentAr,
                _    => !string.IsNullOrEmpty(se.ContentAr) ? se.ContentAr : se.ContentEn
            };

            return new SubCategory
            {
                Id = se.Id,
                NameAr = se.NameAr,
                NameEn = se.NameEn,
                NameFr = se.NameFr,
                Icon = se.Icon,
                Content = content,
                HasAudio = se.HasAudio
            };
        }
    }
}
