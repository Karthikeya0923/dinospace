using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Storage;
using dinospace.Models;

namespace dinospace.Services
{
    // On-device storage for the user's own drawn creations. The descriptions
    // live in preferences (JSON); each drawing is a PNG file in app data. Like
    // everything else in DinoSpace, nothing ever leaves the phone.
    public static class CreationStore
    {
        private const string Key = "creations_v1";

        private static string ImageDir
        {
            get
            {
                var dir = Path.Combine(FileSystem.AppDataDirectory, "creations");
                try { Directory.CreateDirectory(dir); } catch { }
                return dir;
            }
        }

        public static string NewImagePath(string id) => Path.Combine(ImageDir, id + ".png");

        public static List<UserCreation> All()
        {
            try
            {
                var json = Preferences.Get(Key, "");
                if (string.IsNullOrEmpty(json)) return new List<UserCreation>();
                var list = JsonSerializer.Deserialize<List<UserCreation>>(json) ?? new List<UserCreation>();
                return list.OrderByDescending(c => c.CreatedTicks).ToList();
            }
            catch { return new List<UserCreation>(); }
        }

        public static List<UserCreation> Dinos() => All().Where(c => c.Kind == CreationKind.Dinosaur).ToList();
        public static List<UserCreation> Spaces() => All().Where(c => c.Kind == CreationKind.Space).ToList();

        public static UserCreation? Get(string id) => All().FirstOrDefault(c => c.Id == id);

        public static void Save(UserCreation c)
        {
            var list = All();
            int i = list.FindIndex(x => x.Id == c.Id);
            if (i >= 0) list[i] = c; else list.Add(c);
            Persist(list);
        }

        public static void Delete(string id)
        {
            var c = Get(id);
            if (c != null)
            {
                try { if (File.Exists(c.ImagePath)) File.Delete(c.ImagePath); } catch { }
            }
            Persist(All().Where(x => x.Id != id).ToList());
        }

        private static void Persist(List<UserCreation> list)
        {
            try { Preferences.Set(Key, JsonSerializer.Serialize(list)); } catch { }
        }
    }
}
