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
            new() { Id = "dangerous", Title = "Most Dangerous", Subtitle = "The deadliest hunters that ever lived", Domain = "Dino" },
            new() { Id = "speed", Title = "Speed Demons", Subtitle = "Fastest creatures, quickest first", Domain = "Dino" },
            new() { Id = "bite", Title = "Strongest Bites", Subtitle = "Ranked by bite force in PSI", Domain = "Dino" },
            new() { Id = "sea", Title = "Rulers of the Deep", Subtitle = "Giants of the ancient oceans", Domain = "Dino" },
            new() { Id = "cretaceous", Title = "Neighbours of the Cretaceous", Subtitle = "Creatures that shared the same world", Domain = "Dino" },
            new() { Id = "planets", Title = "A Journey From the Sun", Subtitle = "The eight planets in order", Domain = "Space" },
            new() { Id = "cosmic", Title = "Cosmic Giants", Subtitle = "The largest objects in space", Domain = "Space" },
            new() { Id = "hottest", Title = "Hottest to Coldest", Subtitle = "Worlds ranked by temperature", Domain = "Space" },
        };

        public static Collection? ById(string id) => All.FirstOrDefault(c => c.Id == id);

        public static List<CollectionEntry> Entries(string id)
        {
            switch (id)
            {
                case "biggest":
                    return RankDino(d => Num(d.Length), desc: true, d => d.Length);
                case "dangerous":
                    return RankDino(d => d.Strength, desc: true, d => $"Danger {d.Strength}/100");
                case "speed":
                    return RankDino(d => Num(d.Speed), desc: true, d => d.Speed);
                case "bite":
                    return DinoData.All.Where(d => d.BiteForce.Length > 0)
                        .OrderByDescending(d => Num(d.BiteForce))
                        .Select(d => Entry(d, d.BiteForce)).ToList();
                case "sea":
                    return DinoData.All.Where(d => d.Category == "Sea")
                        .OrderByDescending(d => Num(d.Length))
                        .Select(d => Entry(d, d.Length)).ToList();
                case "cretaceous":
                    return DinoData.All.Where(d => d.Era.Contains("Cretaceous"))
                        .OrderBy(d => d.Name)
                        .Select(d => Entry(d, d.Era)).ToList();
                case "planets":
                    var order = new[] { "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune" };
                    return order.Select(SpaceData.ByName).Where(s => s != null)
                        .Select(s => EntryS(s!, s!.Stat2Value)).ToList();
                case "cosmic":
                    var big = new[] { "Phoenix A*", "Sagittarius A*", "Milky Way", "Andromeda Galaxy", "Betelgeuse", "Sun", "Jupiter", "Saturn" };
                    return big.Select(SpaceData.ByName).Where(s => s != null)
                        .Select(s => EntryS(s!, s!.TypeLabel)).ToList();
                case "hottest":
                    var hot = new[] { "Sun", "Venus", "Mercury", "Earth", "Mars", "Neptune", "Pluto" };
                    return hot.Select(SpaceData.ByName).Where(s => s != null)
                        .Select(s => EntryS(s!, s!.TypeLabel)).ToList();
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
