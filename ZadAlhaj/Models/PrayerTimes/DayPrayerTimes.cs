namespace ZadAlhaj.Models.PrayerTimes
{
    /// <summary>
    /// Holds the computed prayer times for a single day.
    /// </summary>
    public class DayPrayerTimes
    {
        public DateTime Date { get; set; }
        public string LocationName { get; set; } = "";
        public string CountryCode { get; set; } = "";
        public CalculationMethod Method { get; set; }

        /// <summary>
        /// The UTC offset (in hours) of the city for which these prayer times were calculated.
        /// Used to compute the city's local "now" for countdown purposes.
        /// </summary>
        public double UtcOffsetHours { get; set; }

        public TimeSpan Fajr { get; set; }
        public TimeSpan Shurooq { get; set; }
        public TimeSpan Dhuhr { get; set; }
        public TimeSpan Asr { get; set; }
        public TimeSpan Maghrib { get; set; }
        public TimeSpan Isha { get; set; }
        public TimeSpan Imsaak { get; set; }
        public TimeSpan NextFajr { get; set; }

        /// <summary>
        /// Returns the TimeSpan for a given prayer type.
        /// </summary>
        public TimeSpan GetTime(PrayerTimeType type) => type switch
        {
            PrayerTimeType.Fajr => Fajr,
            PrayerTimeType.Shurooq => Shurooq,
            PrayerTimeType.Dhuhr => Dhuhr,
            PrayerTimeType.Asr => Asr,
            PrayerTimeType.Maghrib => Maghrib,
            PrayerTimeType.Isha => Isha,
            PrayerTimeType.Imsaak => Imsaak,
            PrayerTimeType.NextFajr => NextFajr,
            _ => TimeSpan.Zero
        };

        /// <summary>
        /// Gets the next prayer after the current time.
        /// Returns the prayer type and its time.
        /// </summary>
        public (PrayerTimeType Type, TimeSpan Time)? GetNextPrayer(TimeSpan currentTime)
        {
            return GetNextPrayer(currentTime, TimeSpan.Zero);
        }

        /// <summary>
        /// Gets the next prayer after the current time, with a grace period.
        /// During the grace period after a prayer time, that prayer is still considered "next".
        /// </summary>
        public (PrayerTimeType Type, TimeSpan Time)? GetNextPrayer(TimeSpan currentTime, TimeSpan gracePeriod)
        {
            var prayers = new[]
            {
                (PrayerTimeType.Fajr, Fajr),
                (PrayerTimeType.Shurooq, Shurooq),
                (PrayerTimeType.Dhuhr, Dhuhr),
                (PrayerTimeType.Asr, Asr),
                (PrayerTimeType.Maghrib, Maghrib),
                (PrayerTimeType.Isha, Isha),
            };

            foreach (var (type, time) in prayers)
            {
                if (time + gracePeriod > currentTime)
                    return (type, time);
            }

            // All prayers passed — next is NextFajr (tomorrow)
            return (PrayerTimeType.NextFajr, NextFajr);
        }
    }
}
