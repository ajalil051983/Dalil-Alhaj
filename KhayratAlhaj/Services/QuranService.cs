using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace KhayratAlhaj.Services
{
    public class QuranService
    {
        public const string WarshEdition = "quran-uthmani-quran-academy";
        public const string TajweedEdition = "quran-tajweed";
        public const string TafsirMuyassarEdition = "ar.muyassar";

        private const string BaseUrl = "https://api.alquran.cloud/v1";
        private const string CachePrefix = "quran_surah_cache_";
        private const string MetaCacheKey = "quran_meta_surah_refs_v1";
        private const int MaxFetchAttempts = 3;
        private static readonly ConcurrentDictionary<string, QuranSurahData> MemoryCache = new();
        private static readonly ConcurrentDictionary<string, Task<QuranSurahData?>> InFlightFetches = new();
        private static readonly SemaphoreSlim MetaLoadLock = new(1, 1);
        private static List<QuranSurahReference>? SurahReferencesCache;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        public Task<QuranSurahData?> GetSurahAsync(int surahNumber, bool forceRefresh = false, string? editionIdentifier = null)
        {
            if (surahNumber < 1 || surahNumber > 114)
            {
                return Task.FromResult<QuranSurahData?>(null);
            }

            var targetEdition = string.IsNullOrWhiteSpace(editionIdentifier)
                ? WarshEdition
                : editionIdentifier;

            // Coalesce concurrent requests for the same surah+edition (e.g. a double tap that
            // pushes two QuranReaderPage instances) into a single in-flight network fetch instead
            // of firing duplicate slow HTTP calls that double the main-thread contention.
            var inFlightKey = BuildMemoryCacheKey(surahNumber, targetEdition);
            var fetchTask = InFlightFetches.GetOrAdd(inFlightKey, _ => FetchSurahAsync(surahNumber, targetEdition, forceRefresh));

            return AwaitAndClearInFlightAsync(inFlightKey, fetchTask);
        }

        private static async Task<QuranSurahData?> AwaitAndClearInFlightAsync(string inFlightKey, Task<QuranSurahData?> fetchTask)
        {
            try
            {
                return await fetchTask;
            }
            finally
            {
                InFlightFetches.TryRemove(new KeyValuePair<string, Task<QuranSurahData?>>(inFlightKey, fetchTask));
            }
        }

        private async Task<QuranSurahData?> FetchSurahAsync(int surahNumber, string targetEdition, bool forceRefresh)
        {
            if (!forceRefresh)
            {
                if (MemoryCache.TryGetValue(BuildMemoryCacheKey(surahNumber, targetEdition), out var memoryCached))
                {
                    return memoryCached;
                }

                var cached = await GetSurahFromCacheAsync(surahNumber, targetEdition);
                if (cached != null)
                {
                    MemoryCache[BuildMemoryCacheKey(surahNumber, targetEdition)] = cached;
                    return cached;
                }
            }

            try
            {
                var url = $"{BaseUrl}/surah/{surahNumber}/{targetEdition}";
                using var response = await GetWithRetryAsync(url, surahNumber, targetEdition);

                if (response == null)
                {
                    Log($"All fetch attempts failed: surah={surahNumber}, edition={targetEdition}");
                    return await GetSurahFromCacheAsync(surahNumber, targetEdition);
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Log($"Not found from API, falling back to cache: surah={surahNumber}, edition={targetEdition}");
                    return await GetSurahFromCacheAsync(surahNumber, targetEdition);
                }

                if (!response.IsSuccessStatusCode)
                {
                    Log($"Non-success status={(int)response.StatusCode} for surah={surahNumber}, edition={targetEdition}; falling back to cache");
                    return await GetSurahFromCacheAsync(surahNumber, targetEdition);
                }

                var json = await response.Content.ReadAsStringAsync();
                var payload = JsonSerializer.Deserialize<QuranApiResponse>(json, JsonOptions);

                if (payload?.Data == null || payload.Code != 200)
                {
                    return await GetSurahFromCacheAsync(surahNumber, targetEdition);
                }

                var mapped = new QuranSurahData
                {
                    Number = payload.Data.Number,
                    Name = payload.Data.Name ?? string.Empty,
                    EnglishName = payload.Data.EnglishName ?? string.Empty,
                    NumberOfAyahs = payload.Data.NumberOfAyahs,
                    EditionIdentifier = payload.Data.Edition?.Identifier ?? string.Empty,
                    Ayahs = payload.Data.Ayahs?
                        .OrderBy(a => a.NumberInSurah)
                        .Select(a => new QuranAyahData
                        {
                            NumberInSurah = a.NumberInSurah,
                            Text = a.Text ?? string.Empty,
                            Page = a.Page
                        })
                        .ToList() ?? new List<QuranAyahData>()
                };

                SaveSurahToCache(surahNumber, targetEdition, mapped);
                MemoryCache[BuildMemoryCacheKey(surahNumber, targetEdition)] = mapped;
                Log($"Fetched from API: surah={surahNumber}, edition={mapped.EditionIdentifier}, ayahs={mapped.NumberOfAyahs}");
                return mapped;
            }
            catch (Exception ex)
            {
                Log($"API error: surah={surahNumber}, edition={targetEdition}, errorType={ex.GetType().Name}, message={ex.Message}");
                return await GetSurahFromCacheAsync(surahNumber, targetEdition);
            }
        }

        public async Task<List<QuranSurahReference>> GetSurahReferencesAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && SurahReferencesCache is { Count: > 0 })
            {
                return SurahReferencesCache;
            }

            await MetaLoadLock.WaitAsync();
            try
            {
                if (!forceRefresh && SurahReferencesCache is { Count: > 0 })
                {
                    return SurahReferencesCache;
                }

                if (!forceRefresh)
                {
                    var cachedJson = Preferences.Get(MetaCacheKey, string.Empty);
                    if (!string.IsNullOrWhiteSpace(cachedJson))
                    {
                        var cachedRefs = JsonSerializer.Deserialize<List<QuranSurahReference>>(cachedJson, JsonOptions);
                        if (cachedRefs is { Count: > 0 })
                        {
                            SurahReferencesCache = cachedRefs;
                            return SurahReferencesCache;
                        }
                    }
                }

                var response = await HttpClient.GetAsync($"{BaseUrl}/meta");
                if (!response.IsSuccessStatusCode)
                {
                    return SurahReferencesCache ?? new List<QuranSurahReference>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var payload = JsonSerializer.Deserialize<QuranMetaResponse>(json, JsonOptions);
                if (payload?.Data?.Surahs?.References == null)
                {
                    return SurahReferencesCache ?? new List<QuranSurahReference>();
                }

                var pageStarts = new Dictionary<int, int>();
                var pageRefs = payload.Data.Pages?.References ?? new List<QuranMetaPageReference>();
                for (var index = 0; index < pageRefs.Count; index++)
                {
                    var surahNo = pageRefs[index].Surah;
                    if (!pageStarts.ContainsKey(surahNo))
                    {
                        pageStarts[surahNo] = index + 1;
                    }
                }

                var refs = payload.Data.Surahs.References
                    .OrderBy(r => r.Number)
                    .Select(r => new QuranSurahReference
                    {
                        Number = r.Number,
                        ArabicName = r.Name ?? string.Empty,
                        EnglishName = r.EnglishName ?? string.Empty,
                        NumberOfAyahs = r.NumberOfAyahs,
                        RevelationType = r.RevelationType ?? string.Empty,
                        StartPage = pageStarts.TryGetValue(r.Number, out var page) ? page : 0
                    })
                    .ToList();

                SurahReferencesCache = refs;
                Preferences.Set(MetaCacheKey, JsonSerializer.Serialize(refs, JsonOptions));
                return refs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QuranService] Meta API error: {ex.Message}");
                return SurahReferencesCache ?? new List<QuranSurahReference>();
            }
            finally
            {
                MetaLoadLock.Release();
            }
        }

        public Task<QuranSurahData?> GetSurahFromCacheAsync(int surahNumber, string? editionIdentifier = null)
        {
            try
            {
                var targetEdition = string.IsNullOrWhiteSpace(editionIdentifier)
                    ? WarshEdition
                    : editionIdentifier;

                var key = BuildCacheKey(surahNumber, targetEdition);
                var json = Preferences.Get(key, string.Empty);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return Task.FromResult<QuranSurahData?>(null);
                }

                var cached = JsonSerializer.Deserialize<QuranSurahData>(json, JsonOptions);
                if (cached != null)
                {
                    MemoryCache[BuildMemoryCacheKey(surahNumber, targetEdition)] = cached;
                }
                return Task.FromResult(cached);
            }
            catch
            {
                return Task.FromResult<QuranSurahData?>(null);
            }
        }

        private static void SaveSurahToCache(int surahNumber, string editionIdentifier, QuranSurahData data)
        {
            var key = BuildCacheKey(surahNumber, editionIdentifier);
            var json = JsonSerializer.Serialize(data, JsonOptions);
            Preferences.Set(key, json);
        }

        private static string BuildCacheKey(int surahNumber, string editionIdentifier)
        {
            return $"{CachePrefix}{editionIdentifier}_{surahNumber}";
        }

        private static string BuildMemoryCacheKey(int surahNumber, string editionIdentifier)
        {
            return $"{editionIdentifier}:{surahNumber}";
        }

        private static async Task<HttpResponseMessage?> GetWithRetryAsync(string url, int surahNumber, string editionIdentifier)
        {
            for (var attempt = 1; attempt <= MaxFetchAttempts; attempt++)
            {
                try
                {
                    Log($"Fetch attempt {attempt}/{MaxFetchAttempts}: surah={surahNumber}, edition={editionIdentifier}");
                    return await HttpClient.GetAsync(url);
                }
                catch (TaskCanceledException ex)
                {
                    Log($"Fetch timeout/cancel on attempt {attempt}: surah={surahNumber}, edition={editionIdentifier}, message={ex.Message}");
                }
                catch (HttpRequestException ex)
                {
                    Log($"Fetch http error on attempt {attempt}: surah={surahNumber}, edition={editionIdentifier}, message={ex.Message}");
                }

                if (attempt < MaxFetchAttempts)
                {
                    var backoffMs = 600 * attempt;
                    await Task.Delay(backoffMs);
                }
            }

            return null;
        }

        private static void Log(string message)
        {
            Debug.WriteLine($"[QuranService] {message}");
        }
    }

    public class QuranSurahData
    {
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public int NumberOfAyahs { get; set; }
        public string EditionIdentifier { get; set; } = string.Empty;
        public List<QuranAyahData> Ayahs { get; set; } = new();
    }

    public class QuranAyahData
    {
        public int NumberInSurah { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Page { get; set; }
    }

    public class QuranSurahReference
    {
        public int Number { get; set; }
        public string ArabicName { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public int NumberOfAyahs { get; set; }
        public string RevelationType { get; set; } = string.Empty;
        public int StartPage { get; set; }
    }

    internal class QuranApiResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("data")]
        public QuranApiSurah? Data { get; set; }
    }

    internal class QuranApiSurah
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("englishName")]
        public string? EnglishName { get; set; }

        [JsonPropertyName("numberOfAyahs")]
        public int NumberOfAyahs { get; set; }

        [JsonPropertyName("ayahs")]
        public List<QuranApiAyah>? Ayahs { get; set; }

        [JsonPropertyName("edition")]
        public QuranApiEdition? Edition { get; set; }
    }

    internal class QuranApiAyah
    {
        [JsonPropertyName("numberInSurah")]
        public int NumberInSurah { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }
    }

    internal class QuranApiEdition
    {
        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }
    }

    internal class QuranMetaResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("data")]
        public QuranMetaData? Data { get; set; }
    }

    internal class QuranMetaData
    {
        [JsonPropertyName("surahs")]
        public QuranMetaSurahs? Surahs { get; set; }

        [JsonPropertyName("pages")]
        public QuranMetaPages? Pages { get; set; }
    }

    internal class QuranMetaSurahs
    {
        [JsonPropertyName("references")]
        public List<QuranMetaSurahReference>? References { get; set; }
    }

    internal class QuranMetaSurahReference
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("englishName")]
        public string? EnglishName { get; set; }

        [JsonPropertyName("numberOfAyahs")]
        public int NumberOfAyahs { get; set; }

        [JsonPropertyName("revelationType")]
        public string? RevelationType { get; set; }
    }

    internal class QuranMetaPages
    {
        [JsonPropertyName("references")]
        public List<QuranMetaPageReference>? References { get; set; }
    }

    internal class QuranMetaPageReference
    {
        [JsonPropertyName("surah")]
        public int Surah { get; set; }

        [JsonPropertyName("ayah")]
        public int Ayah { get; set; }
    }
}
