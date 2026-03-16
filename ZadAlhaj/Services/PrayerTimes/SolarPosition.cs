namespace ZadAlhaj.Services.PrayerTimes
{
    /// <summary>
    /// Helper class holding solar position data for a single Julian Day.
    /// </summary>
    public class AstroDay
    {
        public double Dec { get; set; }      // Declination (radians)
        public double Ra { get; set; }       // Right Ascension (degrees)
        public double Rsum { get; set; }     // Radius vector (AU)
        public double SidTime { get; set; }  // Apparent Sidereal Time (degrees)
    }

    /// <summary>
    /// Simplified Meeus solar position algorithm.
    /// Accuracy within ~1 minute — sufficient for prayer time calculations.
    /// Based on Jean Meeus' "Astronomical Algorithms" (2nd ed., Chapter 25).
    /// </summary>
    public static class SolarPosition
    {
        private const double Deg2Rad = Math.PI / 180.0;
        private const double Rad2Deg = 180.0 / Math.PI;

        /// <summary>
        /// Compute Julian Day Number from a calendar date.
        /// </summary>
        public static double JulianDay(int year, int month, int day)
        {
            if (month <= 2)
            {
                year -= 1;
                month += 12;
            }
            int A = year / 100;
            int B = 2 - A + A / 4;
            return Math.Floor(365.25 * (year + 4716))
                 + Math.Floor(30.6001 * (month + 1))
                 + day + B - 1524.5;
        }

        /// <summary>
        /// Compute the Sun's declination, RA, and sidereal time for a given Julian Day.
        /// Uses the simplified Meeus algorithm.
        /// </summary>
        public static AstroDay ComputeSolarPosition(double jd)
        {
            // Julian centuries from J2000.0
            double T = (jd - 2451545.0) / 36525.0;

            // Geometric mean longitude of the Sun (degrees)
            double L0 = LimitAngle(280.46646 + 36000.76983 * T + 0.0003032 * T * T);

            // Mean anomaly of the Sun (degrees)
            double M = LimitAngle(357.52911 + 35999.05029 * T - 0.0001537 * T * T);
            double Mrad = M * Deg2Rad;

            // Equation of center
            double C = (1.914602 - 0.004817 * T - 0.000014 * T * T) * Math.Sin(Mrad)
                     + (0.019993 - 0.000101 * T) * Math.Sin(2 * Mrad)
                     + 0.000289 * Math.Sin(3 * Mrad);

            // Sun's true longitude
            double sunLon = L0 + C;

            // Sun's true anomaly
            double v = M + C;

            // Sun-Earth distance (AU)
            double R = (1.000001018 * (1 - 0.016708634 * 0.016708634))
                     / (1 + 0.016708634 * Math.Cos(v * Deg2Rad));

            // Apparent longitude (with nutation & aberration)
            double omega = 125.04 - 1934.136 * T;
            double lambda = sunLon - 0.00569 - 0.00478 * Math.Sin(omega * Deg2Rad);

            // Mean obliquity of ecliptic
            double eps0 = 23.0 + (26.0 + (21.448 - T * (46.8150 + T * (0.00059 - T * 0.001813))) / 60.0) / 60.0;
            // Corrected obliquity
            double eps = eps0 + 0.00256 * Math.Cos(omega * Deg2Rad);
            double epsRad = eps * Deg2Rad;

            // Right Ascension (degrees)
            double ra = Math.Atan2(Math.Cos(epsRad) * Math.Sin(lambda * Deg2Rad),
                                   Math.Cos(lambda * Deg2Rad)) * Rad2Deg;
            ra = LimitAngle(ra);

            // Declination (radians)
            double dec = Math.Asin(Math.Sin(epsRad) * Math.Sin(lambda * Deg2Rad));

            // Sidereal time at Greenwich (degrees)
            double theta0 = 280.46061837 + 360.98564736629 * (jd - 2451545.0)
                           + 0.000387933 * T * T - T * T * T / 38710000.0;
            theta0 = LimitAngle(theta0);

            // Nutation in longitude (simplified)
            double nutLon = -0.00478 * Math.Sin(omega * Deg2Rad);
            double apparentSid = theta0 + nutLon * Math.Cos(epsRad);

            return new AstroDay
            {
                Ra = ra,
                Dec = dec,
                Rsum = R,
                SidTime = apparentSid
            };
        }

        /// <summary>
        /// Normalize angle to [0, 360).
        /// </summary>
        public static double LimitAngle(double angle)
        {
            angle %= 360.0;
            if (angle < 0) angle += 360.0;
            return angle;
        }
    }
}
