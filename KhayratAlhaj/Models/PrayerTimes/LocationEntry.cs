using SQLite;

namespace KhayratAlhaj.Models.PrayerTimes
{
    /// <summary>
    /// Maps to the LocationEntity table in embedded_data.db.
    /// Coordinates are stored as actual_value × 100 in the DB.
    /// </summary>
    [Table("LocationEntity")]
    public class LocationEntry
    {
        [PrimaryKey]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = "";

        [Column("countryCode")]
        public string CountryCode { get; set; } = "";

        /// <summary>
        /// Raw latitude from DB (actual_value × 100).
        /// Use <see cref="LatitudeActual"/> for the real value.
        /// </summary>
        [Column("latitude")]
        public int Latitude { get; set; }

        /// <summary>
        /// Raw longitude from DB (actual_value × 100).
        /// Use <see cref="LongitudeActual"/> for the real value.
        /// </summary>
        [Column("longitude")]
        public int Longitude { get; set; }

        [Column("altitude")]
        public int Altitude { get; set; }

        /// <summary>Actual latitude in degrees.</summary>
        [Ignore]
        public double LatitudeActual => Latitude / 100.0;

        /// <summary>Actual longitude in degrees.</summary>
        [Ignore]
        public double LongitudeActual => Longitude / 100.0;
    }
}
