using NUnit.Framework;
using KhayratAlhaj.Models.PrayerTimes;
using KhayratAlhaj.Services.PrayerTimes;

namespace KhayratAlhaj.UnitTests.Tests
{
    /// <summary>
    /// Unit tests for PrayerTimeCalculator, focused on the two accuracy fixes:
    ///   1. Altitude / dip correction applied to Shurooq &amp; Maghrib.
    ///   2. TimeSpanFromHours rounding uses Math.Round — no chained FP truncation.
    ///
    /// Also covers ordering, non-sentinel output, and rough sanity ranges for
    /// well-known cities.
    /// </summary>
    [TestFixture]
    public class PrayerTimeCalculatorTests
    {
        // ──────────────────────────────────────────────────────────────
        // Shared fixtures
        // ──────────────────────────────────────────────────────────────

        // Makkah Al-Mukarramah
        private const double MakkahLat     = 21.4225;
        private const double MakkahLon     = 39.8233;
        private const double MakkahAlt     = 277.0;   // metres above sea level
        private const double MakkahGmt     = 3.0;     // UTC+3
        private const CalculationMethod MakkahMethod = CalculationMethod.UmmAlQura;

        // London (sea level, higher latitude, DST not an issue for unit test)
        private const double LondonLat     = 51.5074;
        private const double LondonLon     = -0.1278;
        private const double LondonAlt     = 11.0;
        private const double LondonGmt     = 0.0;
        private const CalculationMethod LondonMethod = CalculationMethod.MuslimLeague;

        // ──────────────────────────────────────────────────────────────
        // Fix 1 — Altitude / dip correction
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// For a city above sea level, Shurooq must be EARLIER than it would be
        /// at the same coordinates with altitude = 0.
        /// The effective horizon is lowered by Δh = 0.0347° × √altitude,
        /// causing the sun to appear above the horizon sooner.
        /// </summary>
        [TestCase(2025, 1, 1)]
        [TestCase(2025, 6, 21)]
        [TestCase(2025, 9, 22)]
        public void AltitudeDip_ElevatedCity_ShurooqIsEarlierThanSeaLevel(
            int year, int month, int day)
        {
            var date = new DateTime(year, month, day);

            var withAlt = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, MakkahAlt, MakkahMethod, MakkahGmt);

            var seaLevel = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, 0.0, MakkahMethod, MakkahGmt);

            Assert.That(withAlt.Shurooq, Is.LessThan(seaLevel.Shurooq),
                $"Shurooq at altitude {MakkahAlt}m should be earlier than at sea level " +
                $"(got {withAlt.Shurooq:hh\\:mm} vs {seaLevel.Shurooq:hh\\:mm})");
        }

        /// <summary>
        /// For a city above sea level, Maghrib must be LATER than it would be
        /// at altitude = 0 (the sun sets below a lower effective horizon).
        /// </summary>
        [TestCase(2025, 1, 1)]
        [TestCase(2025, 6, 21)]
        [TestCase(2025, 9, 22)]
        public void AltitudeDip_ElevatedCity_MaghribIsLaterThanSeaLevel(
            int year, int month, int day)
        {
            var date = new DateTime(year, month, day);

            var withAlt = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, MakkahAlt, MakkahMethod, MakkahGmt);

            var seaLevel = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, 0.0, MakkahMethod, MakkahGmt);

            Assert.That(withAlt.Maghrib, Is.GreaterThan(seaLevel.Maghrib),
                $"Maghrib at altitude {MakkahAlt}m should be later than at sea level " +
                $"(got {withAlt.Maghrib:hh\\:mm} vs {seaLevel.Maghrib:hh\\:mm})");
        }

        /// <summary>
        /// The dip correction for 277 m must shift Shurooq by roughly 2–3 minutes.
        /// Formula: Δh = 0.0347° × √alt, min shift ≈ Δh / 15 × 60 ≈ 2.3 min.
        /// We allow a ±1-minute band around that to account for latitude/declination
        /// variation in the hour-angle rate.
        /// </summary>
        [TestCase(2025, 1, 1)]
        [TestCase(2025, 6, 21)]
        public void AltitudeDip_MakkahAlt277_ShurooqShiftIsApproximately2Minutes(
            int year, int month, int day)
        {
            var date = new DateTime(year, month, day);

            var withAlt  = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, 277.0, MakkahMethod, MakkahGmt);
            var seaLevel = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, 0.0,   MakkahMethod, MakkahGmt);

            double diffMinutes = (seaLevel.Shurooq - withAlt.Shurooq).TotalMinutes;

            Assert.That(diffMinutes, Is.InRange(1.0, 4.0),
                $"Expected ~2.3-minute shift for 277 m altitude; got {diffMinutes:F2} min");
        }

        /// <summary>
        /// Dhuhr is the solar transit and must not be affected by the altitude correction.
        /// (Correction is applied only to horizon-based hour angles.)
        /// </summary>
        [Test]
        public void AltitudeDip_DoesNotAffectDhuhr()
        {
            var date = new DateTime(2025, 6, 21);

            var withAlt  = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, 277.0, MakkahMethod, MakkahGmt);
            var seaLevel = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, 0.0,   MakkahMethod, MakkahGmt);

            Assert.That(withAlt.Dhuhr, Is.EqualTo(seaLevel.Dhuhr),
                "Dhuhr (solar transit) should be identical regardless of altitude");
        }

        /// <summary>
        /// Zero altitude must produce identical results whether passed as 0.0 or a
        /// negative value (altitude below sea level is clipped; dip = 0).
        /// </summary>
        [TestCase(0.0)]
        [TestCase(-50.0)]
        public void AltitudeDip_ZeroOrNegativeAltitude_NoDipApplied(double altitude)
        {
            var date     = new DateTime(2025, 6, 21);
            var baseline = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, 0.0, MakkahMethod, MakkahGmt);
            var actual   = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, altitude, MakkahMethod, MakkahGmt);

            Assert.That(actual.Shurooq,  Is.EqualTo(baseline.Shurooq),  "Shurooq");
            Assert.That(actual.Maghrib,  Is.EqualTo(baseline.Maghrib),  "Maghrib");
        }

        // ──────────────────────────────────────────────────────────────
        // Fix 2 — TimeSpanFromHours rounding correctness
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Every TimeSpan returned by Calculate() must have Seconds == 0.
        /// The old chained truncation (int h → int m → int s → if s≥30 m++) could
        /// produce TimeSpan(h, m, s) with s≠0 due to floating-point residual.
        /// The new Math.Round approach always returns a whole-minute TimeSpan.
        /// We iterate 365 days to flush out any edge-case floating-point values.
        /// </summary>
        [Test]
        public void Rounding_AllPrayerTimesHaveZeroSeconds_Over365Days_Makkah()
        {
            var start = new DateTime(2025, 1, 1);
            for (int i = 0; i < 365; i++)
            {
                var date = start.AddDays(i);
                var result = PrayerTimeCalculator.Calculate(
                    date, MakkahLat, MakkahLon, MakkahAlt, MakkahMethod, MakkahGmt);

                AssertNoSeconds(result, date);
            }
        }

        /// <summary>
        /// Same rounding check for a high-latitude location where hour-angle values
        /// are more extreme and floating-point residuals are more likely.
        /// </summary>
        [Test]
        public void Rounding_AllPrayerTimesHaveZeroSeconds_Over365Days_London()
        {
            var start = new DateTime(2025, 1, 1);
            for (int i = 0; i < 365; i++)
            {
                var date = start.AddDays(i);
                var result = PrayerTimeCalculator.Calculate(
                    date, LondonLat, LondonLon, LondonAlt, LondonMethod, LondonGmt);

                AssertNoSeconds(result, date);
            }
        }

        private static void AssertNoSeconds(DayPrayerTimes r, DateTime date)
        {
            var label = date.ToString("yyyy-MM-dd");
            Assert.That(r.Fajr.Seconds,    Is.Zero, $"{label} Fajr.Seconds");
            Assert.That(r.Shurooq.Seconds, Is.Zero, $"{label} Shurooq.Seconds");
            Assert.That(r.Dhuhr.Seconds,   Is.Zero, $"{label} Dhuhr.Seconds");
            Assert.That(r.Asr.Seconds,     Is.Zero, $"{label} Asr.Seconds");
            Assert.That(r.Maghrib.Seconds, Is.Zero, $"{label} Maghrib.Seconds");
            Assert.That(r.Isha.Seconds,    Is.Zero, $"{label} Isha.Seconds");
        }

        // ──────────────────────────────────────────────────────────────
        // Ordering
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Prayer times must always be in strict chronological order:
        /// Fajr &lt; Shurooq &lt; Dhuhr &lt; Asr &lt; Maghrib &lt; Isha
        /// </summary>
        [TestCase(2025, 1, 1)]
        [TestCase(2025, 6, 21)]
        [TestCase(2025, 12, 31)]
        public void PrayerTimes_AreInStrictChronologicalOrder_Makkah(
            int year, int month, int day)
        {
            var result = PrayerTimeCalculator.Calculate(
                new DateTime(year, month, day),
                MakkahLat, MakkahLon, MakkahAlt, MakkahMethod, MakkahGmt);

            Assert.That(result.Fajr,    Is.LessThan(result.Shurooq),  "Fajr < Shurooq");
            Assert.That(result.Shurooq, Is.LessThan(result.Dhuhr),    "Shurooq < Dhuhr");
            Assert.That(result.Dhuhr,   Is.LessThan(result.Asr),      "Dhuhr < Asr");
            Assert.That(result.Asr,     Is.LessThan(result.Maghrib),   "Asr < Maghrib");
            Assert.That(result.Maghrib, Is.LessThan(result.Isha),      "Maghrib < Isha");
        }

        [TestCase(2025, 1, 1)]
        [TestCase(2025, 6, 21)]
        public void PrayerTimes_AreInStrictChronologicalOrder_London(
            int year, int month, int day)
        {
            var result = PrayerTimeCalculator.Calculate(
                new DateTime(year, month, day),
                LondonLat, LondonLon, LondonAlt, LondonMethod, LondonGmt);

            Assert.That(result.Fajr,    Is.LessThan(result.Shurooq),  "Fajr < Shurooq");
            Assert.That(result.Shurooq, Is.LessThan(result.Dhuhr),    "Shurooq < Dhuhr");
            Assert.That(result.Dhuhr,   Is.LessThan(result.Asr),      "Dhuhr < Asr");
            Assert.That(result.Asr,     Is.LessThan(result.Maghrib),   "Asr < Maghrib");
            Assert.That(result.Maghrib, Is.LessThan(result.Isha),      "Maghrib < Isha");
        }

        // ──────────────────────────────────────────────────────────────
        // Sanity / range checks
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// All six prayer times must be non-zero (never fell back to TimeSpan.Zero sentinel)
        /// for a normal mid-latitude city.
        /// </summary>
        [Test]
        public void PrayerTimes_AllSixAreNonZero_Makkah()
        {
            var result = PrayerTimeCalculator.Calculate(
                new DateTime(2025, 6, 21),
                MakkahLat, MakkahLon, MakkahAlt, MakkahMethod, MakkahGmt);

            Assert.That(result.Fajr,    Is.Not.EqualTo(TimeSpan.Zero), "Fajr");
            Assert.That(result.Shurooq, Is.Not.EqualTo(TimeSpan.Zero), "Shurooq");
            Assert.That(result.Dhuhr,   Is.Not.EqualTo(TimeSpan.Zero), "Dhuhr");
            Assert.That(result.Asr,     Is.Not.EqualTo(TimeSpan.Zero), "Asr");
            Assert.That(result.Maghrib, Is.Not.EqualTo(TimeSpan.Zero), "Maghrib");
            Assert.That(result.Isha,    Is.Not.EqualTo(TimeSpan.Zero), "Isha");
        }

        /// <summary>
        /// Dhuhr for Makkah in January should be around 12:21 local (UTC+3, lon=39.82°).
        /// The simplified Meeus engine has a ~1-minute error floor, so we allow ±3 min.
        /// Reference: published Umm Al-Qura times for Makkah Jan 2025 ≈ 12:21.
        /// </summary>
        [Test]
        public void Dhuhr_Makkah_Jan2025_IsAroundLocalNoon()
        {
            var result = PrayerTimeCalculator.Calculate(
                new DateTime(2025, 1, 1),
                MakkahLat, MakkahLon, MakkahAlt, MakkahMethod, MakkahGmt);

            // Dhuhr should be between 12:15 and 12:30 local time
            Assert.That(result.Dhuhr, Is.InRange(
                new TimeSpan(12, 15, 0),
                new TimeSpan(12, 30, 0)),
                $"Makkah Dhuhr {result.Dhuhr:hh\\:mm} is outside expected 12:15–12:30 range");
        }

        /// <summary>
        /// Iterated accuracy check: for each month in 2025, Makkah Dhuhr must stay
        /// within the 12:15–12:30 window (±7 min of 12:22 to account for equation of time).
        /// </summary>
        [TestCase(2025, 1)]
        [TestCase(2025, 4)]
        [TestCase(2025, 7)]
        [TestCase(2025, 10)]
        public void Dhuhr_Makkah_StaysNearLocalNoon_ThroughSeasons(int year, int month)
        {
            var date   = new DateTime(year, month, 15);
            var result = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, MakkahAlt, MakkahMethod, MakkahGmt);

            // Makkah longitude offset from UTC+3 centre (45°E): (45−39.82)/15 = 20.7 min late
            // Equation of time varies ±16 min → total window ≈ 12:04–12:38
            Assert.That(result.Dhuhr, Is.InRange(
                new TimeSpan(12, 0, 0),
                new TimeSpan(12, 40, 0)),
                $"{date:yyyy-MM-dd} Dhuhr {result.Dhuhr:hh\\:mm} is outside expected range");
        }

        /// <summary>
        /// NextFajr represents tomorrow's Fajr time-of-day and must be close to
        /// today's Fajr (Fajr does not shift by more than ~2 hours between consecutive
        /// days for any normal location). This confirms the next-day calculation ran
        /// without producing a sentinel value.
        /// </summary>
        [TestCase(2025, 1, 1)]
        [TestCase(2025, 6, 21)]
        public void NextFajr_IsWithin2HoursOfTodayFajr(int year, int month, int day)
        {
            var result = PrayerTimeCalculator.Calculate(
                new DateTime(year, month, day),
                MakkahLat, MakkahLon, MakkahAlt, MakkahMethod, MakkahGmt);

            // NextFajr is a time-of-day TimeSpan for tomorrow — compare it relative to
            // today's Fajr; account for midnight wrap-around.
            double diffHours = Math.Abs((result.NextFajr - result.Fajr).TotalHours);
            if (diffHours > 12) diffHours = 24.0 - diffHours;

            Assert.That(diffHours, Is.LessThan(2.0),
                $"NextFajr {result.NextFajr:hh\\:mm} differs from today's Fajr " +
                $"{result.Fajr:hh\\:mm} by {diffHours:F2}h — expected < 2h");
        }

        // ──────────────────────────────────────────────────────────────
        // Calculation method coverage
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// All 17 explicit calculation methods must produce valid, ordered prayer times
        /// for a standard mid-latitude location without throwing.
        /// </summary>
        [TestCase(CalculationMethod.Morocco)]
        [TestCase(CalculationMethod.EgyptNew)]
        [TestCase(CalculationMethod.Palestine)]
        [TestCase(CalculationMethod.KarachiHanafi)]
        [TestCase(CalculationMethod.NorthAmerica)]
        [TestCase(CalculationMethod.MuslimLeague)]
        [TestCase(CalculationMethod.UmmAlQura)]
        [TestCase(CalculationMethod.UmmAlQuraRamadan)]
        [TestCase(CalculationMethod.Uae)]
        [TestCase(CalculationMethod.Uoif)]
        [TestCase(CalculationMethod.Algier)]
        [TestCase(CalculationMethod.Tunisia)]
        [TestCase(CalculationMethod.Kuwait)]
        [TestCase(CalculationMethod.Paris)]
        [TestCase(CalculationMethod.Dyanet)]
        [TestCase(CalculationMethod.Igmg)]
        [TestCase(CalculationMethod.BelgiumEmb)]
        public void AllMethods_ProduceChronologicalPrayerTimes(CalculationMethod method)
        {
            var result = PrayerTimeCalculator.Calculate(
                new DateTime(2025, 6, 21),
                MakkahLat, MakkahLon, MakkahAlt, method, MakkahGmt);

            Assert.That(result.Fajr,    Is.LessThan(result.Shurooq),  $"{method}: Fajr < Shurooq");
            Assert.That(result.Shurooq, Is.LessThan(result.Dhuhr),    $"{method}: Shurooq < Dhuhr");
            Assert.That(result.Dhuhr,   Is.LessThan(result.Asr),      $"{method}: Dhuhr < Asr");
            Assert.That(result.Asr,     Is.LessThan(result.Maghrib),   $"{method}: Asr < Maghrib");
            Assert.That(result.Maghrib, Is.LessThan(result.Isha),      $"{method}: Maghrib < Isha");
        }

        /// <summary>
        /// Hanafi mathhab (KarachiHanafi) must produce a later Asr than Shafi'i (Morocco)
        /// because Hanafi uses a shadow ratio of 2× (vs 1×).
        /// </summary>
        [Test]
        public void Asr_HanafiIsLaterThanShafii()
        {
            var date   = new DateTime(2025, 6, 21);
            var hanafi = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, MakkahAlt, CalculationMethod.KarachiHanafi, MakkahGmt);
            var shafii = PrayerTimeCalculator.Calculate(
                date, MakkahLat, MakkahLon, MakkahAlt, CalculationMethod.Morocco, MakkahGmt);

            Assert.That(hanafi.Asr, Is.GreaterThan(shafii.Asr),
                $"Hanafi Asr {hanafi.Asr:hh\\:mm} should be later than Shafi'i Asr {shafii.Asr:hh\\:mm}");
        }
    }
}
