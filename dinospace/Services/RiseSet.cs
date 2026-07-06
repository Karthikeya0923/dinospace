using System;

namespace dinospace
{
    // Rise, set, and twilight times for anything in the sky. Works by scanning
    // the body's altitude through the day and bisecting each horizon crossing —
    // slower than a closed formula but correct for everything, including the
    // moon (whose position changes noticeably hour to hour).
    public static class RiseSet
    {
        // Altitude of a body at time t, via a position supplier (RA/Dec can
        // change with time, which is why this takes a function).
        public delegate (double raDeg, double decDeg) PositionAt(DateTime utc);

        public sealed record DayEvents(DateTime? RiseUtc, DateTime? SetUtc);

        // Find when the body crosses `horizonAlt` degrees during the UTC day
        // starting at `dayStartUtc`. Standard horizon for stars/planets is
        // -0.567° (refraction); the moon uses about +0.125 (refraction minus
        // its large apparent radius roughly cancels — close enough here).
        public static DayEvents FindCrossings(PositionAt position, double lat, double lon,
                                              DateTime dayStartUtc, double horizonAlt = -0.567)
        {
            double AltAt(DateTime t)
            {
                var (ra, dec) = position(t);
                return SkyCalc.AltAz(ra, dec, lat, lon, t).altDeg;
            }

            DateTime? rise = null, set = null;
            double prev = AltAt(dayStartUtc);
            for (int step = 1; step <= 48; step++)          // half-hour scan
            {
                var t1 = dayStartUtc.AddMinutes(step * 30);
                double curr = AltAt(t1);
                if (prev < horizonAlt && curr >= horizonAlt && rise == null)
                    rise = Bisect(t1.AddMinutes(-30), t1, horizonAlt, AltAt, rising: true);
                if (prev >= horizonAlt && curr < horizonAlt && set == null)
                    set = Bisect(t1.AddMinutes(-30), t1, horizonAlt, AltAt, rising: false);
                prev = curr;
            }
            return new DayEvents(rise, set);
        }

        private static DateTime Bisect(DateTime lo, DateTime hi, double target, Func<DateTime, double> altAt, bool rising)
        {
            for (int i = 0; i < 20; i++)                    // to well under a minute
            {
                var mid = lo.AddTicks((hi - lo).Ticks / 2);
                bool below = altAt(mid) < target;
                if (below == rising) lo = mid; else hi = mid;
            }
            return hi;
        }

        // Moonrise and moonset for the UTC day. Either can be null — the moon
        // skips a rise or set roughly once a month.
        public static DayEvents MoonRiseSet(double lat, double lon, DateTime dayStartUtc)
            => FindCrossings(t => SkyCalc.MoonRaDec(SkyCalc.JulianDay(t)), lat, lon, dayStartUtc, horizonAlt: 0.125);

        // When the sun drops below (dusk) and climbs back above (dawn) a
        // twilight altitude: -6 civil, -12 nautical, -18 astronomical (fully
        // dark — when the faint stuff comes out).
        public static (DateTime? duskUtc, DateTime? dawnUtc) Twilights(double lat, double lon,
                                                                       DateTime dayStartUtc, double sunAltDeg = -18)
        {
            var events = FindCrossings(t => SkyCalc.SunRaDec(SkyCalc.JulianDay(t)), lat, lon, dayStartUtc, sunAltDeg);
            return (events.SetUtc, events.RiseUtc);
        }
    }
}
