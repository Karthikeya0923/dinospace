using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace dinospace.Services
{
    // A user-made collection. Entries are stored as "d:Name" or "s:Name" so
    // one list can mix dinosaurs and space objects freely.
    public class CustomList
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public List<string> Entries { get; set; } = new();
    }

    // On-device storage for the user's own lists (JSON in preferences,
    // same as bookmarks — nothing ever leaves the phone).
    public static class CustomListStore
    {
        private const string Key = "customlists_v1";

        public static List<CustomList> All()
        {
            try
            {
                var json = Preferences.Get(Key, "");
                if (string.IsNullOrEmpty(json)) return new List<CustomList>();
                return JsonSerializer.Deserialize<List<CustomList>>(json) ?? new List<CustomList>();
            }
            catch { return new List<CustomList>(); }
        }

        public static CustomList Create(string title)
        {
            var lists = All();
            var list = new CustomList { Id = Guid.NewGuid().ToString("N"), Title = title.Trim() };
            lists.Add(list);
            Save(lists);
            return list;
        }

        public static void Update(CustomList list)
        {
            var lists = All();
            int i = lists.FindIndex(l => l.Id == list.Id);
            if (i >= 0) lists[i] = list; else lists.Add(list);
            Save(lists);
        }

        public static void Delete(string id) => Save(All().Where(l => l.Id != id).ToList());

        public static CustomList? Get(string id) => All().FirstOrDefault(l => l.Id == id);

        private static void Save(List<CustomList> lists)
        {
            try { Preferences.Set(Key, JsonSerializer.Serialize(lists)); } catch { }
        }

        // "d:Spinosaurus" -> the entry's display bits, or null if it was
        // removed from the encyclopedia. "c:<id>" resolves a user creation.
        public static (string image, string title, string meta, object data)? Resolve(string entry)
        {
            if (entry.StartsWith("d:"))
            {
                var d = DinoData.ByName(entry[2..]);
                return d == null ? null : (d.ImageFile, d.Name, d.Era, d);
            }
            if (entry.StartsWith("s:"))
            {
                var s = SpaceData.ByName(entry[2..]);
                return s == null ? null : (s.ImageFile, s.Name, s.TypeLabel, s);
            }
            if (entry.StartsWith("c:"))
            {
                var c = CreationStore.Get(entry[2..]);
                if (c == null) return null;
                object data = c.Kind == Models.CreationKind.Dinosaur ? c.ToDinosaur() : (object)c.ToSpaceObject();
                return (c.ImagePath, c.Name, c.MetaLine, data);
            }
            return null;
        }

        public static string KeyFor(object data) => data switch
        {
            Models.UserCreation c => "c:" + c.Id,
            Models.Dinosaur d => "d:" + d.Name,
            Models.SpaceObject s => "s:" + s.Name,
            _ => ""
        };
    }
}
