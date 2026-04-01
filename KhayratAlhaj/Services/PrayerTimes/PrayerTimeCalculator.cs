using KhayratAlhaj.Models.PrayerTimes;

namespace KhayratAlhaj.Services.PrayerTimes
{
    /// <summary>
    /// Computes prayer times using hour angle formulas.
    /// Implements the exact algorithm from Salaat First v6.3.1.
    /// </summary>
    public static class PrayerTimeCalculator
    {
        private const double Deg2Rad = Math.PI / 180.0;
        private const double Rad2Deg = 180.0 / Math.PI;
        private const double SunriseSunsetAngle = -0.83337; // degrees (refraction + semidiameter)

        /// <summary>
        /// Compute the hour angle for a given solar depression angle.
        /// Returns hours, or 99.0 if the sun never reaches this angle (extreme latitude).
        /// </summary>
        private static double HourAngle(double latitude, double declination, double angle)
        {
            double latRad = latitude * Deg2Rad;
            double cosH = (Math.Sin(angle * Deg2Rad) - Math.Sin(latRad) * Math.Sin(declination))
                         / (Math.Cos(latRad) * Math.Cos(declination));

            if (cosH <= -1.0 || cosH >= 1.0)
                return 99.0; // Sentinel: no valid hour angle

            return Math.Acos(cosH) * Rad2Deg / 15.0; // Convert to hours
        }

        /// <summary>
        /// Compute solar transit (Dhuhr/Zuhr) — solar noon in hours UTC.
        /// </summary>
        private static double SolarTransit(double longitude, double ra, double siderealTime)
        {
            double m0 = (ra - longitude - siderealTime) / 360.0;
            m0 -= Math.Floor(m0);
            return m0 * 24.0;
        }

        /// <summary>
        /// Compute all prayer times for a given date and location.
        /// </summary>
        public static DayPrayerTimes Calculate(
            DateTime date,
            double latitude,
            double longitude,
            double altitude,
            CalculationMethod method,
            double gmtOffset)
        {
            var methodParams = MethodParams.Presets[method];

            // 1. Compute Julian Day for this date at noon UTC
            double jd = SolarPosition.JulianDay(date.Year, date.Month, date.Day);

            // 2. Get solar position
            var astro = SolarPosition.ComputeSolarPosition(jd);

            // 3. Solar transit (Dhuhr) in UTC hours
            double transit = SolarTransit(longitude, astro.Ra, astro.SidTime);

            // 4. Sunrise (Shurooq) — base angle −0.83337° (refraction + semi-diameter)
            //    adjusted downward for city elevation: Δh = 0.0347° × √altitude(m)
            //    This lowers the effective horizon so higher-altitude cities get an
            //    earlier sunrise and later sunset, matching observed times.
            double altitudeDip = (altitude > 0) ? 0.0347 * Math.Sqrt(altitude) : 0.0;
            double effectiveSunAngle = SunriseSunsetAngle - altitudeDip;
            double sunriseHA = HourAngle(latitude, astro.Dec, effectiveSunAngle);
            double sunrise = transit - sunriseHA;

            // 5. Sunset (Maghrib) — same effective angle, after transit
            double sunset = transit + sunriseHA;

            // 6. Fajr
            double fajr;
            if (methodParams.FajrInterval > 0)
            {
                fajr = sunrise - methodParams.FajrInterval / 60.0;
            }
            else
            {
                double fajrHA = HourAngle(latitude, astro.Dec, -methodParams.FajrAngle);
                fajr = (fajrHA == 99.0)
                    ? HandleExtremeFajr(sunrise, sunset, methodParams)
                    : transit - fajrHA;
            }

            // 7. Isha
            double isha;
            if (methodParams.IshaInterval > 0)
            {
                isha = sunset + methodParams.IshaInterval / 60.0;
            }
            else
            {
                double ishaHA = HourAngle(latitude, astro.Dec, -methodParams.IshaAngle);
                isha = (ishaHA == 99.0)
                    ? HandleExtremeIsha(sunrise, sunset, methodParams)
                    : transit + ishaHA;
            }

            // 8. Asr
            double asr = CalculateAsr(transit, latitude, astro.Dec, methodParams.Mathhab);

            // 9. Imsaak (typically ~10 min before Fajr)
            double imsaak = fajr - 10.0 / 60.0;

            // 10. Next Fajr (calculate for the next day)
            var nextAstro = SolarPosition.ComputeSolarPosition(jd + 1);
            double nextTransit = SolarTransit(longitude, nextAstro.Ra, nextAstro.SidTime);
            double nextFajr;
            if (methodParams.FajrInterval > 0)
            {
                double nextSunriseHA = HourAngle(latitude, nextAstro.Dec, SunriseSunsetAngle);
                nextFajr = nextTransit - nextSunriseHA - methodParams.FajrInterval / 60.0;
            }
            else
            {
                double nextFajrHA = HourAngle(latitude, nextAstro.Dec, -methodParams.FajrAngle);
                nextFajr = (nextFajrHA == 99.0)
                    ? fajr + 24.0
                    : nextTransit - nextFajrHA;
            }

            // 11. Apply per-prayer offsets (in minutes)
            fajr += methodParams.Offsets[0] / 60.0;
            sunrise += methodParams.Offsets[1] / 60.0;
            transit += methodParams.Offsets[2] / 60.0;
            asr += methodParams.Offsets[3] / 60.0;
            sunset += methodParams.Offsets[4] / 60.0;
            isha += methodParams.Offsets[5] / 60.0;

            // 12. Convert from UTC to local time
            fajr += gmtOffset;
            sunrise += gmtOffset;
            transit += gmtOffset;
            asr += gmtOffset;
            sunset += gmtOffset;
            isha += gmtOffset;
            imsaak += gmtOffset;
            nextFajr += gmtOffset;

            return new DayPrayerTimes
            {
                Date = date,
                Method = method,
                Fajr = TimeSpanFromHours(fajr),
                Shurooq = TimeSpanFromHours(sunrise),
                Dhuhr = TimeSpanFromHours(transit),
                Asr = TimeSpanFromHours(asr),
                Maghrib = TimeSpanFromHours(sunset),
                Isha = TimeSpanFromHours(isha),
                Imsaak = TimeSpanFromHours(imsaak),
                NextFajr = TimeSpanFromHours(nextFajr)
            };
        }

        /// <summary>
        /// Asr calculation using the shadow ratio formula.
        /// </summary>
        private static double CalculateAsr(
            double transit, double latitude, double declination, Mathhab mathhab)
        {
            double latRad = latitude * Deg2Rad;

            // Shadow ratio: 1 for Shafi'i, 2 for Hanafi
            int shadowRatio = (mathhab == Mathhab.Hanafi) ? 2 : 1;

            // Shadow at solar noon = |tan(lat - dec)|
            double noonShadow = Math.Abs(Math.Tan(latRad - declination));

            // Target shadow = noonShadow + shadowRatio
            double targetShadow = noonShadow + shadowRatio;

            // Sun altitude when shadow = target
            double asrAltitude = Math.Atan(1.0 / targetShadow) * Rad2Deg;

            // Hour angle for this altitude
            double cosH = (Math.Sin(asrAltitude * Deg2Rad) - Math.Sin(latRad) * Math.Sin(declination))
                         / (Math.Cos(latRad) * Math.Cos(declination));

            if (cosH <= -1.0 || cosH >= 1.0) return transit + 4.0; // Fallback
            double asrHA = Math.Acos(cosH) * Rad2Deg / 15.0;

            return transit + asrHA;
        }

        /// <summary>
        /// Handle extreme latitude when Fajr angle doesn't produce a valid time.
        /// Uses the 1/7th night fallback.
        /// </summary>
        private static double HandleExtremeFajr(
            double sunrise, double sunset, MethodParams mp)
        {
            double nightDuration = 24.0 - (sunset - sunrise);
            return sunrise - nightDuration / 7.0;
        }

        /// <summary>
        /// Handle extreme latitude when Isha angle doesn't produce a valid time.
        /// Uses the 1/7th night fallback.
        /// </summary>
        private static double HandleExtremeIsha(
            double sunrise, double sunset, MethodParams mp)
        {
            double nightDuration = 24.0 - (sunset - sunrise);
            return sunset + nightDuration / 7.0;
        }

        /// <summary>
        /// Convert fractional hours to TimeSpan, handling wrap-around.
        /// Uses Math.Round on the total minutes to avoid floating-point truncation
        /// artifacts that could cause an off-by-one-minute error every few days.
        /// </summary>
        private static TimeSpan TimeSpanFromHours(double hours)
        {
            hours %= 24.0;
            if (hours < 0) hours += 24.0;

            // Round total minutes first, then decompose — avoids chained FP truncation.
            int totalMinutes = (int)Math.Round(hours * 60.0);
            int h = (totalMinutes / 60) % 24;
            int m = totalMinutes % 60;

            return new TimeSpan(h, m, 0);
        }
    }
}
