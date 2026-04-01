using KhayratAlhaj.Models.PrayerTimes;

namespace KhayratAlhaj.Services.PrayerTimes
{
    /// <summary>
    /// Facade service for computing prayer times.
    /// Wraps the calculation engine, location repository, and user preferences.
    /// </summary>
    public class PrayerTimeService
    {
        private readonly LocationRepository _locationRepo = new();

        // Preference keys
        private const string CalcMethodKey = "prayer_calc_method";
        private const string MathhabKey = "prayer_mathhab";
        private const string CityNameKey = "prayer_city_name";
        private const string CityCountryKey = "prayer_city_country";
        private const string CityLatKey = "prayer_city_lat";
        private const string CityLonKey = "prayer_city_lon";
        private const string CityAltKey = "prayer_city_alt";

        /// <summary>
        /// Get prayer times for a specific date using stored location, 
        /// or GPS if no stored location.
        /// </summary>
        public async Task<DayPrayerTimes?> GetPrayerTimesAsync(DateTime date)
        {
            var location = await GetStoredOrGpsLocationAsync();
            if (location == null) return null;

            var method = GetCalculationMethod();
            var resolvedMethod = (method == CalculationMethod.Auto)
                ? ResolveAutoMethod(location.CountryCode)
                : method;

            // Apply Mathhab override if the user has set Hanafi
            var mathhab = GetMathhab();
            var effectiveMethod = resolvedMethod;

            // Use the selected city's DST-aware UTC offset for the requested date.
            // This correctly handles Morocco reverting to UTC+0 during Ramadan,
            // European DST transitions, etc.
            var gmtOffset = GetCityUtcOffset(location.CountryCode, location.LongitudeActual, date);

            var result = PrayerTimeCalculator.Calculate(
                date,
                location.LatitudeActual,
                location.LongitudeActual,
                location.Altitude,
                effectiveMethod,
                gmtOffset);

            result.LocationName = location.Name;
            result.CountryCode = location.CountryCode;
            result.UtcOffsetHours = gmtOffset;

            return result;
        }

        /// <summary>
        /// Get prayer times for 7 consecutive days starting from the given date.
        /// </summary>
        public async Task<List<DayPrayerTimes>> GetWeeklyPrayerTimesAsync(DateTime startDate)
        {
            var results = new List<DayPrayerTimes>();
            for (int i = 0; i < 7; i++)
            {
                var times = await GetPrayerTimesAsync(startDate.AddDays(i));
                if (times != null)
                    results.Add(times);
            }
            return results;
        }

        /// <summary>
        /// Search cities by name.
        /// </summary>
        public async Task<List<LocationEntry>> SearchCitiesAsync(string query)
        {
            return await _locationRepo.SearchByNameAsync(query);
        }

        /// <summary>
        /// Save a selected city to preferences.
        /// </summary>
        public void SaveSelectedCity(LocationEntry city)
        {
            Preferences.Set(CityNameKey, city.Name);
            Preferences.Set(CityCountryKey, city.CountryCode);
            Preferences.Set(CityLatKey, city.Latitude);
            Preferences.Set(CityLonKey, city.Longitude);
            Preferences.Set(CityAltKey, city.Altitude);
        }

        /// <summary>
        /// Get the stored city name, or null if none stored.
        /// </summary>
        public string? GetStoredCityName()
        {
            var name = Preferences.Get(CityNameKey, string.Empty);
            return string.IsNullOrEmpty(name) ? null : name;
        }

        /// <summary>
        /// Clear stored city so GPS will be used on next call.
        /// </summary>
        public void ClearStoredCity()
        {
            Preferences.Remove(CityNameKey);
            Preferences.Remove(CityCountryKey);
            Preferences.Remove(CityLatKey);
            Preferences.Remove(CityLonKey);
            Preferences.Remove(CityAltKey);
        }

        /// <summary>
        /// Get the user's configured calculation method.
        /// </summary>
        public CalculationMethod GetCalculationMethod()
        {
            var value = Preferences.Get(CalcMethodKey, (int)CalculationMethod.Auto);
            return (CalculationMethod)value;
        }

        /// <summary>
        /// Save the selected calculation method.
        /// </summary>
        public void SetCalculationMethod(CalculationMethod method)
        {
            Preferences.Set(CalcMethodKey, (int)method);
        }

        /// <summary>
        /// Get the user's configured Mathhab.
        /// </summary>
        public Mathhab GetMathhab()
        {
            var value = Preferences.Get(MathhabKey, (int)Mathhab.Shafii);
            return (Mathhab)value;
        }

        /// <summary>
        /// Save the selected Mathhab.
        /// </summary>
        public void SetMathhab(Mathhab mathhab)
        {
            Preferences.Set(MathhabKey, (int)mathhab);
        }

        /// <summary>
        /// Gets the stored location or falls back to GPS detection.
        /// </summary>
        private async Task<LocationEntry?> GetStoredOrGpsLocationAsync()
        {
            // Check if we have a stored city
            var storedName = Preferences.Get(CityNameKey, string.Empty);
            if (!string.IsNullOrEmpty(storedName))
            {
                return new LocationEntry
                {
                    Name = storedName,
                    CountryCode = Preferences.Get(CityCountryKey, ""),
                    Latitude = Preferences.Get(CityLatKey, 0),
                    Longitude = Preferences.Get(CityLonKey, 0),
                    Altitude = Preferences.Get(CityAltKey, 0)
                };
            }

            // Fall back to GPS
            try
            {
                // Use Task.WhenAny to enforce a hard 5-second timeout on GPS request
                // This prevents hanging on emulators or devices with GPS issues.
                // Android requires the Geolocation call (and any internal permission request
                // it may trigger) to run on the main thread — hence InvokeOnMainThreadAsync.
                var gpsTask = MainThread.InvokeOnMainThreadAsync(() =>
                    Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5))));
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(6));

                var completedTask = await Task.WhenAny(gpsTask, timeoutTask);

                if (completedTask == gpsTask)
                {
                    var gpsLocation = await gpsTask;
                    if (gpsLocation != null)
                    {
                        var nearest = await _locationRepo.GetNearestAsync(
                            gpsLocation.Latitude, gpsLocation.Longitude);

                        if (nearest != null)
                        {
                            // Auto-save the detected city
                            SaveSelectedCity(nearest);
                            return nearest;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("GPS location timed out after 6 seconds");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GPS location error: {ex.Message}");
            }

            // Absolute fallback: Makkah
            return await _locationRepo.GetNearestAsync(21.4225, 39.8262);
        }

        /// <summary>
        /// Resolve AUTO to a specific method based on country code.
        /// Exact mapping from Salaat First's UtilsKt.resolve().
        /// </summary>
        public static CalculationMethod ResolveAutoMethod(string countryCode)
        {
            return countryCode.ToUpperInvariant() switch
            {
                "MA" => CalculationMethod.Morocco,
                "TR" => CalculationMethod.Dyanet,
                "TN" => CalculationMethod.Tunisia,
                "PS" => CalculationMethod.Palestine,
                "FR" => CalculationMethod.Uoif,
                "EG" or "LY" or "SD" => CalculationMethod.EgyptNew,
                "DZ" => CalculationMethod.Algier,
                "US" or "CA" => CalculationMethod.NorthAmerica,
                "KW" => CalculationMethod.Kuwait,
                "AE" => CalculationMethod.Uae,
                "BE" => CalculationMethod.BelgiumEmb,
                "SA" or "QA" or "BH" or "OM" or "JO" or "YE" => CalculationMethod.UmmAlQura,
                _ => CalculationMethod.MuslimLeague
            };
        }

        /// <summary>
        /// IANA timezone IDs keyed by ISO 3166-1 alpha-2 country code.
        /// Used for DST-aware UTC offset resolution via TimeZoneInfo.
        /// Countries with multiple timezone zones are omitted and handled by longitude fallback.
        /// </summary>
        private static readonly Dictionary<string, string> CountryIanaTimezones =
            new(StringComparer.OrdinalIgnoreCase)
        {
            // Arabian Peninsula (no DST observed)
            { "SA", "Asia/Riyadh" },   { "KW", "Asia/Kuwait" },   { "QA", "Asia/Qatar" },
            { "BH", "Asia/Bahrain" },  { "YE", "Asia/Aden" },     { "IQ", "Asia/Baghdad" },
            { "OM", "Asia/Muscat" },   { "AE", "Asia/Dubai" },
            // Levant / Near East
            { "JO", "Asia/Amman" },    { "SY", "Asia/Damascus" }, { "LB", "Asia/Beirut" },
            { "PS", "Asia/Gaza" },     { "IL", "Asia/Jerusalem" },
            // Iran / Afghanistan
            { "IR", "Asia/Tehran" },   { "AF", "Asia/Kabul" },
            // North Africa — Morocco (Africa/Casablanca) has a unique DST rule:
            //   standard time is UTC+1, but reverts to UTC+0 during Ramadan.
            //   TimeZoneInfo.FindSystemTimeZoneById("Africa/Casablanca").GetUtcOffset(date)
            //   returns the correct value based on the actual IANA tzdata on the device.
            { "EG", "Africa/Cairo" },  { "LY", "Africa/Tripoli" }, { "SD", "Africa/Khartoum" },
            { "TN", "Africa/Tunis" },  { "DZ", "Africa/Algiers" }, { "MA", "Africa/Casablanca" },
            // East Africa (no DST)
            { "SO", "Africa/Mogadishu" }, { "ET", "Africa/Addis_Ababa" },
            { "DJ", "Africa/Djibouti" },  { "ER", "Africa/Asmara" },
            { "KE", "Africa/Nairobi" },   { "TZ", "Africa/Dar_es_Salaam" },
            { "UG", "Africa/Kampala" },
            // West Africa (no DST; fixed UTC+0 or UTC+1)
            { "MR", "Africa/Nouakchott" }, { "ML", "Africa/Bamako" },
            { "SN", "Africa/Dakar" },      { "GM", "Africa/Banjul" },
            { "GN", "Africa/Conakry" },    { "GW", "Africa/Bissau" },
            { "SL", "Africa/Freetown" },   { "LR", "Africa/Monrovia" },
            { "CI", "Africa/Abidjan" },    { "BF", "Africa/Ouagadougou" },
            { "NE", "Africa/Niamey" },     { "NG", "Africa/Lagos" },
            { "TD", "Africa/Ndjamena" },   { "CM", "Africa/Douala" },
            // South / Southeast Asia
            { "PK", "Asia/Karachi" },    { "IN", "Asia/Kolkata" },
            { "BD", "Asia/Dhaka" },      { "MV", "Indian/Maldives" },
            { "MY", "Asia/Kuala_Lumpur" }, { "BN", "Asia/Brunei" },
            { "SG", "Asia/Singapore" },  { "ID", "Asia/Jakarta" },
            // Central Asia
            { "UZ", "Asia/Tashkent" },  { "TM", "Asia/Ashgabat" },
            { "TJ", "Asia/Dushanbe" },  { "KG", "Asia/Bishkek" },
            { "AZ", "Asia/Baku" },
            // Turkey
            { "TR", "Europe/Istanbul" },
            // Europe (all observe standard DST transitions in spring/autumn)
            { "GB", "Europe/London" },  { "FR", "Europe/Paris" },
            { "DE", "Europe/Berlin" },  { "BE", "Europe/Brussels" },
            { "NL", "Europe/Amsterdam" }, { "ES", "Europe/Madrid" },
            { "IT", "Europe/Rome" },    { "SE", "Europe/Stockholm" },
            { "NO", "Europe/Oslo" },    { "DK", "Europe/Copenhagen" },
            { "AT", "Europe/Vienna" },  { "CH", "Europe/Zurich" },
            { "PT", "Europe/Lisbon" },  { "FI", "Europe/Helsinki" },
            { "GR", "Europe/Athens" },  { "PL", "Europe/Warsaw" },
            // North America – omitted; multiple zones, handled by longitude fallback
        };

        /// <summary>
        /// Hardcoded UTC offsets as a last-resort fallback when IANA lookup fails.
        /// Values are standard (non-DST) offsets.
        /// </summary>
        private static readonly Dictionary<string, double> CountryUtcOffsetsFallback =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "SA", 3 },  { "KW", 3 },  { "QA", 3 },  { "BH", 3 },
            { "YE", 3 },  { "IQ", 3 },  { "OM", 4 },  { "AE", 4 },
            { "JO", 3 },  { "SY", 3 },  { "LB", 3 },  { "PS", 2 }, { "IL", 2 },
            { "IR", 3.5 },{ "AF", 4.5 },
            { "EG", 2 },  { "LY", 2 },  { "SD", 3 },  { "TN", 1 },
            { "DZ", 1 },  { "MA", 1 },
            { "SO", 3 },  { "ET", 3 },  { "DJ", 3 },  { "ER", 3 },
            { "KE", 3 },  { "TZ", 3 },  { "UG", 3 },
            { "MR", 0 },  { "ML", 0 },  { "SN", 0 },  { "GM", 0 },
            { "GN", 0 },  { "GW", 0 },  { "SL", 0 },  { "LR", 0 },
            { "CI", 0 },  { "BF", 0 },
            { "NE", 1 },  { "NG", 1 },  { "TD", 1 },  { "CM", 1 },
            { "PK", 5 },  { "IN", 5.5 },{ "BD", 6 },  { "MV", 5 },
            { "MY", 8 },  { "BN", 8 },  { "SG", 8 },  { "ID", 7 },
            { "UZ", 5 },  { "TM", 5 },  { "TJ", 5 },  { "KG", 6 },
            { "KZ", 6 },  { "AZ", 4 },
            { "TR", 3 },
            { "GB", 0 },  { "FR", 1 },  { "DE", 1 },  { "BE", 1 },
            { "NL", 1 },  { "ES", 1 },  { "IT", 1 },  { "SE", 1 },
            { "NO", 1 },  { "DK", 1 },  { "AT", 1 },  { "CH", 1 },
            { "US", -5 }, { "CA", -5 },
        };

        /// <summary>
        /// Returns the DST-aware UTC offset (hours) for a given location and date.
        /// Primary: resolves via the device's IANA tzdata using TimeZoneInfo,
        ///   which correctly handles rules such as Morocco reverting to UTC+0 during Ramadan,
        ///   or European countries switching between standard/summer time.
        /// Fallback: hardcoded standard offsets when the IANA ID is unknown or unavailable.
        /// </summary>
        public static double GetCityUtcOffset(string countryCode, double longitude, DateTime? date = null)
        {
            var forDate = date ?? DateTime.Today;

            // 1. Try IANA timezone lookup (DST-aware)
            if (!string.IsNullOrEmpty(countryCode) &&
                CountryIanaTimezones.TryGetValue(countryCode, out string? ianaId))
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(ianaId);
                    return tz.GetUtcOffset(forDate).TotalHours;
                }
                catch (TimeZoneNotFoundException)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[PrayerTimeService] IANA timezone not found on device: {ianaId}, falling back");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[PrayerTimeService] Timezone lookup error for {ianaId}: {ex.Message}");
                }
            }

            // 2. Hardcoded fallback (no DST)
            if (!string.IsNullOrEmpty(countryCode) &&
                CountryUtcOffsetsFallback.TryGetValue(countryCode, out double offset))
            {
                return offset;
            }

            // 3. Longitude estimation (±30-minute precision)
            return Math.Round(longitude / 15.0 * 2.0) / 2.0;
        }

        /// <summary>
        /// Get a human-readable display name for a calculation method.
        /// </summary>
        public static string GetMethodDisplayName(CalculationMethod method) => method switch
        {
            CalculationMethod.Auto => "Auto",
            CalculationMethod.Morocco => "Morocco (AWQAF)",
            CalculationMethod.EgyptNew => "Egypt (Survey Authority)",
            CalculationMethod.Palestine => "Palestine",
            CalculationMethod.KarachiHanafi => "Karachi (Hanafi)",
            CalculationMethod.NorthAmerica => "North America (ISNA)",
            CalculationMethod.MuslimLeague => "Muslim World League",
            CalculationMethod.UmmAlQura => "Umm Al Qura",
            CalculationMethod.UmmAlQuraRamadan => "Umm Al Qura (Ramadan)",
            CalculationMethod.Uae => "UAE (GAIAE)",
            CalculationMethod.Uoif => "France (UOIF)",
            CalculationMethod.Algier => "Algeria (MARA)",
            CalculationMethod.Tunisia => "Tunisia",
            CalculationMethod.Kuwait => "Kuwait",
            CalculationMethod.Paris => "Paris (Grand Mosque)",
            CalculationMethod.Dyanet => "Turkey (Diyanet)",
            CalculationMethod.Igmg => "IGMG",
            CalculationMethod.BelgiumEmb => "Belgium (EMB)",
            _ => method.ToString()
        };
    }
}
