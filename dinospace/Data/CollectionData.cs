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
            new() { Id = "biggest", Title = "Biggest Creatures Ever", Subtitle = "Every creature ranked by length", Domain = "Dino" },
            new() { Id = "speed", Title = "Speed Demons", Subtitle = "Every creature ranked by top speed", Domain = "Dino" },
            new() { Id = "bite", Title = "Strongest Bites", Subtitle = "Every creature ranked by bite force", Domain = "Dino" },
            new() { Id = "farthest", Title = "Farthest From Earth", Subtitle = "Every space object, nearest to farthest", Domain = "Space" },
            new() { Id = "cosmic", Title = "Cosmic Giants", Subtitle = "Every space object ranked by size", Domain = "Space" },
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

        // Every space entry ordered by physical size, biggest first.
        // New entries should be added here too or they fall to the end.
        private static readonly (string name, string size)[] CosmicSizeOrder =
        {
            ("Andromeda Galaxy", "220,000 light-years wide"),
            ("Milky Way", "100,000 light-years wide"),
            ("Orion", "spans ~1,100 light-years of sky"),
            ("Orion Nebula", "24 light-years wide"),
            ("Phoenix A*", "590 billion km wide"),
            ("Betelgeuse", "975 million km wide"),
            ("Asteroid Belt", "a ring ~150 million km thick"),
            ("Sagittarius A*", "24 million km wide"),
            ("Sun", "1.39 million km wide"),
            ("Jupiter", "139,820 km wide"),
            ("Saturn", "116,460 km wide"),
            ("Uranus", "50,724 km wide"),
            ("Neptune", "49,244 km wide"),
            ("Earth", "12,750 km wide"),
            ("Venus", "12,104 km wide"),
            ("Mars", "6,779 km wide"),
            ("Mercury", "4,879 km wide"),
            ("Moon", "3,475 km wide"),
            ("Europa", "3,122 km wide"),
            ("Pluto", "2,377 km wide"),
            ("Halley's Comet", "nucleus 15 km wide"),
            ("International Space Station", "109 m wide"),
            ("Voyager 1", "about 4 m wide"),
        };

        public static Collection? ById(string id) => All.FirstOrDefault(c => c.Id == id);

        public static List<CollectionEntry> Entries(string id)
        {
            switch (id)
            {
                case "biggest":
                    // Every creature, longest first.
                    return DinoData.All.OrderByDescending(d => Num(d.Length))
                        .Select(d => Entry(d, d.Length)).ToList();
                case "speed":
                    // Every creature, fastest first.
                    return DinoData.All.OrderByDescending(d => Num(d.Speed))
                        .Select(d => Entry(d, d.Speed)).ToList();
                case "bite":
                    // Every creature, strongest bite first (all have a PSI value).
                    return DinoData.All.OrderByDescending(d => Num(d.BiteForce))
                        .Select(d => Entry(d, string.IsNullOrEmpty(d.BiteForce) ? "—" : d.BiteForce)).ToList();
                case "farthest":
                    return Ordered(FarthestOrder);
                case "cosmic":
                    return Ordered(CosmicSizeOrder);
                default:
                    return new List<CollectionEntry>();
            }
        }

        // Builds a space list from an explicit ordering, then appends any
        // space object the list forgot so nothing is ever missing.
        private static List<CollectionEntry> Ordered((string name, string stat)[] order)
        {
            var result = new List<CollectionEntry>();
            var used = new HashSet<string>();
            foreach (var (name, stat) in order)
            {
                var s = SpaceData.ByName(name);
                if (s == null) continue;
                result.Add(EntryS(s, stat));
                used.Add(name);
            }
            foreach (var s in SpaceData.All)
                if (!used.Contains(s.Name))
                    result.Add(EntryS(s, s.TypeLabel));
            return result;
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
