namespace dinospace
{
    public static class SavedManager
    {
        private const string SavedDinosKey = "saved_dinos";
        private const string SavedSpaceKey = "saved_space";

        // Save a dinosaur
        public static void SaveDino(string name)
        {
            var saved = GetSavedDinos();
            if (!saved.Contains(name))
            {
                saved.Add(name);
                Preferences.Set(SavedDinosKey, string.Join("|", saved));
            }
        }

        // Remove a dinosaur
        public static void UnsaveDino(string name)
        {
            var saved = GetSavedDinos();
            saved.Remove(name);
            Preferences.Set(SavedDinosKey, string.Join("|", saved));
        }

        // Check if a dinosaur is saved
        public static bool IsDinoSaved(string name)
        {
            return GetSavedDinos().Contains(name);
        }

        // Get all saved dinosaur names
        public static List<string> GetSavedDinos()
        {
            var raw = Preferences.Get(SavedDinosKey, "");
            if (string.IsNullOrEmpty(raw)) return new List<string>();
            return raw.Split('|').ToList();
        }

        // Save a space object
        public static void SaveSpace(string name)
        {
            var saved = GetSavedSpace();
            if (!saved.Contains(name))
            {
                saved.Add(name);
                Preferences.Set(SavedSpaceKey, string.Join("|", saved));
            }
        }

        // Remove a space object
        public static void UnsaveSpace(string name)
        {
            var saved = GetSavedSpace();
            saved.Remove(name);
            Preferences.Set(SavedSpaceKey, string.Join("|", saved));
        }

        // Check if a space object is saved
        public static bool IsSpaceSaved(string name)
        {
            return GetSavedSpace().Contains(name);
        }

        // Get all saved space names
        public static List<string> GetSavedSpace()
        {
            var raw = Preferences.Get(SavedSpaceKey, "");
            if (string.IsNullOrEmpty(raw)) return new List<string>();
            return raw.Split('|').ToList();
        }

        // Clear everything
        public static void ClearAll()
        {
            Preferences.Remove(SavedDinosKey);
            Preferences.Remove(SavedSpaceKey);
            Preferences.Remove("quiz_best_Dinosaurs");
            Preferences.Remove("quiz_best_Space");
            Preferences.Remove("quiz_best_Mixed");
        }
    }
}