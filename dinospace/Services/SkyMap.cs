using System;
using System.Collections.Generic;
using System.Linq;

namespace dinospace
{
    // The sky-map engine behind Sky View: the full naked-eye star catalogue,
    // constellation line figures, the Milky Way band, deep-sky highlights, and
    // a stereographic projector that maps any of them to screen coordinates
    // for whatever direction the phone is pointing. Positions are J2000;
    // conversion to tonight's sky happens through SkyCalc's sidereal time.
    public static class SkyMap
    {
        private const double Deg = Math.PI / 180.0;

        public sealed record MapStar(string Name, double RaHours, double DecDeg, double Mag, string Colour);
        public sealed record MapFigure(string Name, (double ra, double dec)[] Stars, (int a, int b)[] Lines);
        public sealed record DeepSkyObject(string Name, string Kind, double RaHours, double DecDeg, string Blurb, bool NakedEye = false);

        // Stick figures for the constellations people actually recognise.
        public static readonly MapFigure[] Figures =
        {
            new("Orion",
                new[] { (5.919, 7.41), (5.418, 6.35), (5.679, -1.94), (5.603, -1.20), (5.533, -0.30), (5.796, -9.67), (5.242, -8.20) },
                new[] { (0, 2), (1, 4), (2, 3), (3, 4), (2, 5), (4, 6), (5, 6), (0, 1) }),
            new("Big Dipper",
                new[] { (11.062, 61.75), (11.031, 56.38), (11.897, 53.69), (12.257, 57.03), (12.900, 55.96), (13.399, 54.93), (13.792, 49.31) },
                new[] { (0, 1), (1, 2), (2, 3), (3, 0), (3, 4), (4, 5), (5, 6) }),
            new("Cassiopeia",
                new[] { (0.153, 59.15), (0.675, 56.54), (0.945, 60.72), (1.430, 60.24), (1.907, 63.67) },
                new[] { (0, 1), (1, 2), (2, 3), (3, 4) }),
            new("Cygnus",
                new[] { (20.690, 45.28), (20.371, 40.26), (19.512, 27.96), (19.749, 45.13), (20.770, 33.97) },
                new[] { (0, 1), (1, 2), (3, 1), (1, 4) }),
            new("Lyra",
                new[] { (18.616, 38.78), (18.746, 37.61), (18.834, 33.36), (18.982, 32.69), (18.908, 36.90) },
                new[] { (0, 1), (1, 2), (2, 3), (3, 4), (4, 1) }),
            new("Scorpius",
                new[] { (16.090, -19.81), (16.005, -22.62), (15.980, -26.11), (16.490, -26.43), (16.598, -28.22), (16.836, -34.29), (17.202, -43.24), (17.560, -37.10) },
                new[] { (0, 1), (1, 2), (1, 3), (3, 4), (4, 5), (5, 6), (6, 7) }),
            new("Leo",
                new[] { (10.139, 11.97), (10.122, 16.76), (10.333, 19.84), (10.278, 23.42), (9.879, 26.01), (9.764, 23.77), (11.237, 15.43), (11.818, 14.57) },
                new[] { (0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (2, 6), (6, 7), (0, 6) }),
            new("Gemini",
                new[] { (7.577, 31.89), (7.755, 28.03), (6.629, 16.40), (7.335, 21.98), (6.732, 25.13), (7.069, 20.57) },
                new[] { (0, 1), (0, 4), (4, 2), (1, 3), (3, 5) }),
            new("Taurus",
                new[] { (4.599, 16.51), (4.477, 15.87), (4.330, 15.63), (4.011, 12.49), (4.477, 19.18), (5.438, 28.61), (5.627, 21.14) },
                new[] { (3, 2), (2, 1), (1, 0), (2, 4), (4, 5), (0, 6) }),
            new("Canis Major",
                new[] { (6.752, -16.72), (6.378, -17.96), (6.977, -28.97), (7.140, -26.39), (7.402, -29.30) },
                new[] { (0, 1), (0, 3), (3, 2), (3, 4) }),
            new("Aquila",
                new[] { (19.846, 8.87), (19.771, 10.61), (19.921, 6.41), (19.090, 13.86), (20.188, -0.82) },
                new[] { (1, 0), (0, 2), (0, 3), (0, 4) }),
            new("Southern Cross",
                new[] { (12.443, -63.10), (12.519, -57.11), (12.795, -59.69), (12.252, -58.75) },
                new[] { (0, 1), (2, 3) }),
            new("Great Square of Pegasus",
                new[] { (23.079, 15.21), (23.063, 28.08), (0.220, 29.09), (0.139, 15.18) },
                new[] { (0, 1), (1, 2), (2, 3), (3, 0) }),
        };

        // Showpiece deep-sky objects for the "through a telescope" card.
        // (Scan Sky itself draws the full Messier + Caldwell catalogues.)
        public static readonly DeepSkyObject[] DeepSky =
        {
            new("Orion Nebula (M42)", "nebula", 5.588, -5.39, "A stellar nursery you can spot below Orion's belt", NakedEye: true),
            new("Pleiades (M45)", "star cluster", 3.790, 24.12, "The Seven Sisters — a sparkling little cluster even city eyes can find", NakedEye: true),
            new("Andromeda Galaxy (M31)", "galaxy", 0.712, 41.27, "The farthest thing visible to the naked eye — 2.5 million light-years", NakedEye: true),
            new("Hercules Cluster (M13)", "star cluster", 16.695, 36.46, "A snowball of 300,000 ancient stars"),
            new("Beehive Cluster (M44)", "star cluster", 8.670, 19.98, "A hive of stars hiding in faint Cancer"),
            new("Lagoon Nebula (M8)", "nebula", 18.060, -24.38, "A glowing cloud in the heart of the Milky Way"),
            new("Ring Nebula (M57)", "nebula", 18.893, 33.03, "A dying star's smoke ring, next to Vega"),
            new("Double Cluster", "star cluster", 2.337, 57.14, "Two clusters in one binocular view, between Perseus and Cassiopeia"),
            new("Sagittarius Star Cloud", "star cloud", 18.290, -18.51, "The densest, richest star field you can sweep with binoculars"),
            new("Albireo", "double star", 19.512, 27.96, "One gold star, one sapphire — the sky's prettiest pair"),
        };

        // ---------- fast per-frame star math ----------
        // Every catalogue star's J2000 unit vector is computed once; each
        // frame builds one local frame (zenith / north / east in equatorial
        // coordinates) and reduces every star to three dot products — no
        // trig per star, which is what lets 1,700 stars redraw at 20 fps.

        public readonly struct LocalFrame
        {
            public readonly double Nx, Ny, Nz;   // north
            public readonly double Ex, Ey, Ez;   // east
            public readonly double Zx, Zy, Zz;   // zenith

            public LocalFrame(double lat, double lon, DateTime utc)
            {
                double theta = (SkyCalc.Gmst(SkyCalc.JulianDay(utc)) + lon) * Deg;   // LST as angle
                double phi = lat * Deg;
                double ct = Math.Cos(theta), st = Math.Sin(theta);
                double cp = Math.Cos(phi), sp = Math.Sin(phi);
                Zx = cp * ct; Zy = cp * st; Zz = sp;
                Nx = -sp * ct; Ny = -sp * st; Nz = cp;
                Ex = -st; Ey = ct; Ez = 0;
            }

            // Horizon components (north, east, up) of a J2000 unit vector.
            public (double n, double e, double u) Horizon(double vx, double vy, double vz)
                => (vx * Nx + vy * Ny + vz * Nz,
                    vx * Ex + vy * Ey + vz * Ez,
                    vx * Zx + vy * Zy + vz * Zz);
        }

        public static (double x, double y, double z) UnitVectorOf(double raDeg, double decDeg)
        {
            double ra = raDeg * Deg, dec = decDeg * Deg;
            double cd = Math.Cos(dec);
            return (cd * Math.Cos(ra), cd * Math.Sin(ra), Math.Sin(dec));
        }

        // Catalogue star unit vectors, built once on first use.
        private static double[]? _starVec;
        public static double[] StarVectors
        {
            get
            {
                if (_starVec == null)
                {
                    var stars = SkyCatalog.Stars;
                    var v = new double[stars.Length * 3];
                    for (int i = 0; i < stars.Length; i++)
                    {
                        var (x, y, z) = UnitVectorOf(stars[i].RaDeg, stars[i].DecDeg);
                        v[i * 3] = x; v[i * 3 + 1] = y; v[i * 3 + 2] = z;
                    }
                    _starVec = v;
                }
                return _starVec;
            }
        }

        // ---------- the Milky Way band ----------
        // Sampled along the galactic equator (IAU J2000 frame) at three
        // galactic latitudes, with a brightness profile that peaks toward the
        // galactic centre in Sagittarius and glows again through Cygnus and
        // Carina — so the drawn band brightens exactly where the real one does.

        public readonly record struct BandPoint(double X, double Y, double Z, float Brightness, float WidthDeg);

        private static BandPoint[]? _milkyWay;
        public static BandPoint[] MilkyWayBand
        {
            get
            {
                if (_milkyWay == null)
                {
                    var pts = new List<BandPoint>();
                    foreach (double b in new[] { -5.5, 0.0, 5.5 })
                        for (double l = 0; l < 360; l += 3)
                        {
                            var (ra, dec) = GalacticToEquatorial(l, b);
                            var (x, y, z) = UnitVectorOf(ra, dec);
                            float bright = (float)BandBrightness(l) * (b == 0 ? 1f : 0.55f);
                            float width = (float)(5.5 + 5.0 * BandBrightness(l));
                            pts.Add(new BandPoint(x, y, z, bright, width));
                        }
                    _milkyWay = pts.ToArray();
                }
                return _milkyWay;
            }
        }

        private const double PoleRa = 192.85948, PoleDec = 27.12825, AscNode = 122.93192;

        public static (double raDeg, double decDeg) GalacticToEquatorial(double lDeg, double bDeg)
        {
            double b = bDeg * Deg, pd = PoleDec * Deg;
            double node = (AscNode - lDeg) * Deg;
            double sinDec = Math.Sin(b) * Math.Sin(pd) + Math.Cos(b) * Math.Cos(pd) * Math.Cos(node);
            double dec = Math.Asin(Math.Clamp(sinDec, -1, 1));
            double y = Math.Cos(b) * Math.Sin(node);
            double x = Math.Sin(b) * Math.Cos(pd) - Math.Cos(b) * Math.Sin(pd) * Math.Cos(node);
            double ra = PoleRa * Deg + Math.Atan2(y, x);
            double raDeg = ra / Deg; raDeg %= 360; if (raDeg < 0) raDeg += 360;
            return (raDeg, dec / Deg);
        }

        public static double BandBrightness(double lDeg)
        {
            double l = ((lDeg % 360) + 360) % 360;
            double toCentre = Math.Min(l, 360 - l);
            double core = Math.Exp(-toCentre * toCentre / (2 * 55.0 * 55.0));
            double cygnus = 0.25 * Math.Exp(-(l - 80) * (l - 80) / (2 * 18.0 * 18.0));
            double carina = 0.22 * Math.Exp(-(l - 287) * (l - 287) / (2 * 15.0 * 15.0));
            return Math.Min(1.0, 0.35 + 0.65 * core + cygnus + carina);
        }

        // ---------- projection (stereographic, like a planisphere) ----------

        public sealed record View(double Lat, double Lon, DateTime Utc, double CenterAz, double CenterAlt, double FovDeg, float SizePx,
            float CxPx = -1, float CyPx = -1)
        {
            // Cached basis so projecting a point is pure arithmetic.
            internal readonly (double x, double y, double z) F = ToVector(CenterAlt, CenterAz);
            internal (double x, double y, double z) Right, Up;
            internal double MaxR => 2.0 * Math.Tan(FovDeg * Deg / 4.0);
            // Where the pointing direction lands on screen. Defaults to the
            // square centre for callers that render a square; the AR view
            // passes its true rectangle centre so the crosshair, the drawn
            // sky, and the target card all agree on ONE centre.
            internal float CX => CxPx >= 0 ? CxPx : SizePx / 2f;
            internal float CY => CyPx >= 0 ? CyPx : SizePx / 2f;
        }

        public static (float x, float y, bool visible) Project(double altDeg, double azDeg, View v)
        {
            var p = ToVector(altDeg, azDeg);
            return ProjectVector(p.x, p.y, p.z, v);
        }

        // Projects a unit vector already expressed in the horizon frame
        // (x = north, y = east, z = up).
        public static (float x, float y, bool visible) ProjectVector(double px, double py, double pz, View v)
        {
            var f = v.F;
            if (v.Right.x == 0 && v.Right.y == 0 && v.Right.z == 0)
            {
                // Screen right = worldUp × forward: east when facing north.
                // (forward × worldUp pointed WEST, which mirrored the whole
                // sky east-to-west — the moon drew opposite its real spot.)
                var right = Cross((0, 0, 1), f);
                double rl = Len(right);
                if (rl < 1e-6) { right = (0, 1, 0); rl = 1; }
                v.Right = (right.x / rl, right.y / rl, right.z / rl);
                v.Up = Cross(f, v.Right);
            }

            double zf = px * f.x + py * f.y + pz * f.z;
            if (zf <= 0.02) return (0, 0, false);

            double k = 2.0 / (1.0 + zf);
            double sx = k * (px * v.Right.x + py * v.Right.y + pz * v.Right.z);
            double sy = k * (px * v.Up.x + py * v.Up.y + pz * v.Up.z);
            double maxR = v.MaxR;
            double scale = (v.SizePx / 2.0) / maxR;
            return ((float)(v.CX + sx * scale), (float)(v.CY - sy * scale),
                    Math.Sqrt(sx * sx + sy * sy) <= maxR * 1.1);
        }

        // Angular distance in degrees between two sky directions.
        public static double Separation(double alt1, double az1, double alt2, double az2)
            => Math.Acos(Math.Clamp(Dot(ToVector(alt1, az1), ToVector(alt2, az2)), -1, 1)) / Deg;

        // The constellation figure whose centre is closest to the view centre
        // (for the "you're looking at ..." label). Null when nothing is near.
        public static string? NearestFigure(double lat, double lon, DateTime utc, double centerAlt, double centerAz, double maxDeg = 30)
        {
            string? best = null;
            double bestSep = maxDeg;
            foreach (var f in Figures)
            {
                double ra = f.Stars.Average(s => s.ra) * 15.0;
                double dec = f.Stars.Average(s => s.dec);
                var (alt, az) = SkyCalc.AltAz(ra, dec, lat, lon, utc);
                if (alt < -10) continue;
                double sep = Separation(alt, az, centerAlt, centerAz);
                if (sep < bestSep) { bestSep = sep; best = f.Name; }
            }
            return best;
        }

        private static (double x, double y, double z) ToVector(double altDeg, double azDeg)
        {
            double alt = altDeg * Deg, az = azDeg * Deg;
            return (Math.Cos(alt) * Math.Cos(az), Math.Cos(alt) * Math.Sin(az), Math.Sin(alt));
        }
        private static double Dot((double x, double y, double z) a, (double x, double y, double z) b) => a.x * b.x + a.y * b.y + a.z * b.z;
        private static (double x, double y, double z) Cross((double x, double y, double z) a, (double x, double y, double z) b)
            => (a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        private static double Len((double x, double y, double z) v) => Math.Sqrt(Dot(v, v));
    }
}
