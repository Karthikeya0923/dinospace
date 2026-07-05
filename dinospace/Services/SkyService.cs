using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;

namespace dinospace
{
    // Where the observer is. When location is off we fall back to a "general
    // view": latitude 45° north and a longitude guessed from the time zone —
    // close enough that the sky is right to within about half an hour.
    public sealed record SkyLocation(double Lat, double Lon, bool FromDevice);

    public sealed record PlanetSighting(string Name, double AltDeg, double AzDeg, string Note);
    public sealed record ConstellationSighting(Constellation Constellation, double AltDeg, double AzDeg);

    // Everything the Sky Tonight UI needs, computed in one shot.
    public sealed record SkyReport(
        SkyLocation Where,
        bool IsNight,                       // dark enough for stargazing right now
        DateTime ReferenceLocal,            // the instant the sightings are computed for
        SkyCalc.MoonInfo Moon,
        bool MoonUp, double MoonAltDeg, double MoonAzDeg,
        List<PlanetSighting> Planets,
        List<ConstellationSighting> Constellations,
        DateTime? NextSunsetLocal, DateTime? NextSunriseLocal);

    public static class SkyService
    {
        private const string KeyLat = "sky.lat";
        private const string KeyLon = "sky.lon";
        private const string KeyDevice = "sky.fromDevice";

        public static SkyLocation Cached =>
            Preferences.ContainsKey(KeyLat)
                ? new SkyLocation(Preferences.Get(KeyLat, 45.0), Preferences.Get(KeyLon, 0.0), Preferences.Get(KeyDevice, false))
                : GeneralView();

        public static SkyLocation GeneralView()
        {
            double lon = Math.Clamp(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalHours * 15.0, -180, 180);
            return new SkyLocation(45.0, lon, false);
        }

        // Best location without ever prompting: the cache, refreshed silently
        // when permission was already granted earlier.
        public static async Task<SkyLocation> GetQuietlyAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status == PermissionStatus.Granted)
                {
                    var loc = await Geolocation.Default.GetLastKnownLocationAsync();
                    if (loc != null) return Save(loc);
                }
            }
            catch { }
            return Cached;
        }

        // Ask for permission (only from an explicit user tap) and fetch a
        // coarse fix. Null when the user says no or nothing comes back.
        public static async Task<SkyLocation?> RequestDeviceLocationAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted) return null;

                var loc = await Geolocation.Default.GetLastKnownLocationAsync();
                loc ??= await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Low, TimeSpan.FromSeconds(10)));
                return loc == null ? null : Save(loc);
            }
            catch { return null; }
        }

        // Only a rough position is ever kept: rounded to 0.1° (about 11 km),
        // stored on-device, never sent anywhere.
        private static SkyLocation Save(Location loc)
        {
            var l = new SkyLocation(Math.Round(loc.Latitude, 1), Math.Round(loc.Longitude, 1), true);
            Preferences.Set(KeyLat, l.Lat);
            Preferences.Set(KeyLon, l.Lon);
            Preferences.Set(KeyDevice, true);
            return l;
        }

        // ---------- the report ----------

        public static SkyReport BuildReport(SkyLocation w)
        {
            var nowUtc = DateTime.UtcNow;
            bool isNight = SkyCalc.SunAltitude(w.Lat, w.Lon, nowUtc) < -6;   // civil dark

            // Sightings are computed for right now at night, or for shortly
            // after tonight's sunset during the day.
            var refUtc = nowUtc;
            var (setToday, riseToday) = NextSunTimes(w, nowUtc);
            if (!isNight && setToday is DateTime s)
                refUtc = s.AddMinutes(75);

            var moon = SkyCalc.Moon(nowUtc);
            double jdRef = SkyCalc.JulianDay(refUtc);

            var (mra, mdec) = SkyCalc.MoonRaDec(jdRef);
            var (mAlt, mAz) = SkyCalc.AltAz(mra, mdec, w.Lat, w.Lon, refUtc);

            var planets = new List<PlanetSighting>();
            foreach (var b in Enum.GetValues<SkyCalc.Body>())
            {
                var (ra, dec, _) = SkyCalc.PlanetRaDec(b, jdRef);
                var (alt, az) = SkyCalc.AltAz(ra, dec, w.Lat, w.Lon, refUtc);
                // Mercury never strays far from the sun — check it in twilight,
                // where it actually shows, instead of after full dark.
                if (b == SkyCalc.Body.Mercury && setToday is DateTime st)
                {
                    var twilight = isNight ? refUtc : st.AddMinutes(40);
                    (alt, az) = SkyCalc.AltAz(ra, dec, w.Lat, w.Lon, twilight);
                    if (alt > 3) planets.Add(new PlanetSighting("Mercury", alt, az, "very low — catch it just after sunset"));
                    continue;
                }
                if (alt > 5) planets.Add(new PlanetSighting(b.ToString(), alt, az, PlanetNote(b)));
            }

            var constellations = SkyData.All
                .Select(c =>
                {
                    var (alt, az) = SkyCalc.AltAz(c.RaHours * 15.0, c.DecDeg, w.Lat, w.Lon, refUtc);
                    return new ConstellationSighting(c, alt, az);
                })
                .Where(s => s.AltDeg > 22)
                .OrderByDescending(s => s.AltDeg)
                .Take(7)
                .ToList();

            return new SkyReport(w, isNight, refUtc.ToLocalTime(), moon,
                mAlt > 0, mAlt, mAz, planets, constellations,
                setToday?.ToLocalTime(), riseToday?.ToLocalTime());
        }

        private static string PlanetNote(SkyCalc.Body b) => b switch
        {
            SkyCalc.Body.Venus => "unmistakable — the brightest thing after the moon",
            SkyCalc.Body.Mars => "look for its rusty orange colour",
            SkyCalc.Body.Jupiter => "big, bright and steady",
            SkyCalc.Body.Saturn => "a calm golden point — rings in any small telescope",
            _ => ""
        };

        // The next sunset and sunrise from now (UTC), looking a day ahead.
        private static (DateTime? set, DateTime? rise) NextSunTimes(SkyLocation w, DateTime nowUtc)
        {
            DateTime? set = null, rise = null;
            for (int d = 0; d <= 1 && (set == null || rise == null); d++)
            {
                var (r, s) = SkyCalc.SunRiseSet(DateTime.Now.Date.AddDays(d), w.Lat, w.Lon);
                if (set == null && s > nowUtc) set = s;
                if (rise == null && r > nowUtc) rise = r;
            }
            return (set, rise);
        }

        // ---------- friendly wording ----------

        // "high in the southeast", "low in the west" ...
        public static string Describe(double altDeg, double azDeg)
        {
            if (altDeg > 65) return "almost straight overhead";
            string height = altDeg > 40 ? "high " : altDeg < 15 ? "low " : "";
            return $"{height}in the {SkyCalc.Compass(azDeg)}";
        }
    }
}
