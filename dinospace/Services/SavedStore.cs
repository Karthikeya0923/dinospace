using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Storage;

namespace dinospace
{
    // Bookmarks for encyclopedia entries. Names are stored newest-first so the
    // Saved screen shows the most recently bookmarked item at the top.
    public static class SavedStore
    {
        // Two separate preference slots so each tab of the Saved screen can
        // read its own list without filtering. The _v2 suffix left the old
        // pre-release format behind without a migration.
        private const string DinoKey = "saved_dinos_v2";
        private const string SpaceKey = "saved_space_v2";

        public static IReadOnlyList<string> Dinos => Read(DinoKey);
        public static IReadOnlyList<string> Space => Read(SpaceKey);

        public static bool IsDinoSaved(string name) => Read(DinoKey).Contains(name);
        public static bool IsSpaceSaved(string name) => Read(SpaceKey).Contains(name);

        public static bool ToggleDino(string name) => Toggle(DinoKey, name);
        public static bool ToggleSpace(string name) => Toggle(SpaceKey, name);

        public static int Count => Read(DinoKey).Count + Read(SpaceKey).Count;

        public static void ClearAll()
        {
            Preferences.Remove(DinoKey);
            Preferences.Remove(SpaceKey);
        }

        // Returns the new saved state (true = now saved).
        private static bool Toggle(string key, string name)
        {
            var list = Read(key).ToList();
            bool nowSaved;
            if (list.Contains(name)) { list.Remove(name); nowSaved = false; }
            else { list.Insert(0, name); nowSaved = true; }
            Preferences.Set(key, string.Join("|", list));
            return nowSaved;
        }

        // Entries persist as one pipe-joined string ("Trex|Saturn|…") — entry
        // names never contain '|', and it keeps the whole store one Preferences
        // read with no JSON dependency.
        private static List<string> Read(string key)
        {
            var raw = Preferences.Get(key, "");
            if (string.IsNullOrEmpty(raw)) return new List<string>();
            return raw.Split('|').Where(s => s.Length > 0).ToList();
        }
    }
}
