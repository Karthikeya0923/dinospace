using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dinospace.Models;

namespace dinospace.Services
{
    // Ranking, ordering, counting and list questions, answered straight from
    // the encyclopedia: "top 5 strongest dinosaurs", "name the planets in
    // order", "top 3 biggest planets", "which planet has the most moons",
    // "how many dinosaurs do you know", "list some sea creatures".
    //
    // These are all just sorts and filters over data the app already has, so
    // they never need the model — they used to fall through to it and come
    // back wrong or not at all.
    public static class RankedAnswer
    {
        public static string? TryAnswer(string q)
        {
            string p = " " + q + " ";

            var counted = HowMany(q, p);
            if (counted != null) return counted;

            var order = PlanetsInOrder(q, p);
            if (order != null) return order;

            var dino = DinoRanking(q, p);
            if (dino != null) return dino;

            var space = SpaceRanking(q, p);
            if (space != null) return space;

            return PlainList(q, p);
        }

        // ---------- shared vocabulary ----------

        private static bool HasAny(string p, params string[] words)
            => words.Any(w => p.Contains(" " + w + " "));

        private static bool WantsDinos(string p)
            => HasAny(p, "dinosaur", "dinosaurs", "dino", "dinos", "creature", "creatures",
                         "animal", "animals", "predator", "predators", "carnivore", "carnivores",
                         "herbivore", "herbivores", "raptor", "raptors", "monster", "monsters")
               || p.Contains(" meat eater") || p.Contains(" plant eater");

        // How many the question asked for: a digit ("top 5"), a number word
        // ("five biggest"), or nothing (null — caller picks a default).
        private static int? WantedCount(string q)
        {
            string[] numberWords = { "one", "two", "three", "four", "five", "six", "seven",
                                     "eight", "nine", "ten", "eleven", "twelve" };
            foreach (var w in q.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(w, out int n) && n >= 1 && n <= 20) return n;
                int idx = Array.IndexOf(numberWords, w);
                if (idx >= 0) return idx + 1;
            }
            return null;
        }

        // A list is being asked for (not a single champion) when the question
        // says "top ...", asks to list/name/rank, or uses the plural subject.
        private static bool WantsList(string q, string p, bool pluralSubject)
            => p.Contains(" top ") || q.StartsWith("top ")
               || HasAny(p, "list", "name", "rank", "ranking", "order")
               || WantedCount(q) != null
               || pluralSubject;

        // ---------- counts ----------

        private static string? HowMany(string q, string p)
        {
            if (!q.Contains("how many")) return null;

            if (HasAny(p, "planet", "planets") && !WantsDinos(p))
                return "Our solar system has eight planets. In order from the Sun they are Mercury, " +
                       "Venus, Earth, Mars, Jupiter, Saturn, Uranus and Neptune. Pluto is now called a dwarf planet.";

            // "how many dinosaurs do you know / are in the app" — an inventory
            // question, not the science question "how many dinosaurs existed".
            if (HasAny(p, "dinosaur", "dinosaurs", "dino", "dinos", "creature", "creatures") &&
                HasAny(p, "know", "app", "encyclopedia", "encyclopaedia", "list", "have", "there"))
            {
                int land = DinoData.All.Count(d => d.Category == "Land");
                int sea = DinoData.All.Count(d => d.Category == "Sea");
                int fly = DinoData.All.Count(d => d.Category == "Flying");
                return $"My encyclopedia has {DinoData.All.Count} prehistoric creatures — {land} on land, " +
                       $"{sea} in the sea and {fly} in the air. Ask me about any of them, or open the Search tab to meet them all!";
            }

            return null;
        }

        // ---------- planets in order ----------

        private static readonly string[] PlanetOrder =
            { "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune" };

        private static string? PlanetsInOrder(string q, string p)
        {
            if (!HasAny(p, "planet", "planets")) return null;

            bool asksOrder = HasAny(p, "order", "sequence") || p.Contains(" in a row ");
            bool asksNames = HasAny(p, "name", "list", "all") && HasAny(p, "planets");
            if (!asksOrder && !asksNames) return null;

            // "planets in order of size" ranks by diameter, not distance.
            if (asksOrder && HasAny(p, "size", "biggest", "largest", "smallest", "big", "diameter"))
            {
                var sized = Planets().OrderByDescending(x => x.diameter).ToList();
                var sb = new StringBuilder("The planets in order of size, biggest first:\n");
                for (int i = 0; i < sized.Count; i++)
                    sb.Append($"{i + 1}. {sized[i].s.Name} — {LocalAnswer.Pretty(sized[i].s.Stat1Value)} across\n");
                sb.Append("Jupiter is so big that all the other planets could fit inside it!");
                return sb.ToString().Trim();
            }

            return "The eight planets in order from the Sun are Mercury, Venus, Earth, Mars, " +
                   "Jupiter, Saturn, Uranus and Neptune. A fun way to remember them: " +
                   "“My Very Easy Method Just Speeds Up Naming”. Pluto is now called a dwarf planet.";
        }

        private static List<(SpaceObject s, double diameter)> Planets()
        {
            var list = new List<(SpaceObject, double)>();
            foreach (var name in PlanetOrder)
            {
                var s = SpaceData.ByName(name);
                if (s == null) continue;
                double d = LocalAnswer.Num(s.Stat1Value);
                if (d > 0) list.Add((s, d));
            }
            return list;
        }

        // ---------- dinosaur rankings ----------

        // Groups that are true dinosaurs; the encyclopedia also holds sharks,
        // marine reptiles, pterosaurs and mammals, and "strongest dinosaur"
        // must not answer with a shark.
        private static readonly HashSet<string> DinosaurGroups = new(StringComparer.OrdinalIgnoreCase)
        {
            "Theropod", "Sauropod", "Raptor", "Ankylosaur", "Ceratopsian",
            "Hadrosaur", "Ornithomimid", "Ornithopod", "Pachycephalosaur", "Stegosaur"
        };

        private sealed record Metric(string Adjective, Func<Dinosaur, double> Value, bool Max, Func<Dinosaur, string> Show);

        private static string? DinoRanking(string q, string p)
        {
            if (!WantsDinos(p) && !(p.Contains(" top ") || q.StartsWith("top "))) return null;
            // Space words present and dino words absent -> not ours.
            if (!WantsDinos(p) && HasAny(p, "planet", "planets", "star", "stars", "galaxy", "galaxies", "moon", "moons"))
                return null;

            Metric? m = Pick(q, p);
            // "rank the dinosaurs by size" — no superlative word, but a clear
            // ranking intent plus a dimension.
            if (m == null && HasAny(p, "rank", "order", "sort") && HasAny(p, "size", "sized"))
                m = new Metric("biggest", d => LocalAnswer.Num(d.Length), true,
                               d => $"about {LocalAnswer.Pretty(d.Length)} long");
            if (m == null) return null;

            bool strictDino = HasAny(p, "dinosaur", "dinosaurs", "dino", "dinos");
            Func<Dinosaur, bool> filter = d => true;
            string noun = strictDino ? "dinosaurs" : "prehistoric creatures";
            if (HasAny(p, "carnivore", "carnivores", "predator", "predators") || p.Contains(" meat eat"))
            { filter = d => d.Diet.Contains("arnivore"); noun = "meat-eaters"; }
            else if (HasAny(p, "herbivore", "herbivores") || p.Contains(" plant eat"))
            { filter = d => d.Diet.Contains("erbivore"); noun = "plant-eaters"; }
            else if (HasAny(p, "sea", "ocean", "marine", "water", "swimming"))
            { filter = d => d.Category == "Sea"; strictDino = false; noun = "sea creatures"; }
            else if (HasAny(p, "flying", "flyers", "air", "pterosaur", "pterosaurs"))
            { filter = d => d.Category == "Flying"; strictDino = false; noun = "flying creatures"; }

            var pool = DinoData.All
                .Where(filter)
                .Where(d => !strictDino || DinosaurGroups.Contains(d.Group))
                .Where(d => m.Value(d) > 0)
                .OrderBy(d => m.Max ? -m.Value(d) : m.Value(d))
                .ToList();
            if (pool.Count == 0) return null;

            int? asked = WantedCount(q);
            bool plural = HasAny(p, "dinosaurs", "dinos", "creatures", "animals", "predators",
                                    "carnivores", "herbivores", "raptors", "monsters");
            if (!WantsList(q, p, plural) || asked == 1)
            {
                var champ = pool[0];
                string one = noun.TrimEnd('s');
                string runners = pool.Count >= 3 ? $" Right behind it come {pool[1].Name} and {pool[2].Name}." : "";
                return $"The {m.Adjective} {one} in my encyclopedia is {champ.Name} — {m.Show(champ)}.{runners}";
            }

            int n = Math.Min(asked ?? 5, Math.Min(10, pool.Count));
            var sb = new StringBuilder($"Here are the top {n} {m.Adjective} {noun} I know:\n");
            for (int i = 0; i < n; i++)
                sb.Append($"{i + 1}. {pool[i].Name} — {m.Show(pool[i])}\n");
            sb.Append("Want to see any of them fight? Try Dino Battle!");
            return sb.ToString().Trim();
        }

        private static Metric? Pick(string q, string p)
        {
            if (HasAny(p, "strongest", "most powerful", "toughest", "most dangerous", "deadliest", "scariest"))
                return new Metric("strongest", d => d.Strength, true, ShowPower);
            if (HasAny(p, "biggest", "largest", "longest"))
                return new Metric("biggest", d => LocalAnswer.Num(d.Length), true,
                                  d => $"about {LocalAnswer.Pretty(d.Length)} long");
            if (HasAny(p, "heaviest"))
                return new Metric("heaviest", d => LocalAnswer.Num(d.Weight), true,
                                  d => $"around {LocalAnswer.Pretty(d.Weight)}");
            if (HasAny(p, "tallest"))
                return new Metric("tallest", d => LocalAnswer.Num(d.Height), true,
                                  d => $"about {LocalAnswer.Pretty(d.Height)} tall");
            if (HasAny(p, "fastest", "quickest", "speediest"))
                return new Metric("fastest", d => LocalAnswer.Num(d.Speed), true,
                                  d => $"up to {LocalAnswer.Pretty(d.Speed)}");
            if (HasAny(p, "smallest", "tiniest", "littlest"))
                return new Metric("smallest", d => LocalAnswer.Num(d.Length), false,
                                  d => $"only about {LocalAnswer.Pretty(d.Length)} long");
            if (HasAny(p, "lightest"))
                return new Metric("lightest", d => LocalAnswer.Num(d.Weight), false,
                                  d => $"just {LocalAnswer.Pretty(d.Weight)}");
            if (HasAny(p, "slowest"))
                return new Metric("slowest", d => LocalAnswer.Num(d.Speed), false,
                                  d => $"only {LocalAnswer.Pretty(d.Speed)}");
            return null;
        }

        // Battle power shows the creature's best weapon, not the raw score.
        private static string ShowPower(Dinosaur d)
        {
            if (!string.IsNullOrEmpty(d.BiteForce)) return $"a bite of about {LocalAnswer.Pretty(d.BiteForce)}";
            if (!string.IsNullOrEmpty(d.Weight)) return $"around {LocalAnswer.Pretty(d.Weight)} of pure power";
            return $"about {LocalAnswer.Pretty(d.Length)} long";
        }

        // ---------- space rankings ----------

        private static string? SpaceRanking(string q, string p)
        {
            // "which planet has the most moons"
            if (HasAny(p, "most moons", "moons") && HasAny(p, "most", "how many") && HasAny(p, "planet", "planets", "which", "what"))
            {
                if (p.Contains(" most "))
                {
                    var byMoons = Planets()
                        .Select(x => (x.s, moons: MoonCount(x.s)))
                        .Where(x => x.moons > 0)
                        .OrderByDescending(x => x.moons)
                        .ToList();
                    if (byMoons.Count >= 2)
                        return $"{byMoons[0].s.Name} has the most moons — about {byMoons[0].moons:0}! " +
                               $"{byMoons[1].s.Name} comes second with around {byMoons[1].moons:0}. " +
                               "Astronomers keep finding new ones, so the count keeps growing.";
                }
            }

            // "what is the biggest moon" — the honest answer isn't in the
            // encyclopedia (Ganymede has no entry), so it's curated here.
            if (HasAny(p, "biggest", "largest") && HasAny(p, "moon", "moons") && !HasAny(p, "planet", "planets"))
                return "Ganymede, one of Jupiter's moons, is the biggest moon in the whole solar system — " +
                       "it's even bigger than the planet Mercury! Our own Moon is the fifth biggest.";

            bool planets = HasAny(p, "planet", "planets");
            bool galaxies = p.Contains(" galax");
            bool holes = p.Contains(" black hole");
            if (!planets && !galaxies && !holes) return null;

            bool wantBig = HasAny(p, "biggest", "largest");
            bool wantSmall = HasAny(p, "smallest", "tiniest", "littlest");
            if (!wantBig && !wantSmall) return null;

            if (galaxies || holes)
            {
                string type = galaxies ? "Galaxy" : "Black Hole";
                var pool = SpaceData.All.Where(s => s.TypeLabel == type)
                    .Select(s => (s, size: StatNumber(s, galaxies ? "diameter" : "mass")))
                    .Where(x => x.size > 0)
                    .OrderBy(x => wantBig ? -x.size : x.size).ToList();
                if (pool.Count == 0) return null;
                var top = pool[0].s;
                return $"The {(wantBig ? "biggest" : "smallest")} {type.ToLowerInvariant()} I know is {top.Name} — {LocalAnswer.FirstSentences(top.AboutText, 2)}";
            }

            var ranked = Planets().OrderBy(x => wantBig ? -x.diameter : x.diameter).ToList();
            if (ranked.Count == 0) return null;

            int? asked = WantedCount(q);
            bool plural = p.Contains(" planets ");
            if (!WantsList(q, p, plural) || asked == 1)
            {
                var c = ranked[0];
                string extra = wantBig
                    ? "It's so big that all the other planets could fit inside it!"
                    : "It's only a little bigger than our Moon.";
                return $"The {(wantBig ? "biggest" : "smallest")} planet is {c.s.Name}, about {LocalAnswer.Pretty(c.s.Stat1Value)} across. {extra}";
            }

            int n = Math.Min(asked ?? 3, ranked.Count);
            var sb = new StringBuilder($"The {n} {(wantBig ? "biggest" : "smallest")} planets:\n");
            for (int i = 0; i < n; i++)
                sb.Append($"{i + 1}. {ranked[i].s.Name} — about {LocalAnswer.Pretty(ranked[i].s.Stat1Value)} across\n");
            sb.Append(wantBig ? "Jupiter alone is heavier than all the other planets put together!"
                              : "All three are rocky worlds, much smaller than the gas giants.");
            return sb.ToString().Trim();
        }

        private static double MoonCount(SpaceObject s)
        {
            foreach (var (label, value) in Stats(s))
                if (label.Contains("moon", StringComparison.OrdinalIgnoreCase))
                    return LocalAnswer.Num(value);
            return 0;
        }

        private static double StatNumber(SpaceObject s, string labelKey)
        {
            foreach (var (label, value) in Stats(s))
            {
                if (!label.Contains(labelKey, StringComparison.OrdinalIgnoreCase)) continue;
                double v = LocalAnswer.Num(value);
                string low = value.ToLowerInvariant();
                if (low.Contains("trillion")) v *= 1e12;
                else if (low.Contains("billion")) v *= 1e9;
                else if (low.Contains("million")) v *= 1e6;
                return v;
            }
            return 0;
        }

        private static IEnumerable<(string label, string value)> Stats(SpaceObject s)
        {
            yield return (s.Stat1Label, s.Stat1Value);
            yield return (s.Stat2Label, s.Stat2Value);
            yield return (s.Stat3Label, s.Stat3Value);
            yield return (s.Stat4Label, s.Stat4Value);
        }

        // ---------- plain lists ----------

        // "name some dinosaurs", "list 5 sea creatures" — no stat to rank by,
        // so the best-known (highest battle power) lead the list.
        private static string? PlainList(string q, string p)
        {
            if (!HasAny(p, "name", "list", "some", "examples", "know"))
                return null;
            if (!HasAny(p, "dinosaurs", "dinos", "creatures", "animals", "carnivores", "herbivores"))
                return null;
            // A named entity means this is about ONE creature, not a list.
            if (HasAny(p, "mean", "means", "meaning")) return null;

            // "name some dinosaurs" must list actual dinosaurs, not the
            // sharks and marine reptiles that share the encyclopedia.
            Func<Dinosaur, bool> filter = d => DinosaurGroups.Contains(d.Group);
            string what = "dinosaurs";
            if (HasAny(p, "carnivores") || p.Contains(" meat eat")) { filter = d => d.Diet.Contains("arnivore"); what = "meat-eaters"; }
            else if (HasAny(p, "herbivores") || p.Contains(" plant eat")) { filter = d => d.Diet.Contains("erbivore"); what = "plant-eaters"; }
            else if (HasAny(p, "sea", "ocean", "marine")) { filter = d => d.Category == "Sea"; what = "sea creatures"; }
            else if (HasAny(p, "flying")) { filter = d => d.Category == "Flying"; what = "flying creatures"; }
            else if (HasAny(p, "creatures", "animals")) { filter = d => true; what = "prehistoric creatures"; }

            var pool = DinoData.All.Where(filter).OrderByDescending(d => d.Strength).ToList();
            if (pool.Count == 0) return null;

            int n = Math.Min(WantedCount(q) ?? 5, Math.Min(8, pool.Count));
            string names = string.Join(", ", pool.Take(n - 1).Select(d => d.Name)) +
                           (n > 1 ? " and " + pool[n - 1].Name : "");
            return $"Some {what} I know: {names}. Ask me about any of them!";
        }
    }
}
