using SQLite;
using ZadAlhaj.Models.PrayerTimes;

namespace ZadAlhaj.Services.PrayerTimes
{
    /// <summary>
    /// Reads location data from the embedded_data.db (Salaat First database).
    /// Uses sqlite-net-pcl ORM consistent with the project's DatabaseService pattern.
    /// </summary>
    public class LocationRepository
    {
        private SQLiteAsyncConnection? _database;
        private bool _initialized;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        /// <summary>
        /// Ensures the database is copied from app assets and opened.
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;

                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "embedded_data.db");

                // Copy from app package if not already present
                if (!File.Exists(dbPath))
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync("embedded_data.db");
                    using var fileStream = File.Create(dbPath);
                    await stream.CopyToAsync(fileStream);
                }

                _database = new SQLiteAsyncConnection(dbPath,
                    SQLiteOpenFlags.ReadOnly | SQLiteOpenFlags.SharedCache);

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Search locations by name (partial match). Returns up to 20 results.
        /// </summary>
        public async Task<List<LocationEntry>> SearchByNameAsync(string query)
        {
            await EnsureInitializedAsync();

            var results = await _database!.QueryAsync<LocationEntry>(
                "SELECT id, name, countryCode, latitude, longitude, altitude " +
                "FROM LocationEntity WHERE name LIKE ? LIMIT 20",
                $"%{query}%");

            return results;
        }

        /// <summary>
        /// Find the nearest location to given coordinates.
        /// Uses simple Euclidean distance on (lat×100, lon×100) — sufficient for nearest-city lookup.
        /// </summary>
        public async Task<LocationEntry?> GetNearestAsync(double latitude, double longitude)
        {
            await EnsureInitializedAsync();

            // Convert to the DB's integer scale (×100)
            var lat100 = latitude * 100.0;
            var lon100 = longitude * 100.0;

            var results = await _database!.QueryAsync<LocationEntry>(
                "SELECT id, name, countryCode, latitude, longitude, altitude " +
                "FROM LocationEntity " +
                "ORDER BY ((latitude - ?) * (latitude - ?) + (longitude - ?) * (longitude - ?)) " +
                "LIMIT 1",
                lat100, lat100, lon100, lon100);

            return results.FirstOrDefault();
        }
    }
}
