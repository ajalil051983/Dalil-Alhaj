using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KhayratAlhaj.Services
{
    /// <summary>
    /// Service to fetch walking/driving routes from the OSRM public API.
    /// Returns real road-based geometry, distance, and duration.
    /// </summary>
    public class RoutingService
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15),
            DefaultRequestHeaders =
            {
                // OSM/OSRM usage policy requires an identifying User-Agent
                { "User-Agent", "KhayratAlhaj/1.0 (+https://github.com/khayrat-alhaj; hajj-guide-app)" }
            }
        };

        // The official OSRM demo server (router.project-osrm.org) only serves the "car"
        // profile — foot/bike requests there silently return car routing. FOSSGIS hosts
        // dedicated profile-specific endpoints that return real walking / biking geometry.
        private const string OsrmCarBaseUrl = "https://router.project-osrm.org/route/v1/driving";
        private const string OsrmFootBaseUrl = "https://routing.openstreetmap.de/routed-foot/route/v1/foot";
        private const string OsrmBikeBaseUrl = "https://routing.openstreetmap.de/routed-bike/route/v1/bike";

        /// <summary>
        /// Fetches a route from OSRM for the given waypoints.
        /// </summary>
        /// <param name="waypoints">List of (Latitude, Longitude) waypoints in order.</param>
        /// <param name="profile">"foot" for walking, "car"/"driving" for driving, "bike" for cycling.</param>
        /// <returns>A RouteResult with geometry coordinates, total distance, and duration. Null if the request fails.</returns>
        public async Task<RouteResult?> GetRouteAsync(List<(double Lat, double Lon)> waypoints, string profile = "foot")
        {
            try
            {
                // OSRM expects coordinates as lon,lat pairs separated by semicolons
                var coordString = string.Join(";", waypoints.Select(w => $"{w.Lon:F6},{w.Lat:F6}"));
                var baseUrl = profile switch
                {
                    "foot" or "walking" => OsrmFootBaseUrl,
                    "bike" or "cycling" => OsrmBikeBaseUrl,
                    _ => OsrmCarBaseUrl,
                };
                var url = $"{baseUrl}/{coordString}?overview=full&geometries=geojson";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var osrmResponse = JsonSerializer.Deserialize<OsrmResponse>(json);

                if (osrmResponse?.Code != "Ok" || osrmResponse.Routes == null || osrmResponse.Routes.Count == 0)
                    return null;

                var route = osrmResponse.Routes[0];
                var coordinates = route.Geometry?.Coordinates?
                    .Select(c => (Lat: c[1], Lon: c[0]))
                    .ToList() ?? new List<(double Lat, double Lon)>();

                return new RouteResult
                {
                    Coordinates = coordinates,
                    DistanceMeters = route.Distance,
                    DurationSeconds = route.Duration
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OSRM routing error: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Result of a routing API call.
    /// </summary>
    public class RouteResult
    {
        /// <summary>
        /// Ordered list of (Lat, Lon) coordinates forming the route polyline.
        /// </summary>
        public List<(double Lat, double Lon)> Coordinates { get; set; } = new();

        /// <summary>
        /// Total route distance in meters.
        /// </summary>
        public double DistanceMeters { get; set; }

        /// <summary>
        /// Total route duration in seconds.
        /// </summary>
        public double DurationSeconds { get; set; }

        /// <summary>
        /// Formatted distance string (e.g., "12.3 km" or "850 m").
        /// </summary>
        public string FormattedDistance
        {
            get
            {
                if (DistanceMeters < 1000)
                    return $"{DistanceMeters:F0} m";
                return $"{DistanceMeters / 1000.0:F1} km";
            }
        }

        /// <summary>
        /// Formatted duration string (e.g., "25 min" or "1.5 hr").
        /// </summary>
        public string FormattedDuration
        {
            get
            {
                var hours = DurationSeconds / 3600.0;
                if (hours < 1)
                    return $"{DurationSeconds / 60.0:F0} min";
                return $"{hours:F1} hr";
            }
        }
    }

    // ==================== OSRM JSON Response Models ====================

    internal class OsrmResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("routes")]
        public List<OsrmRoute>? Routes { get; set; }
    }

    internal class OsrmRoute
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }

        [JsonPropertyName("geometry")]
        public OsrmGeometry? Geometry { get; set; }
    }

    internal class OsrmGeometry
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("coordinates")]
        public List<double[]>? Coordinates { get; set; }
    }
}
