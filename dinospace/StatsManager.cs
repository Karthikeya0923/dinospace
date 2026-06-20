namespace dinospace
{
    public static class StatsManager
    {
        // ===== View tracking =====
        public static void RecordView(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            int count = Preferences.Get($"views_{name}", 0);
            Preferences.Set($"views_{name}", count + 1);
        }

        public static int GetViews(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            return Preferences.Get($"views_{name}", 0);
        }

        // Returns the name of the most-viewed entry, or "" if nothing has been opened yet
        public static string GetMostViewedName()
        {
            string topName = "";
            int topCount = 0;

            foreach (var d in DinosaurData.GetAll())
            {
                int c = GetViews(d.Name);
                if (c > topCount) { topCount = c; topName = d.Name; }
            }
            foreach (var s in SpaceData.GetAll())
            {
                int c = GetViews(s.Name);
                if (c > topCount) { topCount = c; topName = s.Name; }
            }
            return topName;
        }

        // ===== Daily streak =====
        // Call once when Home appears. Returns the current streak count.
        public static int UpdateAndGetStreak()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            string last = Preferences.Get("streak_last_date", "");
            int streak = Preferences.Get("streak_count", 0);

            if (last == today)
            {
                if (streak < 1) streak = 1;   // already counted today
            }
            else if (last == yesterday)
            {
                streak = streak + 1;          // opened yesterday too, continue
            }
            else
            {
                streak = 1;                   // first ever open, or a gap, restart
            }

            Preferences.Set("streak_count", streak);
            Preferences.Set("streak_last_date", today);
            return streak;
        }
    }
}