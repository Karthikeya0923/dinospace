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
                if (await HasLocationPermission())
                {
                    var loc = await FullyGranted()
                        ? await Geolocation.Default.GetLastKnownLocationAsync()
                        : await GetCoarseLastKnown();
                    if (loc != null) return Save(loc);
                }
            }
            catch { }
            return Cached;
        }

        // "Approximate" on Android 12+ grants ONLY coarse location, and the
        // cross-platform check then reports the whole location group as not
        // granted — which used to read as a denial and threw the "No
        // location" alert at people who had just said yes. Coarse is all
        // this app ever wants (positions are rounded to 0.1° anyway), so
        // accept the raw coarse grant as a full yes.
        private static async Task<bool> HasLocationPermission()
        {
            if (await FullyGranted()) return true;
#if ANDROID
            try
            {
                var ctx = Android.App.Application.Context;
                return AndroidX.Core.Content.ContextCompat.CheckSelfPermission(
                    ctx, Android.Manifest.Permission.AccessCoarseLocation)
                    == Android.Content.PM.Permission.Granted;
            }
            catch { }
#endif
            return false;
        }

        // True only when the whole location group (precise included) is
        // granted — the case where the cross-platform Geolocation API works
        // without re-prompting.
        private static async Task<bool> FullyGranted()
        {
            try { return await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() == PermissionStatus.Granted; }
            catch { return false; }
        }

#if ANDROID
        // Coarse-only path: read the phone's location straight from Android's
        // LocationManager. Needs nothing beyond ACCESS_COARSE_LOCATION and
        // never shows a permission sheet.
        private static Task<Location?> GetCoarseLastKnown()
        {
            try
            {
                var lm = (Android.Locations.LocationManager?)Android.App.Application.Context
                    .GetSystemService(Android.Content.Context.LocationService);
                if (lm == null) return Task.FromResult<Location?>(null);
                // "fused" first: the system-wide cache other apps keep warm.
                // It answers instantly even the moment after permission is
                // granted — the bare network provider often has nothing yet,
                // because coarse-only apps get freshly-computed fixes only at
                // a throttled rate.
                foreach (var p in new[] { "fused",
                                          Android.Locations.LocationManager.NetworkProvider,
                                          Android.Locations.LocationManager.PassiveProvider,
                                          Android.Locations.LocationManager.GpsProvider })
                {
                    try
                    {
                        var l = lm.GetLastKnownLocation(p);
                        if (l != null) return Task.FromResult<Location?>(new Location(l.Latitude, l.Longitude));
                    }
                    catch { }
                }
            }
            catch { }
            return Task.FromResult<Location?>(null);
        }

        private sealed class OneShotListener : Java.Lang.Object, Android.Locations.ILocationListener
        {
            public TaskCompletionSource<Location?> Tcs { get; } = new();
            public void OnLocationChanged(Android.Locations.Location location)
                => Tcs.TrySetResult(new Location(location.Latitude, location.Longitude));
            public void OnProviderDisabled(string provider) => Tcs.TrySetResult(null);
            public void OnProviderEnabled(string provider) { }
            public void OnStatusChanged(string? provider, Android.Locations.Availability status, Android.OS.Bundle? extras) { }
        }

        private static async Task<Location?> GetCoarseLocationAsync(TimeSpan timeout)
        {
            var last = await GetCoarseLastKnown();
            if (last != null) return last;
            try
            {
                var lm = (Android.Locations.LocationManager?)Android.App.Application.Context
                    .GetSystemService(Android.Content.Context.LocationService);
                if (lm == null) return null;
                var listener = new OneShotListener();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
#pragma warning disable CA1422 // deprecated but universally available; fine for a one-shot coarse fix
                        lm.RequestSingleUpdate(Android.Locations.LocationManager.NetworkProvider,
                            listener, Android.OS.Looper.MainLooper);
#pragma warning restore CA1422
                    }
                    catch { listener.Tcs.TrySetResult(null); }
                });
                var done = await Task.WhenAny(listener.Tcs.Task, Task.Delay(timeout));
                try { lm.RemoveUpdates(listener); } catch { }
                if (done == listener.Tcs.Task && listener.Tcs.Task.Result != null)
                    return listener.Tcs.Task.Result;
                // The wait itself often warms the system cache even when the
                // one-shot request misses its throttled window — read it once
                // more before giving up.
                return await GetCoarseLastKnown();
            }
            catch { return null; }
        }
#else
        private static Task<Location?> GetCoarseLastKnown() => Task.FromResult<Location?>(null);
        private static Task<Location?> GetCoarseLocationAsync(TimeSpan timeout) => Task.FromResult<Location?>(null);
#endif

        // Ask for permission (only from an explicit user tap) and fetch a
        // coarse fix. Null when the user says no or nothing comes back.
        public static async Task<SkyLocation?> RequestDeviceLocationAsync()
        {
            try
            {
                if (!await HasLocationPermission())
                {
                    await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (!await HasLocationPermission()) return null;
                }

                // Last-known is instant when the phone has any recent fix; the
                // live request is the fallback. Medium accuracy with a 15s
                // window succeeds indoors far more often than a short Low
                // request, which used to time out to a "No location" alert on
                // the very first try after granting permission.
                //
                // With approximate-only permission the cross-platform
                // Geolocation calls re-request the permission group and pop
                // the system's "change to precise?" sheet EVERY time — so the
                // coarse path reads the phone's location natively instead,
                // which never prompts.
                Location? loc;
                if (await FullyGranted())
                {
                    loc = await Geolocation.Default.GetLastKnownLocationAsync();
                    loc ??= await Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(15)));
                }
                else
                    loc = await GetCoarseLocationAsync(TimeSpan.FromSeconds(20));
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

        // "9:03 p.m." — Canadian-style times.
        public static string FormatTime(DateTime t)
            => t.ToString("h:mm") + (t.Hour < 12 ? " a.m." : " p.m.");

        // "high in the southeast", "low in the west" ...
        public static string Describe(double altDeg, double azDeg)
        {
            if (altDeg > 65) return "almost straight overhead";
            string height = altDeg > 40 ? "high " : altDeg < 15 ? "low " : "";
            return $"{height}in the {SkyCalc.Compass(azDeg)}";
        }
    }
}
