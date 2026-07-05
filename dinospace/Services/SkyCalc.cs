using System;

namespace dinospace
{
    // The astronomy engine behind Sky Tonight. Pure math, no UI, no network:
    // moon phase (Meeus), sun rise/set (NOAA method), planet positions (NASA
    // JPL mean Keplerian elements, valid 1800-2050), and alt-az conversion for
    // "what's above you right now". Everything works from a UTC time plus an
    // approximate latitude/longitude.
    public static class SkyCalc
    {
        private const double Deg = Math.PI / 180.0;
        public const double SynodicMonth = 29.530588853;   // days, new moon to new moon

        // ---------- time ----------

        public static double JulianDay(DateTime utc)
        {
            // Standard Gregorian-calendar Julian Day, valid for all app dates.
            int y = utc.Year, m = utc.Month;
            double d = utc.Day + (utc.Hour + (utc.Minute + (utc.Second + utc.Millisecond / 1000.0) / 60.0) / 60.0) / 24.0;
            if (m <= 2) { y--; m += 12; }
            int a = y / 100;
            int b = 2 - a + a / 4;
            return Math.Floor(365.25 * (y + 4716)) + Math.Floor(30.6001 * (m + 1)) + d + b - 1524.5;
        }

        private static double Centuries(double jd) => (jd - 2451545.0) / 36525.0;

        private static double Wrap360(double deg)
        {
            deg %= 360.0;
            return deg < 0 ? deg + 360.0 : deg;
        }

        // Greenwich mean sidereal time, in degrees.
        public static double Gmst(double jd)
        {
            double t = Centuries(jd);
            return Wrap360(280.46061837 + 360.98564736629 * (jd - 2451545.0)
                           + 0.000387933 * t * t - t * t * t / 38710000.0);
        }

        private static double Obliquity(double t) => 23.439291 - 0.0130042 * t;

        // ---------- sun ----------

        // Geometric ecliptic longitude of the sun, in degrees (Meeus ch. 25).
        public static double SunLongitude(double jd)
        {
            double t = Centuries(jd);
            double l0 = 280.46646 + 36000.76983 * t + 0.0003032 * t * t;
            double m = (357.52911 + 35999.05029 * t - 0.0001537 * t * t) * Deg;
            double c = (1.914602 - 0.004817 * t - 0.000014 * t * t) * Math.Sin(m)
                     + (0.019993 - 0.000101 * t) * Math.Sin(2 * m)
                     + 0.000289 * Math.Sin(3 * m);
            return Wrap360(l0 + c);
        }

        public static (double raDeg, double decDeg) SunRaDec(double jd)
            => EclipticToEquatorial(SunLongitude(jd), 0, Centuries(jd));

        // Sunrise and sunset (UTC) for the local calendar date, NOAA's method.
        // Null when the sun never rises or never sets there that day.
        public static (DateTime? riseUtc, DateTime? setUtc) SunRiseSet(DateTime localDate, double lat, double lon)
        {
            var noonUtc = new DateTime(localDate.Year, localDate.Month, localDate.Day, 12, 0, 0, DateTimeKind.Local).ToUniversalTime();
            int doy = localDate.DayOfYear;

            double g = 2 * Math.PI / 365.0 * (doy - 1 + (noonUtc.Hour - 12) / 24.0);
            double eqTime = 229.18 * (0.000075 + 0.001868 * Math.Cos(g) - 0.032077 * Math.Sin(g)
                          - 0.014615 * Math.Cos(2 * g) - 0.040849 * Math.Sin(2 * g));
            double decl = 0.006918 - 0.399912 * Math.Cos(g) + 0.070257 * Math.Sin(g)
                        - 0.006758 * Math.Cos(2 * g) + 0.000907 * Math.Sin(2 * g)
                        - 0.002697 * Math.Cos(3 * g) + 0.00148 * Math.Sin(3 * g);

            // Hour angle for the top of the sun touching the horizon (90.833°
            // accounts for refraction and the sun's radius).
            double cosHa = Math.Cos(90.833 * Deg) / (Math.Cos(lat * Deg) * Math.Cos(decl))
                         - Math.Tan(lat * Deg) * Math.Tan(decl);
            if (cosHa > 1 || cosHa < -1) return (null, null);
            double haDeg = Math.Acos(cosHa) / Deg;

            double riseMin = 720 - 4 * (lon + haDeg) - eqTime;   // minutes after 00:00 UTC of the local date
            double setMin = 720 - 4 * (lon - haDeg) - eqTime;
            var utcBase = new DateTime(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, DateTimeKind.Utc);
            return (utcBase.AddMinutes(riseMin), utcBase.AddMinutes(setMin));
        }

        // ---------- moon ----------

        public enum MoonPhaseKind { New, WaxingCrescent, FirstQuarter, WaxingGibbous, Full, WaningGibbous, LastQuarter, WaningCrescent }

        public sealed record MoonInfo(
            MoonPhaseKind Phase, string PhaseName, double Illumination, double AgeDays,
            double ElongationDeg, bool Waxing, DateTime NextFullUtc, DateTime NextNewUtc);

        // Moon ecliptic longitude in degrees — the main terms of the Meeus
        // (ch. 47) series, good to ~0.05°, far more than phase needs.
        public static double MoonLongitude(double jd)
        {
            double t = Centuries(jd);
            double lp = Wrap360(218.3164477 + 481267.88123421 * t - 0.0015786 * t * t);
            double d = (297.8501921 + 445267.1114034 * t - 0.0018819 * t * t) * Deg;
            double m = (357.5291092 + 35999.0502909 * t - 0.0001536 * t * t) * Deg;
            double mp = (134.9633964 + 477198.8675055 * t + 0.0087414 * t * t) * Deg;
            double f = (93.2720950 + 483202.0175233 * t - 0.0036539 * t * t) * Deg;

            double lon = lp
                + 6.288774 * Math.Sin(mp)
                + 1.274027 * Math.Sin(2 * d - mp)
                + 0.658314 * Math.Sin(2 * d)
                + 0.213618 * Math.Sin(2 * mp)
                - 0.185116 * Math.Sin(m)
                - 0.114332 * Math.Sin(2 * f)
                + 0.058793 * Math.Sin(2 * d - 2 * mp)
                + 0.057066 * Math.Sin(2 * d - m - mp)
                + 0.053322 * Math.Sin(2 * d + mp)
                + 0.045758 * Math.Sin(2 * d - m)
                - 0.040923 * Math.Sin(m - mp)
                - 0.034720 * Math.Sin(d)
                - 0.030383 * Math.Sin(m + mp);
            return Wrap360(lon);
        }

        // Moon ecliptic latitude in degrees (top terms; enough for direction).
        public static double MoonLatitude(double jd)
        {
            double t = Centuries(jd);
            double d = (297.8501921 + 445267.1114034 * t) * Deg;
            double mp = (134.9633964 + 477198.8675055 * t) * Deg;
            double f = (93.2720950 + 483202.0175233 * t) * Deg;
            return 5.128122 * Math.Sin(f)
                 + 0.280602 * Math.Sin(mp + f)
                 + 0.277693 * Math.Sin(mp - f)
                 + 0.173237 * Math.Sin(2 * d - f);
        }

        public static (double raDeg, double decDeg) MoonRaDec(double jd)
            => EclipticToEquatorial(MoonLongitude(jd), MoonLatitude(jd), Centuries(jd));

        // Sun-moon elongation in degrees: 0 = new, 90 = first quarter, 180 = full.
        public static double MoonElongation(double jd) => Wrap360(MoonLongitude(jd) - SunLongitude(jd));

        public static MoonInfo Moon(DateTime utc)
        {
            double jd = JulianDay(utc);
            double e = MoonElongation(jd);
            double illum = (1 - Math.Cos(e * Deg)) / 2.0;
            double age = e / 360.0 * SynodicMonth;
            bool waxing = e < 180;

            var kind = PhaseKind(e);
            return new MoonInfo(kind, PhaseName(kind), illum, age, e, waxing,
                NextElongation(utc, 180), NextElongation(utc, 0));
        }

        // The cardinal names get a ±10° window (about ±¾ of a day) so "Full
        // Moon" is shown on the night it actually looks full.
        private static MoonPhaseKind PhaseKind(double e) => e switch
        {
            < 10 or > 350 => MoonPhaseKind.New,
            < 80 => MoonPhaseKind.WaxingCrescent,
            < 100 => MoonPhaseKind.FirstQuarter,
            < 170 => MoonPhaseKind.WaxingGibbous,
            < 190 => MoonPhaseKind.Full,
            < 260 => MoonPhaseKind.WaningGibbous,
            < 280 => MoonPhaseKind.LastQuarter,
            _ => MoonPhaseKind.WaningCrescent
        };

        public static string PhaseName(MoonPhaseKind k) => k switch
        {
            MoonPhaseKind.New => "New Moon",
            MoonPhaseKind.WaxingCrescent => "Waxing Crescent",
            MoonPhaseKind.FirstQuarter => "First Quarter",
            MoonPhaseKind.WaxingGibbous => "Waxing Gibbous",
            MoonPhaseKind.Full => "Full Moon",
            MoonPhaseKind.WaningGibbous => "Waning Gibbous",
            MoonPhaseKind.LastQuarter => "Last Quarter",
            _ => "Waning Crescent"
        };

        // Walk forward in time to the next moment the elongation crosses
        // `target` degrees: coarse 6-hour steps, then bisection to the minute.
        private static DateTime NextElongation(DateTime utc, double target)
        {
            double Diff(DateTime t)
            {
                double d = MoonElongation(JulianDay(t)) - target;
                while (d > 180) d -= 360;
                while (d < -180) d += 360;
                return d;
            }

            var a = utc;
            double da = Diff(a);
            for (int i = 0; i < 130; i++)   // covers a full synodic month in 6 h steps
            {
                var b = a.AddHours(6);
                double db = Diff(b);
                // Crossing found when the signed difference passes upward through 0.
                if (da < 0 && db >= 0)
                {
                    for (int j = 0; j < 24; j++)   // bisect to under a minute
                    {
                        var mid = a.AddTicks((b - a).Ticks / 2);
                        if (Diff(mid) < 0) a = mid; else b = mid;
                    }
                    return b;
                }
                a = b; da = db;
            }
            return utc.AddDays(SynodicMonth); // unreachable fallback
        }

        // ---------- planets (NASA JPL approximate elements, 1800-2050) ----------

        public enum Body { Mercury, Venus, Mars, Jupiter, Saturn }

        // a (au), e, I (deg), L (deg), longitude of perihelion (deg), longitude
        // of ascending node (deg) — J2000 value and per-century rate.
        // Source: NASA JPL "Approximate Positions of the Planets", Table 1.
        private static readonly double[][] Elements =
        {
            // Mercury
            new[] { 0.38709927, 0.00000037, 0.20563593, 0.00001906, 7.00497902, -0.00594749, 252.25032350, 149472.67411175, 77.45779628, 0.16047689, 48.33076593, -0.12534081 },
            // Venus
            new[] { 0.72333566, 0.00000390, 0.00677672, -0.00004107, 3.39467605, -0.00078890, 181.97909950, 58517.81538729, 131.60246718, 0.00268329, 76.67984255, -0.27769418 },
            // Mars
            new[] { 1.52371034, 0.00001847, 0.09339410, 0.00007882, 1.84969142, -0.00813131, -4.55343205, 19140.30268499, -23.94362959, 0.44441088, 49.55953891, -0.29257343 },
            // Jupiter
            new[] { 5.20288700, -0.00011607, 0.04838624, -0.00013253, 1.30439695, -0.00183714, 34.39644051, 3034.74612775, 14.72847983, 0.21252668, 100.47390909, 0.20469106 },
            // Saturn
            new[] { 9.53667594, -0.00125060, 0.05386179, -0.00050991, 2.48599187, 0.00193609, 49.95424423, 1222.49362201, 92.59887831, -0.41897216, 113.66242448, -0.28867794 },
        };

        // Earth-Moon barycentre, same table — needed to go heliocentric → geocentric.
        private static readonly double[] EarthElements =
            { 1.00000261, 0.00000562, 0.01671123, -0.00004392, -0.00001531, -0.01294668, 100.46457166, 35999.37244981, 102.93768193, 0.32327364, 0.0, 0.0 };

        // Heliocentric ecliptic position (au) from mean elements at time t.
        private static (double x, double y, double z) Heliocentric(double[] el, double t)
        {
            double a = el[0] + el[1] * t;
            double e = el[2] + el[3] * t;
            double inc = (el[4] + el[5] * t) * Deg;
            double L = el[6] + el[7] * t;
            double lonPeri = el[8] + el[9] * t;
            double lonNode = el[10] + el[11] * t;

            double m = Wrap360(L - lonPeri) * Deg;
            double w = (lonPeri - lonNode) * Deg;
            double node = lonNode * Deg;

            // Kepler's equation by Newton's method (converges in a few steps).
            double E = m;
            for (int i = 0; i < 8; i++)
                E -= (E - e * Math.Sin(E) - m) / (1 - e * Math.Cos(E));

            double xo = a * (Math.Cos(E) - e);                     // orbital plane, x toward perihelion
            double yo = a * Math.Sqrt(1 - e * e) * Math.Sin(E);

            double cw = Math.Cos(w), sw = Math.Sin(w);
            double cn = Math.Cos(node), sn = Math.Sin(node);
            double ci = Math.Cos(inc), si = Math.Sin(inc);

            double x = (cw * cn - sw * sn * ci) * xo + (-sw * cn - cw * sn * ci) * yo;
            double y = (cw * sn + sw * cn * ci) * xo + (-sw * sn + cw * cn * ci) * yo;
            double z = (sw * si) * xo + (cw * si) * yo;
            return (x, y, z);
        }

        // Geocentric RA/Dec of a planet, plus its distance from Earth in au.
        public static (double raDeg, double decDeg, double distAu) PlanetRaDec(Body body, double jd)
        {
            double t = Centuries(jd);
            var p = Heliocentric(Elements[(int)body], t);
            var earth = Heliocentric(EarthElements, t);

            double gx = p.x - earth.x, gy = p.y - earth.y, gz = p.z - earth.z;
            double dist = Math.Sqrt(gx * gx + gy * gy + gz * gz);
            double lon = Wrap360(Math.Atan2(gy, gx) / Deg);
            double lat = Math.Atan2(gz, Math.Sqrt(gx * gx + gy * gy)) / Deg;
            // The elements are J2000; precess the longitude to today's equinox
            // so alt-az (which uses sidereal time of date) lines up.
            lon = Wrap360(lon + 1.39697 * t);
            var (ra, dec) = EclipticToEquatorial(lon, lat, t);
            return (ra, dec, dist);
        }

        // ---------- coordinate conversions ----------

        public static (double raDeg, double decDeg) EclipticToEquatorial(double lonDeg, double latDeg, double t)
        {
            double eps = Obliquity(t) * Deg;
            double lon = lonDeg * Deg, lat = latDeg * Deg;
            double ra = Math.Atan2(Math.Sin(lon) * Math.Cos(eps) - Math.Tan(lat) * Math.Sin(eps), Math.Cos(lon)) / Deg;
            double dec = Math.Asin(Math.Sin(lat) * Math.Cos(eps) + Math.Cos(lat) * Math.Sin(eps) * Math.Sin(lon)) / Deg;
            return (Wrap360(ra), dec);
        }

        // Where a fixed RA/Dec appears in the local sky: altitude above the
        // horizon and azimuth from north (0° = N, 90° = E).
        public static (double altDeg, double azDeg) AltAz(double raDeg, double decDeg, double lat, double lon, DateTime utc)
        {
            double lst = Wrap360(Gmst(JulianDay(utc)) + lon);          // local sidereal time, degrees
            double h = Wrap360(lst - raDeg) * Deg;                     // hour angle
            double phi = lat * Deg, dec = decDeg * Deg;

            double sinAlt = Math.Sin(phi) * Math.Sin(dec) + Math.Cos(phi) * Math.Cos(dec) * Math.Cos(h);
            double alt = Math.Asin(Math.Clamp(sinAlt, -1, 1));
            double cosAlt = Math.Cos(alt);
            if (Math.Abs(cosAlt) < 1e-9) return (alt / Deg, 0);        // zenith: azimuth is meaningless

            double sinA = -Math.Cos(dec) * Math.Sin(h) / cosAlt;
            double cosA = (Math.Sin(dec) - Math.Sin(phi) * sinAlt) / (Math.Cos(phi) * cosAlt);
            double az = Wrap360(Math.Atan2(sinA, cosA) / Deg);
            return (alt / Deg, az);
        }

        // Sun's altitude right now — used to decide whether it's dark enough
        // for stargazing and which reference time "tonight" means.
        public static double SunAltitude(double lat, double lon, DateTime utc)
        {
            var (ra, dec) = SunRaDec(JulianDay(utc));
            return AltAz(ra, dec, lat, lon, utc).altDeg;
        }

        // "NE", "SSW" style compass point from an azimuth.
        public static string Compass(double azDeg)
        {
            string[] pts = { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" };
            return pts[(int)Math.Round(Wrap360(azDeg) / 45.0) % 8];
        }
    }
}
