using SQLite;
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

        public int? SurahNumber { get; set; }
        public string ApiLookupName { get; set; } = string.Empty;

        public bool HasAudioAr { get; set; }
        public bool HasAudioEn { get; set; }
        public bool HasAudioFr { get; set; }
    }

    // ---------------------------------------------------------------------------
    // SQLite entity – DhikrEntity table
    // ---------------------------------------------------------------------------
    [Table("DhikrEntity")]
    internal class DhikrEntity
    {
        [PrimaryKey]
        [Column("id")]
        public int Id { get; set; }

        [Column("type")]
        public string Type { get; set; } = string.Empty;

        [Column("text")]
        public string Text { get; set; } = string.Empty;

        [Column("enTranslation")]
        public string EnTranslation { get; set; } = string.Empty;

        [Column("frTranslation")]
        public string FrTranslation { get; set; } = string.Empty;

        [Column("times")]
        public int Times { get; set; }
    }

    // ---------------------------------------------------------------------------
    // AppDb – shared encrypted connection (used by DatabaseService & LocationRepository)
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Singleton that owns the single SQLCipher connection to the combined app database
    /// (<c>appdata.bin</c>).  Both <see cref="DatabaseService"/> and
    /// <c>LocationRepository</c> call <see cref="GetAsync"/> instead of opening their
    /// own connections, so the file is opened only once per app session.
    /// </summary>
    internal static class AppDb
    {
        internal const string FileName = "appdata.bin";
        private static readonly string[] RequiredTables = { "Categories", "SubCategories", "LocationEntity" };
        private const int MinimumCategoryCount = 4;

        /// <summary>
        /// Bump this number every time the bundled appdata.bin content changes.
        /// When the app detects a mismatch with the stored value, the local DB is
        /// replaced with the fresh bundled copy.
        /// </summary>
        private const int BundledDbVersion = 3;

        private static SQLiteAsyncConnection? _connection;
        private static readonly SemaphoreSlim _lock = new(1, 1);

        internal static async Task<SQLiteAsyncConnection> GetAsync()
        {
            if (_connection != null) return _connection;

            await _lock.WaitAsync();
            try
            {
                if (_connection != null) return _connection;

                var dbPath = Path.Combine(FileSystem.AppDataDirectory, FileName);
                System.Diagnostics.Debug.WriteLine(
                    $"[AppDb] {dbPath} (exists: {File.Exists(dbPath)})");

                var needsCopy = false;

                if (!File.Exists(dbPath))
                {
                    needsCopy = true;
                    System.Diagnostics.Debug.WriteLine("[AppDb] No local database found.");
                }
                else if (!IsDatabaseCompatible(dbPath))
                {
                    needsCopy = true;
                    System.Diagnostics.Debug.WriteLine("[AppDb] Existing local database is stale/incompatible.");
                }
                else if (IsBundledDbNewer())
                {
                    needsCopy = true;
                    System.Diagnostics.Debug.WriteLine("[AppDb] Bundled database is newer than local copy.");
                }

                if (needsCopy)
                {
                    if (File.Exists(dbPath)) File.Delete(dbPath);
                    await CopyBundledDatabaseAsync(dbPath);
                    Preferences.Default.Set("db_version", BundledDbVersion);
                    System.Diagnostics.Debug.WriteLine($"[AppDb] Database replaced with bundled version {BundledDbVersion}.");
                }

                _connection = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite);
                await EnsureSubCategoriesSchemaAsync(_connection);
            }
            finally
            {
                _lock.Release();
            }

            return _connection!;
        }

        private static async Task CopyBundledDatabaseAsync(string dbPath)
        {
            using var asset = await FileSystem.OpenAppPackageFileAsync(FileName);
            using var file = File.Create(dbPath);
            await asset.CopyToAsync(file);
        }

        private static bool IsDatabaseCompatible(string dbPath)
        {
            try
            {
                using var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);

                var tableNames = db.QueryScalars<string>("SELECT name FROM sqlite_master WHERE type='table';");
                var hasRequiredTables = RequiredTables.All(tableNames.Contains);
                if (!hasRequiredTables)
                {
                    return false;
                }

                var categoryCount = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Categories;");
                return categoryCount >= MinimumCategoryCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppDb] Compatibility check failed: {ex.Message}");
                return false;
            }
        }

        private static bool IsBundledDbNewer()
        {
            var storedVersion = Preferences.Default.Get("db_version", 0);
            return BundledDbVersion > storedVersion;
        }

        private static async Task EnsureSubCategoriesSchemaAsync(SQLiteAsyncConnection connection)
        {
            const int targetVersion = 3;

            var versionRows = await connection.QueryAsync<PragmaIntResult>("PRAGMA user_version;");
            var version = versionRows.FirstOrDefault()?.Value ?? 0;
            if (version >= targetVersion)
            {
                return;
            }

            var tableInfo = await connection.QueryAsync<PragmaTableInfoResult>("PRAGMA table_info(SubCategories);");
            var columnNames = new HashSet<string>(tableInfo.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);

            var hasAllAudioColumns = columnNames.Contains("HasAudioAr") && columnNames.Contains("HasAudioEn") && columnNames.Contains("HasAudioFr");
            var hasSurahColumns = columnNames.Contains("SurahNumber") && columnNames.Contains("ApiLookupName");

            if (hasAllAudioColumns && hasSurahColumns)
            {
                await PopulateQuranMetadataFallbackAsync(connection);
                await connection.ExecuteAsync($"PRAGMA user_version = {targetVersion};");
                return;
            }

            await connection.ExecuteAsync("BEGIN TRANSACTION;");
            try
            {
                if (!hasAllAudioColumns)
                {
                    await connection.ExecuteAsync(@"
                        CREATE TABLE IF NOT EXISTS SubCategories_new (
                            Id INTEGER PRIMARY KEY,
                            CategoryId INTEGER NOT NULL,
                            NameAr TEXT NOT NULL DEFAULT '',
                            NameEn TEXT NOT NULL DEFAULT '',
                            NameFr TEXT NOT NULL DEFAULT '',
                            Icon TEXT NOT NULL DEFAULT '📖',
                            ContentAr TEXT NOT NULL DEFAULT '',
                            ContentEn TEXT NOT NULL DEFAULT '',
                            ContentFr TEXT NOT NULL DEFAULT '',
                            SurahNumber INTEGER NULL,
                            ApiLookupName TEXT NOT NULL DEFAULT '',
                            HasAudioAr INTEGER NOT NULL DEFAULT 0,
                            HasAudioEn INTEGER NOT NULL DEFAULT 0,
                            HasAudioFr INTEGER NOT NULL DEFAULT 0
                        );
                    ");

                    var hasLegacyHasAudio = columnNames.Contains("HasAudio");
                    var selectHasAudioAr = columnNames.Contains("HasAudioAr") ? "HasAudioAr" : (hasLegacyHasAudio ? "HasAudio" : "0");
                    var selectHasAudioEn = columnNames.Contains("HasAudioEn") ? "HasAudioEn" : "0";
                    var selectHasAudioFr = columnNames.Contains("HasAudioFr") ? "HasAudioFr" : "0";
                    var selectSurahNumber = columnNames.Contains("SurahNumber") ? "SurahNumber" : "NULL";
                    var selectApiLookupName = columnNames.Contains("ApiLookupName") ? "ApiLookupName" : "''";

                    await connection.ExecuteAsync($@"
                        INSERT INTO SubCategories_new
                        (Id, CategoryId, NameAr, NameEn, NameFr, Icon, ContentAr, ContentEn, ContentFr, SurahNumber, ApiLookupName, HasAudioAr, HasAudioEn, HasAudioFr)
                        SELECT
                        Id, CategoryId, NameAr, NameEn, NameFr, Icon, ContentAr, ContentEn, ContentFr,
                        {selectSurahNumber}, IFNULL({selectApiLookupName}, ''),
                        IFNULL({selectHasAudioAr}, 0), IFNULL({selectHasAudioEn}, 0), IFNULL({selectHasAudioFr}, 0)
                        FROM SubCategories;
                    ");

                    await connection.ExecuteAsync("DROP TABLE SubCategories;");
                    await connection.ExecuteAsync("ALTER TABLE SubCategories_new RENAME TO SubCategories;");
                    await connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_SubCategories_CategoryId ON SubCategories(CategoryId);");
                }

                if (!columnNames.Contains("SurahNumber"))
                {
                    await connection.ExecuteAsync("ALTER TABLE SubCategories ADD COLUMN SurahNumber INTEGER NULL;");
                }

                if (!columnNames.Contains("ApiLookupName"))
                {
                    await connection.ExecuteAsync("ALTER TABLE SubCategories ADD COLUMN ApiLookupName TEXT NOT NULL DEFAULT '';");
                }

                await PopulateQuranMetadataFallbackAsync(connection);
                await connection.ExecuteAsync($"PRAGMA user_version = {targetVersion};");
                await connection.ExecuteAsync("COMMIT;");
            }
            catch
            {
                await connection.ExecuteAsync("ROLLBACK;");
                throw;
            }
        }

                private static async Task PopulateQuranMetadataFallbackAsync(SQLiteAsyncConnection connection)
                {
                        await connection.ExecuteAsync(@"
                                UPDATE SubCategories
                                SET SurahNumber = Id - 400
                                WHERE CategoryId = 4
                                    AND Id BETWEEN 401 AND 514
                                    AND (SurahNumber IS NULL OR SurahNumber = 0);
                        ");

                        await connection.ExecuteAsync(@"
                                UPDATE SubCategories
                                SET ApiLookupName = NameEn
                                WHERE CategoryId = 4
                                    AND (ApiLookupName IS NULL OR ApiLookupName = '');
                        ");
                }

        private class PragmaTableInfoResult
        {
            [Column("name")]
            public string Name { get; set; } = string.Empty;
        }

        private class PragmaIntResult
        {
            [Column("user_version")]
            public int Value { get; set; }
        }
    }

    // ---------------------------------------------------------------------------
    // DatabaseService
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Provides encrypted SQLite-backed persistence for category and subcategory data.
    /// Uses the shared <see cref="AppDb"/> connection so only one file handle is open.
    /// </summary>
    public class DatabaseService
    {
        private static Task<SQLiteAsyncConnection> GetDatabaseAsync() => AppDb.GetAsync();

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

        /// <summary>
        /// Returns Dhikr entries filtered by type (e.g., Morning/Evening) with localized text.
        /// </summary>
        public async Task<List<DhikrItem>> GetDhikrsByTypeAsync(string dhikrType, string language)
        {
            var db = await GetDatabaseAsync();

            if (string.IsNullOrWhiteSpace(dhikrType))
            {
                return new List<DhikrItem>();
            }

            var normalizedType = dhikrType.Trim();

            var entities = await db.Table<DhikrEntity>()
                .Where(d => d.Type == normalizedType)
                .OrderBy(d => d.Id)
                .ToListAsync();

            return entities.Select(d => MapDhikrItem(d, language)).ToList();
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
                SurahNumber = se.SurahNumber,
                ApiLookupName = se.ApiLookupName,
                HasAudioAr = se.HasAudioAr,
                HasAudioEn = se.HasAudioEn,
                HasAudioFr = se.HasAudioFr
            };
        }

        private static DhikrItem MapDhikrItem(DhikrEntity entity, string language)
        {
            return new DhikrItem
            {
                Id = entity.Id,
                Type = entity.Type,
                Text = entity.Text,
                EnTranslation = entity.EnTranslation,
                FrTranslation = entity.FrTranslation,
                InitialTimes = entity.Times > 0 ? entity.Times : 1
            };
        }
    }
}
