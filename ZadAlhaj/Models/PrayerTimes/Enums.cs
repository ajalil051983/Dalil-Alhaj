namespace ZadAlhaj.Models.PrayerTimes
{
    /// <summary>
    /// The 8 prayer time slots computed by the engine.
    /// </summary>
    public enum PrayerTimeType
    {
        Fajr = 0,
        Shurooq = 1,   // Sunrise
        Dhuhr = 2,
        Asr = 3,
        Maghrib = 4,   // Sunset
        Isha = 5,
        Imsaak = 6,
        NextFajr = 7
    }

    /// <summary>
    /// Juristic school for Asr calculation.
    /// </summary>
    public enum Mathhab
    {
        /// <summary>Shafi'i, Maliki, Hanbali — shadow = 1× object length</summary>
        Shafii = 1,
        /// <summary>Hanafi — shadow = 2× object length</summary>
        Hanafi = 2
    }

    /// <summary>
    /// Calculation method presets.
    /// Each defines Fajr/Isha angles, offsets, and high-latitude handling.
    /// </summary>
    public enum CalculationMethod
    {
        Auto = 0,
        Morocco,
        EgyptNew,
        Palestine,
        KarachiHanafi,
        NorthAmerica,
        MuslimLeague,
        UmmAlQura,
        UmmAlQuraRamadan,
        Uae,
        Uoif,
        Algier,
        Tunisia,
        Kuwait,
        Paris,
        Dyanet,
        Igmg,
        BelgiumEmb
    }
}
