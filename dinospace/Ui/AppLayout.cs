namespace dinospace
{
    // The app has one layout: the storybook look from the design sheet — big
    // rounded Baloo headlines, lowercase labels, chunky pills, generous
    // corners. These knobs stay as the single source of truth for fonts and
    // shapes so every view keeps reading them the same way.
    public static class AppLayout
    {
        public static bool Playful => true;

        public static void ApplyCurrent() { }

        // ---- fonts ----
        public static string DisplayFont => "Baloo";
        public static string DisplayItalicFont => "Baloo";
        public static string BodyFont => "Body";

        // ---- shape & scale ----
        public static double CardRadius => 24;
        public static double ButtonRadius => 22;
        public static double HeroRadius => 26;
        public static double HeadlineScale => 1.05;
        public static bool FriendlyHeaders => true;
    }
}
