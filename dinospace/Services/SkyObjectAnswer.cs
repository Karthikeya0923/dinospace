using System;
using System.Collections.Generic;
using System.Linq;
using dinospace.Data;

namespace dinospace.Services
{
    // NovaSaur's knowledge of everything Scan Sky can point at: every named
    // catalogue star, all 219 Messier + Caldwell deep-sky objects, and all 88
    // constellations. Each answer is a real description — a hand-written fact
    // for every named star, the catalogue's own story for every deep-sky
    // object, and a computed where-and-when for every constellation.
    public static class SkyObjectAnswer
    {
        private enum Kind { Star, DeepSky, Constellation }

        // normalized name -> (kind, index into its catalogue). Lazy<T> so the
        // first build is thread-safe. MaxKeyTokens caps the span search below.
        private static readonly Lazy<Dictionary<string, (Kind kind, int idx)>> _index = new(BuildIndex);
        private static int _maxKeyTokens = 1;

        public static string? TryAnswer(string q)
        {
            var index = _index.Value;

            // Longest name wins ("crab nebula" beats "crab"); token-bounded so
            // "leo" can't fire from inside another word. The question arrives
            // normalized, so every name in it is a contiguous run of tokens —
            // looking up each token span beats scanning the whole index.
            var tokens = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string? bestKey = null;
            (Kind kind, int idx) best = default;
            for (int len = Math.Min(_maxKeyTokens, tokens.Length); len >= 1; len--)
                for (int i = 0; i + len <= tokens.Length; i++)
                {
                    string span = len == 1 ? tokens[i] : string.Join(' ', tokens, i, len);
                    if (index.TryGetValue(span, out var v) && (bestKey == null || span.Length > bestKey.Length))
                    { bestKey = span; best = v; }
                }
            if (bestKey == null) return null;

            var (kind, idx) = best;
            return kind switch
            {
                Kind.Star => StarAnswer(SkyCatalog.Stars[idx], q),
                Kind.DeepSky => DeepSkyAnswer(SkyDeepSkyCatalog.All[idx]),
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
                int tokens = 1;
                foreach (char c in key) if (c == ' ') tokens++;
                if (tokens > _maxKeyTokens) _maxKeyTokens = tokens;
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
            string month = BestMonth(s.RaDeg / 15.0);

            string story = StarBlurbs.TryGetValue(Retriever.Normalize(s.Name), out var b)
                ? b
                : $"a {colour} star of the constellation {con}";

            string far = Has(q, "far", "distance", "away", "light year", "light years")
                ? " Like all stars beyond the Sun it is trillions of kilometres away — its light has been travelling for years before it reaches your eyes."
                : "";

            return $"{s.Name} is {story}. It shines at magnitude {s.Mag:0.0} with a {colour} glow, and it stands highest in the evening sky around {month}.{far}";
        }

        private static string DeepSkyAnswer(DeepSkyEntry d)
        {
            string baseName = d.Name.Split(" (")[0];
            int p = d.Name.IndexOf(" (");
            string alias = p > 0 ? $" — catalogued as {d.Name[(p + 2)..].TrimEnd(')')} —" : "";
            string blurb = d.Blurb.TrimEnd('.');
            string month = BestMonth(d.RaHours);
            string see = d.Mag switch
            {
                >= 90 => "It is a dark nebula: a cold dust cloud you spot as a black shape against the glowing band of the Milky Way.",
                < 4.5f => "Under a dark sky you can pick it out with just your eyes.",
                < 7f => "A pair of binoculars shows it well under a dark sky.",
                _ => "A small telescope and a dark night bring it out best.",
            };
            return $"The {baseName}{alias} is a {d.Kind.ToLowerInvariant()}: {blurb}. {see} It rides highest in the evening sky around {month}.";
        }

        private static string ConstellationAnswer(Constellation c)
        {
            string blurb = c.Blurb.TrimEnd('.');
            string month = BestMonth(c.RaHours);
            return $"{c.Name} is one of the 88 constellations astronomers use to map the whole sky. {blurb}. It climbs highest in the evening sky around {month}.";
        }

        // ---------- hand-written star stories ----------

        // One true, memorable fact for every named star in the catalogue,
        // keyed by normalized name.
        private static readonly Dictionary<string, string> StarBlurbs = BuildBlurbs();

        private static Dictionary<string, string> BuildBlurbs()
        {
            var raw = new Dictionary<string, string>
            {
                ["Sirius"] = "the brightest star in Earth's entire night sky — a white neighbour just 8.6 light-years away, circled by a tiny white-dwarf companion nicknamed the Pup",
                ["Canopus"] = "the second-brightest star in the night sky, a brilliant southern beacon so steady that spacecraft have used it to steer by",
                ["Arcturus"] = "an orange giant and the brightest star of the northern half of the sky — follow the arc of the Big Dipper's handle and you land right on it",
                ["Rigil Kentaurus"] = "the closest star system to our Sun, about 4.4 light-years away — a triple system whose faintest member, Proxima, is the Sun's nearest neighbour of all",
                ["Vega"] = "the sapphire anchor of the Summer Triangle, just 25 light-years away — astronomers long used it as the zero point their whole brightness scale was measured against",
                ["Capella"] = "the golden charioteer's star of Auriga, which looks like one star but is really two pairs of golden suns waltzing around each other",
                ["Rigel"] = "Orion's blazing blue-white foot: a supergiant tens of thousands of times more luminous than the Sun, shining from hundreds of light-years away",
                ["Procyon"] = "the Little Dog Star, only 11.5 light-years away, with a faint white-dwarf companion — it rises just before Sirius, as if announcing it",
                ["Achernar"] = "the star at the end of the river constellation Eridanus — it spins so fast it is one of the most squashed, flattened stars ever measured",
                ["Betelgeuse"] = "Orion's red supergiant shoulder — a dying star so huge it would swallow Mars if it replaced the Sun, and one day it will explode as a supernova",
                ["Hadar"] = "a brilliant blue southern star that, together with its neighbour Alpha Centauri, points straight at the Southern Cross",
                ["Altair"] = "the eagle's eye of Aquila and a corner of the Summer Triangle — it spins so fast that a day there lasts about nine hours and the star bulges at its middle",
                ["Aldebaran"] = "the fiery orange eye of Taurus the Bull, glaring out in front of the sparkling Hyades star cluster",
                ["Antares"] = "the red supergiant heart of Scorpius — its name means 'rival of Mars', because the two red points are so easy to mix up",
                ["Spica"] = "the blue jewel of Virgo: two hot stars orbiting so closely that they pull each other into egg shapes",
                ["Pollux"] = "the brighter of the Gemini twins, an orange giant 34 light-years away with a planet bigger than Jupiter circling it",
                ["Fomalhaut"] = "the 'lonely star of autumn' — the one bright star in a dim patch of sky, wearing a vast ring of dust that telescopes have photographed directly",
                ["Mimosa"] = "the second-brightest star of the Southern Cross, a hot blue star pulsing gently from hundreds of light-years away",
                ["Deneb"] = "the tail of Cygnus the swan and one of the most luminous stars we can see — it looks bright even though it lies roughly two thousand light-years away",
                ["Acrux"] = "the brightest star of the Southern Cross — through a telescope it splits into a dazzling family of blue stars",
                ["Regulus"] = "the heart of Leo the lion — it spins around in less than a day, whirling itself into a flattened shape",
                ["Adhara"] = "a scorching blue star in the Great Dog, and one of the brightest ultraviolet beacons in our whole sky",
                ["Gacrux"] = "the red giant at the top of the Southern Cross — the nearest red giant star to Earth, about 88 light-years away",
                ["Shaula"] = "the bright sting at the very tip of the scorpion's curled tail in Scorpius",
                ["Bellatrix"] = "Orion's other shoulder, nicknamed the Amazon Star — a hot blue-white star that will one day become a giant",
                ["Elnath"] = "the sharp tip of Taurus the Bull's northern horn, shared right on the border with the charioteer constellation Auriga",
                ["Miaplacidus"] = "the second-brightest star of Carina, the great ship's keel, whose lovely name may mean 'placid waters'",
                ["Alnilam"] = "the middle star of Orion's Belt — a blue supergiant so far away and so bright that it outshines almost everything between us and it",
                ["Alnair"] = "the brightest star of Grus, the elegant southern crane",
                ["Alioth"] = "the brightest star of the Big Dipper, third from the end of its famous handle",
                ["Mirfak"] = "the brightest star of Perseus, a yellow-white supergiant wrapped in a glittering cluster of young stars",
                ["Dubhe"] = "one of the two Pointer stars of the Big Dipper's bowl — draw a line through it and you arrive at the North Star",
                ["Wezen"] = "a far-off supergiant in the Great Dog — it looks modest, but it outshines the Sun tens of thousands of times",
                ["Kaus Australis"] = "the brightest star of Sagittarius, marking the base of the archer's bow — and the spout of the famous Teapot shape",
                ["Avior"] = "a bright star of Carina used by generations of navigators finding their way across the southern seas",
                ["Alkaid"] = "the very last star in the Big Dipper's handle — unlike its neighbours, it travels through space on its own path",
                ["Sargas"] = "a bright star deep in the scorpion's tail, low and splendid from southern skies",
                ["Menkalinan"] = "Auriga's second star — really a close pair of twin suns that eclipse each other as they spin",
                ["Atria"] = "the brightest star of the small, neat Southern Triangle",
                ["Alhena"] = "the bright star marking a foot of the Gemini twins",
                ["Peacock"] = "the brightest star of Pavo the peacock — it got its English name so pilots and navigators would have something easier to say than its catalogue number",
                ["Mirzam"] = "nicknamed 'the announcer', because it rises just ahead of dazzling Sirius and announces its arrival",
                ["Castor"] = "the other Gemini twin — point a telescope at it and one star becomes six, all bound together in a single system",
                ["Alphard"] = "'the solitary one' — the single bright heart of Hydra, alone in a huge dim stretch of sky",
                ["Hamal"] = "the brightest star of Aries the ram, an orange giant with a known planet",
                ["Polaris"] = "the North Star: it sits almost exactly above Earth's north pole, so it never seems to move and has guided travellers for centuries",
                ["Nunki"] = "a bright star in the handle of the Sagittarius Teapot, pointing toward the richest star fields of the Milky Way",
                ["Diphda"] = "the brightest star of Cetus the sea monster, glowing alone in the watery autumn sky",
                ["Alnitak"] = "the eastern star of Orion's Belt, hanging right beside the glowing Flame Nebula",
                ["Alpheratz"] = "a star with two jobs: it is the head of Andromeda and a corner of the Great Square of Pegasus at the same time",
                ["Mirach"] = "the middle star of Andromeda — star-hoppers use it as the signpost that leads to the Andromeda Galaxy",
                ["Saiph"] = "Orion's other knee — a hot supergiant that pours out most of its light in colours our eyes can't see",
                ["Menkent"] = "a bright orange giant in Centaurus, one of the handiest signposts of the far southern sky",
                ["Kochab"] = "the bright edge of the Little Dipper's bowl — thousands of years ago, before the sky slowly shifted, it served as the pole star",
                ["Rasalhague"] = "the head of Ophiuchus, the great serpent-bearer striding across summer evenings",
                ["Algol"] = "the famous Demon Star of Perseus: every few nights it visibly fades and brightens again, because a dimmer star crosses in front of it like clockwork",
                ["Mizar"] = "the middle star of the Big Dipper's handle — look closely and you'll spot its faint partner Alcor, an eyesight test used since ancient times",
                ["Alcyone"] = "the brightest star of the Pleiades, the little glittering cluster that looks like a tiny dipper of diamonds",
            };
            var map = new Dictionary<string, string>();
            foreach (var (name, blurb) in raw) map[Retriever.Normalize(name)] = blurb;
            return map;
        }

        // ---------- helpers ----------

        // The month when something at this right ascension stands highest in
        // the early-evening sky (~9 pm): the sky slides two hours of RA per
        // month, anchored at RA 9h culminating in late March evenings.
        private static string BestMonth(double raHours)
        {
            int monthsAfterMarch = (int)Math.Round(((raHours - 9 + 24) % 24) / 2.0) % 12;
            return new DateTime(2000, 3, 15).AddMonths(monthsAfterMarch).ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture);
        }

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
