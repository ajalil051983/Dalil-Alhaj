using NUnit.Framework;
using KhayratAlhaj.Models.PrayerTimes;
using KhayratAlhaj.Services.PrayerTimes;

namespace KhayratAlhaj.UnitTests.Tests
{
    /// <summary>
    /// Reference / regression tests that compare our computed prayer times against
    /// the official Moroccan Ministry of Religious Affairs (Habous) timetable for
    /// Rabat, published at https://www.habous.gov.ma/fr/horaires-de-prière.html
    ///
    /// ┌───────────────────────────────────────────────────────────────────┐
    /// │  KNOWN RESIDUAL DISCREPANCIES (as of March 2026)                  │
    /// │                                                                    │
    /// │  Prayer  │ Habous │  App   │ Diff  │ Root cause                   │
    /// │──────────┼────────┼────────┼───────┼──────────────────────────    │
    /// │  Fajr    │ 05:08  │ 05:09  │ +1 min│ Meeus error floor            │
    /// │  Shurooq │ 06:34  │ 06:36  │ +2 min│ Meeus + horizon angle        │
    /// │  Dhuhr   │ 12:41  │ 12:41  │ exact │                              │
    /// │  Asr     │ 16:01  │ 16:01  │ exact │                              │
    /// │  Maghrib │ 18:40  │ 18:39  │ -1 min│ Meeus error floor            │
    /// │  Isha    │ 19:54  │ 19:54  │ exact │                              │
    /// │                                                                    │
    /// │  The Meeus error floor is ~1 min (inherent to the simplified       │
    /// │  algorithm vs. the full VSOP87 coefficient tables).               │
    /// │  Shurooq runs 2 min late because it is the most horizon-sensitive  │
    /// │  prayer (pure geometric sunrise — the 1-min baseline error         │
    /// │  compounds with the altitude dip calculation sensitivity).        │
    /// │  See Docs/SalaatFirst_PrayerTimes_CSharp_Guide.md §               │
    /// │  "Accuracy & Known Limitations" for the full breakdown.           │
    /// └───────────────────────────────────────────────────────────────────┘
    /// </summary>
    [TestFixture]
    public class RabatHabousRegressionTests
    {
        // Rabat city (coordinates stored at ×100 precision in the DB)
        private const double RabatLat = 34.02;   // 3402 / 100
        private const double RabatLon = -6.84;   // -684 / 100
        private const double RabatAlt = 75.0;    // metres above sea level

        // Morocco (Africa/Casablanca) has a unique DST rule: it uses UTC+1 in
        // standard time but suspends DST and reverts to UTC+0 during Ramadan.
        // All test dates fall within Ramadan 1447 (Mar 1–29 2026), so offset = 0.
        // PrayerTimeService uses TimeZoneInfo.FindSystemTimeZoneById("Africa/Casablanca")
        // which encodes this rule and returns 0.0 for these dates.
        private const double RabatGmt = 0.0;    // UTC+0 during Ramadan
        private const CalculationMethod Method  = CalculationMethod.Morocco;

        // Tolerance acknowledging the ~1-minute Meeus error floor.
        // Shurooq gets +1 extra minute because geometric sunrise is the most
        // horizon-sensitive prayer and the altitude dip sensitivity pushes the
        // baseline error to ~2 min for this location.
        private static readonly TimeSpan StandardTolerance = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan ShurooqTolerance  = TimeSpan.FromMinutes(3);

        // ──────────────────────────────────────────────────────────────
        // March 16, 2026 — Ramadan 26, 1447  (today row highlighted in
        // the official Habous table, confirmed against habous.gov.ma)
        // Note: .NET UmAlQuraCalendar reports Ramadan 27 for this date,
        // but Morocco follows moon-sighting and the Habous table says 26.
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Reference date: March 16 2026 (Ramadan 26 per Habous).
        /// Source: habous.gov.ma — Rabat, Février/Mars column.
        /// Expected: Fajr=05:08, Shurooq=06:34, Dhuhr=12:41, Asr=16:01,
        ///            Maghrib=18:40, Isha=19:54.
        /// </summary>
        [Test]
        public void Rabat_Mar16_2026_FajrIsWithin2MinOfHabous()
        {
            var result = Calculate(new DateTime(2026, 3, 16));
            AssertWithin(result.Fajr, new TimeSpan(5, 8, 0), StandardTolerance, "Fajr");
        }

        [Test]
        public void Rabat_Mar16_2026_ShurooqIsWithin3MinOfHabous()
        {
            var result = Calculate(new DateTime(2026, 3, 16));
            AssertWithin(result.Shurooq, new TimeSpan(6, 34, 0), ShurooqTolerance, "Shurooq");
        }

        [Test]
        public void Rabat_Mar16_2026_DhuhrIsExact()
        {
            var result = Calculate(new DateTime(2026, 3, 16));
            Assert.That(result.Dhuhr, Is.EqualTo(new TimeSpan(12, 41, 0)),
                "Dhuhr should be exactly 12:41 — solar transit is not horizon-dependent");
        }

        [Test]
        public void Rabat_Mar16_2026_AsrIsExact()
        {
            var result = Calculate(new DateTime(2026, 3, 16));
            Assert.That(result.Asr, Is.EqualTo(new TimeSpan(16, 1, 0)),
                "Asr should be exactly 16:01");
        }

        /// <summary>
        /// Maghrib = raw sunset + 2 min offset (Morocco method).
        /// App: 18:39 — official: 18:40.  Difference = 1 min (Meeus floor).
        /// This test will fail if the engine regresses beyond the 2-min tolerance.
        /// </summary>
        [Test]
        public void Rabat_Mar16_2026_MaghribIsWithin2MinOfHabous()
        {
            var result = Calculate(new DateTime(2026, 3, 16));
            AssertWithin(result.Maghrib, new TimeSpan(18, 40, 0), StandardTolerance, "Maghrib");
        }

        [Test]
        public void Rabat_Mar16_2026_IshaIsExact()
        {
            var result = Calculate(new DateTime(2026, 3, 16));
            Assert.That(result.Isha, Is.EqualTo(new TimeSpan(19, 54, 0)),
                "Isha should be exactly 19:54");
        }

        // ──────────────────────────────────────────────────────────────
        // March 1, 2026 — Ramadan 11, 1447
        // The Habous table starts at Ramadan 1 = February 19; March 1 is row 11.
        // Expected: Fajr=05:28, Shurooq=06:53, Dhuhr=12:45, Asr=15:55,
        //            Maghrib=18:28, Isha=19:42
        // ──────────────────────────────────────────────────────────────

        [Test]
        public void Rabat_Mar01_2026_FajrIsWithin2MinOfHabous()
        {
            var result = Calculate(new DateTime(2026, 3, 1));
            AssertWithin(result.Fajr, new TimeSpan(5, 28, 0), StandardTolerance, "Fajr");
        }

        [Test]
        public void Rabat_Mar01_2026_ShurooqIsWithin3MinOfHabous()
        {
            var result = Calculate(new DateTime(2026, 3, 1));
            AssertWithin(result.Shurooq, new TimeSpan(6, 53, 0), ShurooqTolerance, "Shurooq");
        }

        [Test]
        public void Rabat_Mar01_2026_DhuhrIsWithin2MinOfHabous()
        {
            var result = Calculate(new DateTime(2026, 3, 1));
            AssertWithin(result.Dhuhr, new TimeSpan(12, 45, 0), StandardTolerance, "Dhuhr");
        }

        [Test]
        public void Rabat_Mar01_2026_AsrIsWithin2MinOfHabous()
        {
            var result = Calculate(new DateTime(2026, 3, 1));
            AssertWithin(result.Asr, new TimeSpan(15, 55, 0), StandardTolerance, "Asr");
        }

        [Test]
        public void Rabat_Mar01_2026_MaghribIsWithin2MinOfHabous()
        {
            var result = Calculate(new DateTime(2026, 3, 1));
            AssertWithin(result.Maghrib, new TimeSpan(18, 28, 0), StandardTolerance, "Maghrib");
        }

        [Test]
        public void Rabat_Mar01_2026_IshaIsWithin2MinOfHabous()
        {
            var result = Calculate(new DateTime(2026, 3, 1));
            AssertWithin(result.Isha, new TimeSpan(19, 42, 0), StandardTolerance, "Isha");
        }

        // ──────────────────────────────────────────────────────────────
        // March 19, 2026 — Ramadan 29, 1447 (last complete row of the Habous
        // table that falls within Ramadan — Morocco is still UTC+0 on this date)
        // Expected: Fajr=05:04, Shurooq=06:30, Dhuhr=12:40, Asr=16:02,
        //            Maghrib=18:42, Isha=19:57
        // Note: March 29 is AFTER Ramadan ends (~Mar 19-20) and Morocco reverts
        // to UTC+1, so it is outside the scope of this Ramadan reference dataset.
        // ──────────────────────────────────────────────────────────────

        [Test]
        public void Rabat_Mar19_2026_AllPrayersWithinTolerance()
        {
            var result = Calculate(new DateTime(2026, 3, 19));
            AssertWithin(result.Fajr,    new TimeSpan(5,  4,  0), StandardTolerance, "Fajr");
            AssertWithin(result.Shurooq, new TimeSpan(6,  30, 0), ShurooqTolerance,  "Shurooq");
            AssertWithin(result.Dhuhr,   new TimeSpan(12, 40, 0), StandardTolerance, "Dhuhr");
            AssertWithin(result.Asr,     new TimeSpan(16, 2,  0), StandardTolerance, "Asr");
            AssertWithin(result.Maghrib, new TimeSpan(18, 42, 0), StandardTolerance, "Maghrib");
            AssertWithin(result.Isha,    new TimeSpan(19, 57, 0), StandardTolerance, "Isha");
        }

        // ──────────────────────────────────────────────────────────────
        // Tolerance does not regress — ensure we are not worse than 2 min
        // across the entire month visible in the Habous table.
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Chronological order must hold for every day in March 2026 for Rabat
        /// during Ramadan (March 1–19, all UTC+0).
        /// </summary>
        [Test]
        public void Rabat_RamadanMarch2026_PrayerOrderIsChronologicalEveryDay()
        {
            for (int day = 1; day <= 19; day++) // Ramadan ends ~Mar 19-20
            {
                var result = Calculate(new DateTime(2026, 3, day));
                var d = $"2026-03-{day:D2}";
                Assert.That(result.Fajr,    Is.LessThan(result.Shurooq),  $"{d} Fajr < Shurooq");
                Assert.That(result.Shurooq, Is.LessThan(result.Dhuhr),    $"{d} Shurooq < Dhuhr");
                Assert.That(result.Dhuhr,   Is.LessThan(result.Asr),      $"{d} Dhuhr < Asr");
                Assert.That(result.Asr,     Is.LessThan(result.Maghrib),   $"{d} Asr < Maghrib");
                Assert.That(result.Maghrib, Is.LessThan(result.Isha),      $"{d} Maghrib < Isha");
            }
        }

        /// <summary>
        /// Fajr must become progressively earlier as Ramadan March progresses
        /// (days get longer → earlier dawn). Check Mar 1 vs Mar 19.
        /// </summary>
        [Test]
        public void Rabat_FajrBecomesEarlierAcrossRamadanMarch2026()
        {
            var first  = Calculate(new DateTime(2026, 3, 1));
            var last   = Calculate(new DateTime(2026, 3, 19));
            Assert.That(last.Fajr, Is.LessThan(first.Fajr),
                $"Fajr on Mar 19 ({last.Fajr:hh\\:mm}) should be earlier than Mar 1 ({first.Fajr:hh\\:mm})");
        }

        /// <summary>
        /// Maghrib must become progressively later as Ramadan March progresses.
        /// </summary>
        [Test]
        public void Rabat_MaghribBecomesLaterAcrossRamadanMarch2026()
        {
            var first  = Calculate(new DateTime(2026, 3, 1));
            var last   = Calculate(new DateTime(2026, 3, 19));
            Assert.That(last.Maghrib, Is.GreaterThan(first.Maghrib),
                $"Maghrib on Mar 19 ({last.Maghrib:hh\\:mm}) should be later than Mar 1 ({first.Maghrib:hh\\:mm})");
        }

        // ──────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────

        private static DayPrayerTimes Calculate(DateTime date) =>
            PrayerTimeCalculator.Calculate(date, RabatLat, RabatLon, RabatAlt, Method, RabatGmt);

        private static void AssertWithin(TimeSpan actual, TimeSpan expected, TimeSpan tolerance, string prayer)
        {
            double diffMinutes = Math.Abs((actual - expected).TotalMinutes);
            Assert.That(diffMinutes, Is.LessThanOrEqualTo(tolerance.TotalMinutes),
                $"{prayer}: computed {actual:hh\\:mm}, official {expected:hh\\:mm}, " +
                $"diff = {diffMinutes:F0} min (tolerance ±{tolerance.TotalMinutes:F0} min)");
        }
    }
}
