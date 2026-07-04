using System;
using System.Collections.Generic;
using System.Linq;

namespace dinospace.Data
{
    // A ranked row inside a curated collection.
    public class CollectionEntry
    {
        public string Image { get; init; } = "";
        public string Name { get; init; } = "";
        public string StatText { get; init; } = "";
        public object Data { get; init; } = null!;
        public bool IsSpace { get; init; }
    }

    public class Collection
    {
        public string Id { get; init; } = "";
        public string Title { get; init; } = "";
        public string Subtitle { get; init; } = "";
        public string Domain { get; init; } = "Dino"; // Dino / Space
    }

    // Curated, ranked lists that give people a reason to keep browsing.
    public static class CollectionData
    {
        public static readonly List<Collection> All = new()
        {
            new() { Id = "biggest", Title = "Biggest Creatures Ever", Subtitle = "Ranked by length, longest first", Domain = "Dino" },
            new() { Id = "speed", Title = "Speed Demons", Subtitle = "Fastest creatures, quickest first", Domain = "Dino" },
            new() { Id = "bite", Title = "Strongest Bites", Subtitle = "Ranked by bite force in PSI", Domain = "Dino" },
            new() { Id = "farthest", Title = "Farthest From Earth", Subtitle = "Every space object, nearest to farthest", Domain = "Space" },
            new() { Id = "cosmic", Title = "Cosmic Giants", Subtitle = "The largest objects in space", Domain = "Space" },
            new() { Id = "hottest", Title = "Hottest to Coldest", Subtitle = "Worlds ranked by temperature", Domain = "Space" },
        };

        // Every space entry ordered by distance from Earth (closest approach /
        // typical figures, kid-friendly rounding).
        private static readonly (string name, string distance)[] FarthestOrder =
        {
            ("Earth", "You are here!"),
            ("Milky Way", "We live inside it"),
            ("International Space Station", "400 km up"),
            ("Moon", "384,400 km"),
            ("Venus", "40 million km"),
            ("Mars", "56 million km"),
            ("Mercury", "77 million km"),
            ("Sun", "150 million km"),
            ("Asteroid Belt", "330 million km"),
            ("Jupiter", "588 million km"),
            ("Europa", "588 million km"),
            ("Saturn", "1.2 billion km"),
            ("Uranus", "2.6 billion km"),
            ("Neptune", "4.3 billion km"),
            ("Pluto", "5.7 billion km"),
            ("Halley's Comet", "up to 5.3 billion km"),
            ("Voyager 1", "24+ billion km"),
            ("Betelgeuse", "550 light-years"),
            ("Orion", "1,344 light-years"),
            ("Orion Nebula", "1,344 light-years"),
            ("Sagittarius A*", "26,000 light-years"),
            ("Andromeda Galaxy", "2.5 million light-years"),
            ("Phoenix A*", "5.8 billion light-years"),
        };

        // Approximate surface (or effective) temperatures for the ranking.
        private static readonly Dictionary<string, string> TempC = new()
        {
            ["Sun"] = "5,500°C", ["Venus"] = "465°C", ["Mercury"] = "430°C (day)",
            ["Earth"] = "15°C", ["Mars"] = "-60°C", ["Neptune"] = "-200°C", ["Pluto"] = "-230°C",
        };

        // Human-readable "size" for the cosmic giants ranking.
        private static readonly Dictionary<string, string> CosmicSize = new()
        {
            ["Phoenix A*"] = "590 billion km wide",
            ["Milky Way"] = "100,000 light-years wide",
            ["Andromeda Galaxy"] = "220,000 light-years wide",
            ["Betelgeuse"] = "700× the Sun's width",
            ["Sagittarius A*"] = "24 million km wide",
            ["Sun"] = "1.39 million km wide",
            ["Jupiter"] = "139,820 km wide",
            ["Saturn"] = "116,460 km wide",
        };

        public static Collection? ById(string id) => All.FirstOrDefault(c => c.Id == id);

        public static List<CollectionEntry> Entries(string id)
        {
            switch (id)
            {
                case "biggest":
                    return RankDino(d => Num(d.Length), desc: true, d => d.Length);
                case "speed":
                    return RankDino(d => Num(d.Speed), desc: true, d => d.Speed);
                case "bite":
                    return DinoData.All.Where(d => d.BiteForce.Length > 0)
                        .OrderByDescending(d => Num(d.BiteForce))
                        .Select(d => Entry(d, d.BiteForce)).ToList();
                case "farthest":
                    return FarthestOrder
                        .Select(x => (obj: SpaceData.ByName(x.name), x.distance))
                        .Where(x => x.obj != null)
                        .Select(x => EntryS(x.obj!, x.distance)).ToList();
                case "cosmic":
                    var big = new[] { "Phoenix A*", "Milky Way", "Andromeda Galaxy", "Betelgeuse", "Sagittarius A*", "Sun", "Jupiter", "Saturn" };
                    return big.Select(SpaceData.ByName).Where(s => s != null)
                        .Select(s => EntryS(s!, CosmicSize.GetValueOrDefault(s!.Name, s!.TypeLabel))).ToList();
                case "hottest":
                    var hot = new[] { "Sun", "Venus", "Mercury", "Earth", "Mars", "Neptune", "Pluto" };
                    return hot.Select(SpaceData.ByName).Where(s => s != null)
                        .Select(s => EntryS(s!, TempC.GetValueOrDefault(s!.Name, "—"))).ToList();
                default:
                    return new List<CollectionEntry>();
            }
        }

        private static List<CollectionEntry> RankDino(Func<Dinosaur, double> key, bool desc, Func<Dinosaur, string> stat)
        {
            var ordered = desc
                ? DinoData.All.OrderByDescending(key)
                : DinoData.All.OrderBy(key);
            return ordered.Take(15).Select(d => Entry(d, stat(d))).ToList();
        }

        private static CollectionEntry Entry(Dinosaur d, string stat) => new()
        { Image = d.ImageFile, Name = d.Name, StatText = stat, Data = d, IsSpace = false };

        private static CollectionEntry EntryS(SpaceObject s, string stat) => new()
        { Image = s.ImageFile, Name = s.Name, StatText = stat, Data = s, IsSpace = true };

        private static double Num(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var sb = new System.Text.StringBuilder();
            bool started = false;
            foreach (char c in s.Replace(",", ""))
            {
                if (char.IsDigit(c) || (c == '.' && started)) { sb.Append(c); started = true; }
                else if (started) break;
            }
            return sb.Length > 0 && double.TryParse(sb.ToString(), out var v) ? v : 0;
        }
    }
}
