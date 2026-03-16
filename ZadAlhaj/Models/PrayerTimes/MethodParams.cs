namespace ZadAlhaj.Models.PrayerTimes
{
    /// <summary>
    /// Defines a calculation method's parameters.
    /// All 17 presets exactly as defined in Salaat First v6.3.1.
    /// </summary>
    public class MethodParams
    {
        public double FajrAngle { get; set; }
        public double IshaAngle { get; set; }
        public double ImsaakAngle { get; set; } = 1.5;
        public int FajrInterval { get; set; }     // Minutes before sunrise (0 = use angle)
        public int IshaInterval { get; set; }     // Minutes after sunset (0 = use angle)
        public Mathhab Mathhab { get; set; } = Mathhab.Shafii;
        public double NearestLat { get; set; } = 48.5;
        public double ExtremeLat { get; set; } = 55.0;
        public double[] Offsets { get; set; } = new double[6]; // [Fajr, Shurooq, Dhuhr, Asr, Maghrib, Isha]

        public static readonly Dictionary<CalculationMethod, MethodParams> Presets = new()
        {
            [CalculationMethod.Morocco] = new MethodParams
            {
                FajrAngle = 19.0, IshaAngle = 17.0,
                Offsets = [0, 0, 5, 0, 2, 0]
            },
            [CalculationMethod.EgyptNew] = new MethodParams
            {
                FajrAngle = 19.5, IshaAngle = 17.5,
                Offsets = [0, 0, 0, 0, 0, 0]
            },
            [CalculationMethod.Palestine] = new MethodParams
            {
                FajrAngle = 20.0, IshaAngle = 18.0,
                Offsets = [0, 0, 0, 0, 4, 0]
            },
            [CalculationMethod.KarachiHanafi] = new MethodParams
            {
                FajrAngle = 18.0, IshaAngle = 18.0,
                Mathhab = Mathhab.Hanafi,
                Offsets = [0, 0, 0, 0, 0, 0]
            },
            [CalculationMethod.NorthAmerica] = new MethodParams
            {
                FajrAngle = 15.0, IshaAngle = 15.0,
                Offsets = [0, 0, 0, 0, 0, 0]
            },
            [CalculationMethod.MuslimLeague] = new MethodParams
            {
                FajrAngle = 18.0, IshaAngle = 17.0,
                NearestLat = 45.0, ExtremeLat = 48.0,
                Offsets = [0, 0, 0, 0, 0, 0]
            },
            [CalculationMethod.Paris] = new MethodParams
            {
                FajrAngle = 18.0, IshaAngle = 16.0,
                NearestLat = 45.0, ExtremeLat = 45.0,
                Offsets = [1, 0, 0, 0, 0, 0]
            },
            [CalculationMethod.UmmAlQura] = new MethodParams
            {
                FajrAngle = 18.5, IshaAngle = 0.0,
                IshaInterval = 90,
                Offsets = [0, 0, 0, 0, 1, 1]
            },
            [CalculationMethod.UmmAlQuraRamadan] = new MethodParams
            {
                FajrAngle = 18.5, IshaAngle = 0.0,
                IshaInterval = 120,
                Offsets = [0, 0, 0, 0, 1, 1]
            },
            [CalculationMethod.Uae] = new MethodParams
            {
                FajrAngle = 18.0, IshaAngle = 18.0,
                Offsets = [0, 0, 2, 2, 2, 0]
            },
            [CalculationMethod.Uoif] = new MethodParams
            {
                FajrAngle = 12.0, IshaAngle = 12.0,
                Offsets = [-5, 0, 5, 0, 2, 5]
            },
            [CalculationMethod.Algier] = new MethodParams
            {
                FajrAngle = 18.0, IshaAngle = 17.0,
                Offsets = [0, 0, 0, 0, 0, 0]
            },
            [CalculationMethod.Tunisia] = new MethodParams
            {
                FajrAngle = 18.0, IshaAngle = 18.0,
                Offsets = [0, 0, 7, 0, 2, 0]
            },
            [CalculationMethod.Kuwait] = new MethodParams
            {
                FajrAngle = 18.0, IshaAngle = 17.5,
                Offsets = [0, 0, 0, 0, 0, 0]
            },
            [CalculationMethod.Dyanet] = new MethodParams
            {
                FajrAngle = 18.0, IshaAngle = 17.0,
                NearestLat = 45.0, ExtremeLat = 48.0,
                Offsets = [1, 0, 5, 4, 0, 0]
            },
            [CalculationMethod.Igmg] = new MethodParams
            {
                FajrAngle = 0.0, IshaAngle = 0.0,
                FajrInterval = 80,
                IshaInterval = 70,
                NearestLat = 48.5, ExtremeLat = 48.0,
                Offsets = [-5, -5, 5, 5, 5, 5]
            },
            [CalculationMethod.BelgiumEmb] = new MethodParams
            {
                FajrAngle = 18.0, IshaAngle = 17.0,
                NearestLat = 48.5, ExtremeLat = 55.0,
                Offsets = [0, 0, 0, 0, 0, 0]
            }
        };
    }
}
