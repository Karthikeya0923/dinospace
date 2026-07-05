using System;
using System.Collections.Generic;
using System.Linq;

// Desktop stand-in for the app's SkyService (which uses MAUI location and
// preferences). Same report logic, fixed Guelph-ish fallback location.
namespace dinospace
{
    public sealed record SkyLocation(double Lat, double Lon, bool FromDevice);
    public sealed record PlanetSighting(string Name, double AltDeg, double AzDeg, string Note);
    public sealed record ConstellationSighting(Constellation Constellation, double AltDeg, double AzDeg);
    public sealed record SkyReport(
        SkyLocation Where, bool IsNight, DateTime ReferenceLocal, SkyCalc.MoonInfo Moon,
        bool MoonUp, double MoonAltDeg, double MoonAzDeg,
        List<PlanetSighting> Planets, List<ConstellationSighting> Constellations,
        DateTime? NextSunsetLocal, DateTime? NextSunriseLocal);

    public static class SkyService
    {
        public static SkyLocation Cached => new(45.0, -80.0, false);

        public static SkyReport BuildReport(SkyLocation w)
        {
            var nowUtc = DateTime.UtcNow;
            bool isNight = SkyCalc.SunAltitude(w.Lat, w.Lon, nowUtc) < -6;
            var refUtc = nowUtc;
            var (r0, s0) = SkyCalc.SunRiseSet(DateTime.Now.Date, w.Lat, w.Lon);
            if (!isNight && s0 is DateTime s) refUtc = s.AddMinutes(75);
            var moon = SkyCalc.Moon(nowUtc);
            double jd = SkyCalc.JulianDay(refUtc);
            var (mra, mdec) = SkyCalc.MoonRaDec(jd);
            var (mAlt, mAz) = SkyCalc.AltAz(mra, mdec, w.Lat, w.Lon, refUtc);
            var planets = new List<PlanetSighting>();
            foreach (var b in Enum.GetValues<SkyCalc.Body>())
            {
                var (ra, dec, _) = SkyCalc.PlanetRaDec(b, jd);
                var (alt, az) = SkyCalc.AltAz(ra, dec, w.Lat, w.Lon, refUtc);
                if (alt > 5) planets.Add(new PlanetSighting(b.ToString(), alt, az, ""));
            }
            var cons = SkyData.All
                .Select(c => { var (alt, az) = SkyCalc.AltAz(c.RaHours * 15.0, c.DecDeg, w.Lat, w.Lon, refUtc); return new ConstellationSighting(c, alt, az); })
                .Where(x => x.AltDeg > 22).OrderByDescending(x => x.AltDeg).Take(7).ToList();
            return new SkyReport(w, isNight, refUtc.ToLocalTime(), moon, mAlt > 0, mAlt, mAz, planets, cons,
                s0?.ToLocalTime(), r0?.ToLocalTime());
        }

        public static string FormatTime(DateTime t) => t.ToString("h:mm") + (t.Hour < 12 ? " a.m." : " p.m.");
        public static string Describe(double altDeg, double azDeg)
        {
            if (altDeg > 65) return "almost straight overhead";
            string height = altDeg > 40 ? "high " : altDeg < 15 ? "low " : "";
            return $"{height}in the {SkyCalc.Compass(azDeg)}";
        }
    }
}
