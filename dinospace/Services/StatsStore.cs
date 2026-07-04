using System;
using System.Linq;
using Microsoft.Maui.Storage;
using dinospace.Data;

namespace dinospace
{
    // Lightweight progress tracking: which entries were opened, the daily
    // open streak, quiz accuracy, and XP. All key/value in Preferences.
    public static class StatsStore
    {
        // ----- views -----
        public static void RecordView(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            Preferences.Set($"views_{name}", Preferences.Get($"views_{name}", 0) + 1);
        }

        public static int Views(string name)
            => string.IsNullOrEmpty(name) ? 0 : Preferences.Get($"views_{name}", 0);

        public static string MostViewedName()
        {
            string top = "";
            int best = 0;
            foreach (var d in DinoData.All)
            {
                int c = Views(d.Name);
                if (c > best) { best = c; top = d.Name; }
            }
            foreach (var s in SpaceData.All)
            {
                int c = Views(s.Name);
                if (c > best) { best = c; top = s.Name; }
            }
            return top;
        }

        public static int DinosSeen() => DinoData.All.Count(d => Views(d.Name) > 0);
        public static int SpaceSeen() => SpaceData.All.Count(s => Views(s.Name) > 0);

        // ----- daily streak -----
        public static int UpdateStreak()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            string last = Preferences.Get("streak_last", "");
            int streak = Preferences.Get("streak_count", 0);

            if (last == today) streak = Math.Max(streak, 1);
            else if (last == yesterday) streak += 1;
            else streak = 1;

            Preferences.Set("streak_count", streak);
            Preferences.Set("streak_last", today);
            return streak;
        }

        public static int Streak() => Preferences.Get("streak_count", 0);

        // ----- quizzes -----
        public static void RecordQuiz(string mode, int correct, int total)
        {
            Preferences.Set($"quiz_answered_{mode}", Preferences.Get($"quiz_answered_{mode}", 0) + total);
            Preferences.Set($"quiz_correct_{mode}", Preferences.Get($"quiz_correct_{mode}", 0) + correct);
            int best = Preferences.Get($"quiz_best_{mode}", 0);
            int pct = total > 0 ? (int)Math.Round(100.0 * correct / total) : 0;
            if (pct > best) Preferences.Set($"quiz_best_{mode}", pct);
        }

        public static string QuizAccuracy(string mode)
        {
            int answered = Preferences.Get($"quiz_answered_{mode}", 0);
            if (answered == 0) return "—";
            int correct = Preferences.Get($"quiz_correct_{mode}", 0);
            return $"{(int)Math.Round(100.0 * correct / answered)}%";
        }

        public static int QuizBest(string mode) => Preferences.Get($"quiz_best_{mode}", 0);

        public static void ClearProgress()
        {
            foreach (var d in DinoData.All) Preferences.Remove($"views_{d.Name}");
            foreach (var s in SpaceData.All) Preferences.Remove($"views_{s.Name}");
            foreach (var m in new[] { "Dinosaurs", "Space", "Mixed" })
            {
                Preferences.Remove($"quiz_answered_{m}");
                Preferences.Remove($"quiz_correct_{m}");
                Preferences.Remove($"quiz_best_{m}");
            }
            Preferences.Remove("streak_count");
            Preferences.Remove("streak_last");
        }
    }
}
