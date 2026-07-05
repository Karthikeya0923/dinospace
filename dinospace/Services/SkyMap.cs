using System;
using System.Collections.Generic;
using System.Linq;

namespace dinospace
{
    // The sky-map engine behind Sky View: bright stars, constellation line
    // figures, and deep-sky highlights, plus a stereographic projector that
    // maps any of them to screen coordinates for whatever direction the
    // phone is pointing. Same math as the SkyScanner chart engine.
    public static class SkyMap
    {
        private const double Deg = Math.PI / 180.0;

        public sealed record MapStar(string Name, double RaHours, double DecDeg, double Mag, string Colour);
        public sealed record MapFigure(string Name, (double ra, double dec)[] Stars, (int a, int b)[] Lines);
        public sealed record DeepSkyObject(string Name, string Kind, double RaHours, double DecDeg, string Blurb);

        // The 24 stars worth naming on a phone screen.
        public static readonly MapStar[] Stars =
        {
            new("Sirius", 6.752, -16.72, -1.46, "blue-white"),
            new("Canopus", 6.399, -52.70, -0.74, "white"),
            new("Alpha Centauri", 14.660, -60.83, -0.27, "yellow"),
            new("Arcturus", 14.261, 19.18, -0.05, "orange"),
            new("Vega", 18.616, 38.78, 0.03, "blue-white"),
            new("Capella", 5.278, 45.99, 0.08, "golden"),
            new("Rigel", 5.242, -8.20, 0.13, "blue-white"),
            new("Procyon", 7.655, 5.22, 0.34, "white"),
            new("Achernar", 1.629, -57.24, 0.46, "blue"),
            new("Betelgeuse", 5.919, 7.41, 0.50, "red-orange"),
            new("Hadar", 14.064, -60.37, 0.61, "blue"),
            new("Altair", 19.846, 8.87, 0.76, "white"),
            new("Acrux", 12.443, -63.10, 0.76, "blue"),
            new("Aldebaran", 4.599, 16.51, 0.86, "orange"),
            new("Antares", 16.490, -26.43, 0.96, "red"),
            new("Spica", 13.420, -11.16, 0.97, "blue-white"),
            new("Pollux", 7.755, 28.03, 1.14, "orange"),
            new("Fomalhaut", 22.961, -29.62, 1.16, "white"),
            new("Deneb", 20.690, 45.28, 1.25, "white"),
            new("Mimosa", 12.795, -59.69, 1.25, "blue"),
            new("Regulus", 10.139, 11.97, 1.35, "blue-white"),
            new("Adhara", 6.977, -28.97, 1.50, "blue"),
            new("Castor", 7.577, 31.89, 1.58, "white"),
            new("Polaris", 2.530, 89.26, 1.98, "yellow-white"),
        };

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
        public static readonly DeepSkyObject[] DeepSky =
        {
            new("Orion Nebula (M42)", "nebula", 5.588, -5.39, "A stellar nursery you can spot with binoculars, below Orion's belt"),
            new("Pleiades (M45)", "star cluster", 3.790, 24.12, "The Seven Sisters — a sparkling little cluster even city eyes can find"),
            new("Andromeda Galaxy (M31)", "galaxy", 0.712, 41.27, "The farthest thing visible to the naked eye — 2.5 million light-years"),
            new("Hercules Cluster (M13)", "star cluster", 16.695, 36.46, "A snowball of 300,000 ancient stars"),
            new("Beehive Cluster (M44)", "star cluster", 8.670, 19.98, "A hive of stars hiding in faint Cancer"),
            new("Lagoon Nebula (M8)", "nebula", 18.060, -24.38, "A glowing cloud in the heart of the Milky Way"),
            new("Ring Nebula (M57)", "nebula", 18.893, 33.03, "A dying star's smoke ring, next to Vega"),
            new("Double Cluster", "star cluster", 2.337, 57.14, "Two clusters in one binocular view, between Perseus and Cassiopeia"),
            new("Sagittarius Star Cloud", "star cloud", 18.290, -18.51, "The densest, richest star field you can sweep with binoculars"),
            new("Albireo", "double star", 19.512, 27.96, "One gold star, one sapphire — the sky's prettiest pair"),
        };

        // ---------- projection (stereographic, like a planisphere) ----------

        public sealed record View(double Lat, double Lon, DateTime Utc, double CenterAz, double CenterAlt, double FovDeg, float SizePx);

        public static (float x, float y, bool visible) Project(double altDeg, double azDeg, View v)
        {
            var p = ToVector(altDeg, azDeg);
            var f = ToVector(v.CenterAlt, v.CenterAz);
            var right = Cross(f, (0, 0, 1));
            double rl = Len(right);
            if (rl < 1e-6) { right = (1, 0, 0); rl = 1; }
            right = (right.x / rl, right.y / rl, right.z / rl);
            var up = Cross(right, f);

            double zf = Dot(p, f);
            if (zf <= 0.02) return (0, 0, false);

            double k = 2.0 / (1.0 + zf);
            double px = k * Dot(p, right), py = k * Dot(p, up);
            double maxR = 2.0 * Math.Tan(v.FovDeg * Deg / 4.0);
            double scale = (v.SizePx / 2.0) / maxR;
            return ((float)(v.SizePx / 2.0 + px * scale), (float)(v.SizePx / 2.0 - py * scale),
                    Math.Sqrt(px * px + py * py) <= maxR * 1.1);
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
