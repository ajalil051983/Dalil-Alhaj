using System.Globalization;
using System.Text.Json;

namespace ZadAlhaj.Services.PrayerTimes
{
    /// <summary>
    /// Provides Hijri (Islamic) date conversion.
    /// Uses the Aladhan API (<see href="https://aladhan.com/islamic-calendar-api"/>)
    /// as the primary source and falls back to .NET's UmAlQuraCalendar offline.
    /// 
    /// A user-configurable adjustment (−2 … +2 days) is applied so the date can
    /// match local moon-sighting authorities (e.g. Morocco's Ministry of Habous).
    /// </summary>
    public class HijriDateService
    {
        private const string AdjustmentKey = "hijri_date_adjustment";

        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static readonly string[] HijriMonthsAr =
        {
            "",
            "محرم", "صفر", "ربيع الأول", "ربيع الثاني",
            "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان",
            "رمضان", "شوال", "ذو القعدة", "ذو الحجة"
        };

        private static readonly string[] HijriMonthsEn =
        {
            "",
            "Muharram", "Safar", "Rabi' al-Awwal", "Rabi' al-Thani",
            "Jumada al-Ula", "Jumada al-Akhirah", "Rajab", "Sha'ban",
            "Ramadan", "Shawwal", "Dhul-Qi'dah", "Dhul-Hijjah"
        };

        private static readonly string[] HijriMonthsFr =
        {
            "",
            "Mouharram", "Safar", "Rabia al-Awal", "Rabia ath-Thani",
            "Joumada al-Oula", "Joumada ath-Thania", "Rajab", "Chaabane",
            "Ramadan", "Chawwal", "Dhou al-Qi'da", "Dhou al-Hijja"
        };

        // ───────── Preference accessors ─────────

        /// <summary>
        /// Gets the user's Hijri date adjustment (−2 … +2 days).
        /// Default is 0 (no adjustment).
        /// </summary>
        public static int GetAdjustment()
            => Preferences.Get(AdjustmentKey, 0);

        /// <summary>
        /// Saves the Hijri date adjustment.
        /// </summary>
        public static void SetAdjustment(int adjustment)
            => Preferences.Set(AdjustmentKey, Math.Clamp(adjustment, -2, 2));

        // ───────── Public API ─────────

        /// <summary>
        /// Returns a formatted Hijri date string for the given Gregorian date
        /// (e.g. "27 رمضان 1447").
        /// Tries the Aladhan API first, then falls back to the local calendar.
        /// </summary>
        public async Task<HijriDate?> GetHijriDateAsync(DateTime gregorianDate)
        {
            int adjustment = GetAdjustment();

            // 1. Try online API
            var result = await TryAladhanApiAsync(gregorianDate, adjustment);
            if (result != null) return result;

            // 2. Fallback to local UmAlQuraCalendar + adjustment
            return GetLocalHijriDate(gregorianDate, adjustment);
        }

        /// <summary>
        /// Synchronous fallback — used when an async call is impractical.
        /// Uses only the local UmAlQuraCalendar.
        /// </summary>
        public static HijriDate GetLocalHijriDate(DateTime gregorianDate, int? adjustment = null)
        {
            int adj = adjustment ?? GetAdjustment();
            var adjusted = gregorianDate.AddDays(adj);

            var cal = new UmAlQuraCalendar();
            return new HijriDate
            {
                Day = cal.GetDayOfMonth(adjusted),
                Month = cal.GetMonth(adjusted),
                Year = cal.GetYear(adjusted)
            };
        }

        // ───────── Aladhan API ─────────

        /// <summary>
        /// Calls GET http://api.aladhan.com/v1/gToH/{dd-MM-yyyy}?adjustment={n}.
        /// Returns null on any failure (network, parse, etc.).
        /// </summary>
        private static async Task<HijriDate?> TryAladhanApiAsync(DateTime date, int adjustment)
        {
            try
            {
                string dateStr = date.ToString("dd-MM-yyyy");
                string url = $"http://api.aladhan.com/v1/gToH/{dateStr}?adjustment={adjustment}";

                using var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var hijri = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("hijri");

                int day = int.Parse(hijri.GetProperty("day").GetString() ?? "0");
                int month = int.Parse(hijri.GetProperty("month").GetProperty("number").GetRawText());
                int year = int.Parse(hijri.GetProperty("year").GetString() ?? "0");

                if (day == 0 || year == 0) return null;

                return new HijriDate { Day = day, Month = month, Year = year };
            }
            catch
            {
                return null;
            }
        }

        // ───────── Formatting helpers ─────────

        /// <summary>
        /// Format a HijriDate as a localized string (e.g. "27 رمضان 1447").
        /// </summary>
        public static string Format(HijriDate h, string? languageCode = null)
        {
            var lang = languageCode ?? LocalizationService.GetCurrentLanguage();
            var months = lang switch
            {
                "fr" => HijriMonthsFr,
                "en" => HijriMonthsEn,
                _ => HijriMonthsAr
            };

            int idx = Math.Clamp(h.Month, 1, 12);
            return $"{h.Day} {months[idx]} {h.Year}";
        }
    }

    /// <summary>
    /// Simple value object for a Hijri date.
    /// </summary>
    public class HijriDate
    {
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
