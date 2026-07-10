using System;
using System.Collections.Generic;
using System.Linq;
using dinospace.Data;

namespace dinospace.Services
{
    // NovaSaur's knowledge of everything Scan Sky can point at: every named
    // catalogue star, all 219 Messier + Caldwell deep-sky objects, and all 88
    // constellations. Answers are templated from the exact same data the sky
    // view draws, so "Learn more" and "Ask NovaSaur" always land — tap Mirfak
    // and NovaSaur knows Mirfak.
    public static class SkyObjectAnswer
    {
        private enum Kind { Star, DeepSky, Constellation }

        // normalized name -> (kind, index into its catalogue)
        private static Dictionary<string, (Kind kind, int idx)>? _index;

        public static string? TryAnswer(string q)
        {
            var index = _index ??= BuildIndex();

            // Longest name wins ("crab nebula" beats "crab"); token-bounded so
            // "leo" can't fire from inside another word.
            string padded = " " + q + " ";
            string? bestKey = null;
            foreach (var key in index.Keys)
                if (padded.Contains(" " + key + " ") && (bestKey == null || key.Length > bestKey.Length))
                    bestKey = key;
            if (bestKey == null) return null;

            var (kind, idx) = index[bestKey];
            return kind switch
            {
                Kind.Star => StarAnswer(SkyCatalog.Stars[idx], q),
                Kind.DeepSky => DeepSkyAnswer(SkyDeepSkyCatalog.All[idx], q),
                _ => ConstellationAnswer(SkyData.All[idx]),
            };
        }

        private static Dictionary<string, (Kind, int)> BuildIndex()
        {
            var map = new Dictionary<string, (Kind, int)>();

            void Add(string name, Kind kind, int idx)
            {
                string key = Retriever.Normalize(name);
                // Two-letter M/C catalogue codes (M1…M9, C4…) are real names;
                // anything else that short is too risky to match on.
                bool catCode = key.Length == 2 && (key[0] == 'm' || key[0] == 'c') && char.IsDigit(key[1]);
                if (key.Length < 3 && !catCode) return;
                // The rich encyclopedia entry always wins (Betelgeuse, Orion,
                // the Milky Way…) — this index only covers what it doesn't.
                if (SpaceData.ByName(name) != null) return;
                if (!map.ContainsKey(key)) map[key] = (kind, idx);
            }

            var stars = SkyCatalog.Stars;
            for (int i = 0; i < stars.Length; i++)
                if (stars[i].Name.Length > 0)
                    Add(stars[i].Name, Kind.Star, i);

            var dsos = SkyDeepSkyCatalog.All;
            for (int i = 0; i < dsos.Length; i++)
            {
                string full = dsos[i].Name;                     // "Crab Nebula (M1)"
                int p = full.IndexOf(" (");
                if (p > 0)
                {
                    Add(full[..p], Kind.DeepSky, i);            // "Crab Nebula"
                    string code = full[(p + 2)..].TrimEnd(')'); // "M1"
                    Add(code, Kind.DeepSky, i);
                }
                else Add(full, Kind.DeepSky, i);
            }

            var cons = SkyData.All;
            for (int i = 0; i < cons.Count; i++)
                Add(cons[i].Name, Kind.Constellation, i);

            return map;
        }

        // ---------- the answers ----------

        private static string StarAnswer(CatStar s, string q)
        {
            string con = NearestConstellation(s.RaDeg, s.DecDeg);
            string colour = ColourWord(s.TempK);
            string bright = s.Mag switch
            {
                < 0.5f => "one of the very brightest stars in the whole night sky",
                < 1.6f => "among the brightest stars you can see",
                _ => "bright enough to spot with just your eyes on a clear night",
            };

            string far = Has(q, "far", "distance", "away", "light year", "light years")
                ? " It sits many light-years away — so far that the light hitting your eyes tonight left it years ago."
                : "";

            return $"{s.Name} is a {colour} star in the constellation {con} — {bright}, shining at magnitude {s.Mag:0.0}.{far} " +
                   $"Open Scan Sky and aim at {con}: the app will name {s.Name} the moment your crosshair touches it.";
        }

        private static string DeepSkyAnswer(DeepSkyEntry d, string q)
        {
            string baseName = d.Name.Split(" (")[0];
            int p = d.Name.IndexOf(" (");
            string alias = p > 0 ? $" (astronomers call it {d.Name[(p + 2)..].TrimEnd(')')})" : "";
            string blurb = d.Blurb.TrimEnd('.');
            string see = d.Mag switch
            {
                >= 90 => "It's a dark nebula — you spot it as a shadow against the starry band of the Milky Way, best from a really dark place.",
                < 4.5f => "It's bright enough to find with just your eyes under a dark sky.",
                < 7f => "A pair of binoculars will show it under a dark sky.",
                _ => "You'd want a small telescope and a dark night to see it well.",
            };
            return $"The {baseName}{alias} is a {d.Kind.ToLowerInvariant()}: {blurb}. {see} Scan Sky's view-all mode can show you where it hides.";
        }

        private static string ConstellationAnswer(Constellation c)
        {
            string blurb = c.Blurb.TrimEnd('.');
            return $"{c.Name} is one of the 88 constellations that tile the whole sky. {blurb}. " +
                   $"Open Scan Sky and sweep your phone around — the card will say \"{c.Name}\" the moment you're pointing into it.";
        }

        // ---------- helpers ----------

        private static string NearestConstellation(double raDeg, double decDeg)
        {
            string best = "the night sky"; double bestSep = double.MaxValue;
            double d1 = decDeg * Math.PI / 180.0, r1 = raDeg * Math.PI / 180.0;
            foreach (var c in SkyData.All)
            {
                double d2 = c.DecDeg * Math.PI / 180.0, r2 = c.RaHours * 15.0 * Math.PI / 180.0;
                double cosSep = Math.Sin(d1) * Math.Sin(d2) + Math.Cos(d1) * Math.Cos(d2) * Math.Cos(r1 - r2);
                double sep = Math.Acos(Math.Clamp(cosSep, -1, 1));
                if (sep < bestSep) { bestSep = sep; best = c.Name; }
            }
            return best;
        }

        private static string ColourWord(int tempK) => tempK switch
        {
            0 => "white",
            < 3700 => "red-orange",
            < 5000 => "orange",
            < 6000 => "yellow",
            < 7500 => "yellow-white",
            < 10000 => "white",
            _ => "blue-white",
        };

        private static bool Has(string q, params string[] words)
        {
            string p = " " + q + " ";
            return words.Any(w => w.Contains(' ') ? p.Contains(w) : p.Contains(" " + w + " "));
        }
    }
}
