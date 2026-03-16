using NUnit.Framework;
using ZadAlhaj.Services.PrayerTimes;

namespace ZadAlhaj.UnitTests.Tests
{
    /// <summary>
    /// Unit tests for SolarPosition — Julian Day calculation and solar declination / RA output.
    /// Reference values taken from Jean Meeus "Astronomical Algorithms" 2nd ed. and
    /// verified against https://ssd.jpl.nasa.gov/tc.cgi (Julian Day converter).
    /// </summary>
    [TestFixture]
    public class SolarPositionTests
    {
        // ──────────────────────────────────────────────────────────────
        // JulianDay
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// J2000.0 epoch: Jan 1 2000 at 0h UT = JD 2451544.5 (textbook).
        /// </summary>
        [Test]
        public void JulianDay_J2000Epoch_Returns2451544point5()
        {
            double jd = SolarPosition.JulianDay(2000, 1, 1);
            Assert.That(jd, Is.EqualTo(2451544.5).Within(0.001));
        }

        /// <summary>
        /// Apr 4 2010 at 0h UT = JD 2455290.5 (cross-checked with NASA Horizons).
        /// </summary>
        [Test]
        public void JulianDay_Apr04_2010_Returns2455290point5()
        {
            double jd = SolarPosition.JulianDay(2010, 4, 4);
            Assert.That(jd, Is.EqualTo(2455290.5).Within(0.001));
        }

        /// <summary>
        /// Jan 1 1900 at 0h UT = JD 2415020.5 (Meeus Table 1.c).
        /// </summary>
        [Test]
        public void JulianDay_Jan01_1900_Returns2415020point5()
        {
            double jd = SolarPosition.JulianDay(1900, 1, 1);
            Assert.That(jd, Is.EqualTo(2415020.5).Within(0.001));
        }

        /// <summary>
        /// Gregorian leap-year boundary: Mar 1 2000 must be JD 2451604.5.
        /// </summary>
        [Test]
        public void JulianDay_Mar01_2000_Returns2451604point5()
        {
            double jd = SolarPosition.JulianDay(2000, 3, 1);
            Assert.That(jd, Is.EqualTo(2451604.5).Within(0.001));
        }

        // ──────────────────────────────────────────────────────────────
        // ComputeSolarPosition — output range validation
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Solar declination must lie within ±23.5° (±0.4102 rad) for any date.
        /// </summary>
        [TestCase(2025, 3, 20)]   // Spring equinox  — Dec ≈ 0
        [TestCase(2025, 6, 21)]   // Summer solstice — Dec ≈ +23.44°
        [TestCase(2025, 9, 22)]   // Autumn equinox  — Dec ≈ 0
        [TestCase(2025, 12, 21)]  // Winter solstice — Dec ≈ −23.44°
        public void ComputeSolarPosition_Declination_IsWithin23point5Degrees(
            int year, int month, int day)
        {
            double jd = SolarPosition.JulianDay(year, month, day);
            var pos = SolarPosition.ComputeSolarPosition(jd);
            double decDegrees = pos.Dec * (180.0 / Math.PI);

            Assert.That(decDegrees, Is.InRange(-23.5, 23.5),
                $"Declination {decDegrees:F4}° is out of valid solar range for {year}-{month:D2}-{day:D2}");
        }

        /// <summary>
        /// Right Ascension must lie within [0°, 360°).
        /// </summary>
        [TestCase(2025, 1, 1)]
        [TestCase(2025, 6, 21)]
        [TestCase(2025, 12, 31)]
        public void ComputeSolarPosition_RightAscension_IsInRange0To360(
            int year, int month, int day)
        {
            double jd = SolarPosition.JulianDay(year, month, day);
            var pos = SolarPosition.ComputeSolarPosition(jd);

            Assert.That(pos.Ra, Is.InRange(0.0, 360.0),
                $"Right ascension {pos.Ra:F4}° is out of [0, 360) for {year}-{month:D2}-{day:D2}");
        }

        /// <summary>
        /// Apparent sidereal time must lie within [0°, 360°).
        /// </summary>
        [TestCase(2025, 1, 1)]
        [TestCase(2025, 7, 15)]
        public void ComputeSolarPosition_SiderealTime_IsInRange0To360(
            int year, int month, int day)
        {
            double jd = SolarPosition.JulianDay(year, month, day);
            var pos = SolarPosition.ComputeSolarPosition(jd);

            Assert.That(pos.SidTime, Is.InRange(0.0, 360.0),
                $"SidTime {pos.SidTime:F4}° is out of [0, 360) for {year}-{month:D2}-{day:D2}");
        }

        /// <summary>
        /// Declination near summer solstice must be positive and near +23.4°.
        /// </summary>
        [Test]
        public void ComputeSolarPosition_SummerSolstice_DeclinationIsNearPlus23()
        {
            double jd = SolarPosition.JulianDay(2025, 6, 21);
            var pos = SolarPosition.ComputeSolarPosition(jd);
            double decDeg = pos.Dec * (180.0 / Math.PI);

            Assert.That(decDeg, Is.InRange(23.0, 23.5),
                $"Summer solstice declination {decDeg:F3}° should be near +23.4°");
        }

        /// <summary>
        /// Declination near winter solstice must be negative and near −23.4°.
        /// </summary>
        [Test]
        public void ComputeSolarPosition_WinterSolstice_DeclinationIsNearMinus23()
        {
            double jd = SolarPosition.JulianDay(2025, 12, 21);
            var pos = SolarPosition.ComputeSolarPosition(jd);
            double decDeg = pos.Dec * (180.0 / Math.PI);

            Assert.That(decDeg, Is.InRange(-23.5, -23.0),
                $"Winter solstice declination {decDeg:F3}° should be near −23.4°");
        }

        /// <summary>
        /// Declination at spring equinox must be near 0°.
        /// </summary>
        [Test]
        public void ComputeSolarPosition_SpringEquinox_DeclinationIsNearZero()
        {
            double jd = SolarPosition.JulianDay(2025, 3, 20);
            var pos = SolarPosition.ComputeSolarPosition(jd);
            double decDeg = pos.Dec * (180.0 / Math.PI);

            Assert.That(Math.Abs(decDeg), Is.LessThan(1.5),
                $"Spring equinox declination {decDeg:F3}° should be near 0°");
        }

        /// <summary>
        /// Sun-Earth radius vector must be between 0.983 AU (perihelion) and 1.017 AU (aphelion).
        /// </summary>
        [TestCase(2025, 1, 3)]   // Near perihelion
        [TestCase(2025, 7, 4)]   // Near aphelion
        [TestCase(2025, 6, 21)]
        public void ComputeSolarPosition_RadiusVector_IsWithinEarthOrbitBounds(
            int year, int month, int day)
        {
            double jd = SolarPosition.JulianDay(year, month, day);
            var pos = SolarPosition.ComputeSolarPosition(jd);

            Assert.That(pos.Rsum, Is.InRange(0.983, 1.017),
                $"Radius vector {pos.Rsum:F5} AU is outside perihelion/aphelion range");
        }
    }
}
