using SQLite;
using KhayratAlhaj.Models.PrayerTimes;
using KhayratAlhaj.Services;

namespace KhayratAlhaj.Services.PrayerTimes
{
    /// <summary>
    /// Reads location data from the combined app database (LocationEntity table).
    /// Uses the shared <see cref="AppDb"/> connection consistent with DatabaseService.
    /// </summary>
    public class LocationRepository
    {
        private static Task<SQLiteAsyncConnection> GetDbAsync() => AppDb.GetAsync();

        /// <summary>
        /// Search locations by name (partial match). Returns up to 20 results.
        /// </summary>
        public async Task<List<LocationEntry>> SearchByNameAsync(string query)
        {
            try
            {
                var db = await GetDbAsync();

                var results = await db.QueryAsync<LocationEntry>(
                    "SELECT id, name, countryCode, latitude, longitude, altitude " +
                    "FROM LocationEntity WHERE name LIKE ? LIMIT 20",
                    $"%{query}%");

                return results;
            }
            catch (Exception ex)
            {
                if (IsMissingLocationTable(ex))
                {
                    System.Diagnostics.Debug.WriteLine("[LocationRepository] LocationEntity table missing. Returning empty search results.");
                    return new List<LocationEntry>();
                }

                throw;
            }
        }

        /// <summary>
        /// Find the nearest location to given coordinates.
        /// Uses simple Euclidean distance on (lat×100, lon×100) — sufficient for nearest-city lookup.
        /// </summary>
        public async Task<LocationEntry?> GetNearestAsync(double latitude, double longitude)
        {
            try
            {
                var db = await GetDbAsync();

                // Convert to the DB's integer scale (×100)
                var lat100 = latitude * 100.0;
                var lon100 = longitude * 100.0;

                var results = await db.QueryAsync<LocationEntry>(
                    "SELECT id, name, countryCode, latitude, longitude, altitude " +
                    "FROM LocationEntity " +
                    "ORDER BY ((latitude - ?) * (latitude - ?) + (longitude - ?) * (longitude - ?)) " +
                    "LIMIT 1",
                    lat100, lat100, lon100, lon100);

                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                if (IsMissingLocationTable(ex))
                {
                    System.Diagnostics.Debug.WriteLine("[LocationRepository] LocationEntity table missing. Returning null nearest location.");
                    return null;
                }

                throw;
            }
        }

        private static bool IsMissingLocationTable(Exception ex)
        {
            return ex.Message?.Contains("no such table: LocationEntity", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
