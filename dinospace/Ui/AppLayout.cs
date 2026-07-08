namespace dinospace
{
    public enum LayoutMode { Native, Playful }

    // The app ships two completely different layouts on top of the same
    // features and the same themes:
    //
    //  • Native  — the editorial default: DM Serif headlines, tight caps-and-
    //              rule section headers, a flat iOS-style tab bar. Grown-up,
    //              magazine-like.
    //
    //  • Playful — built for 5-to-10-year-olds: big rounded Baloo headlines,
    //              chunky pill tabs with a highlighted bubble, a warm "Hi
    //              explorer!" home screen, rounder cards and bigger targets.
    //
    // Views never check the mode directly for fonts or shapes — they read the
    // knobs below (Ui forwards to them), so switching layouts re-skins the
    // whole app the same way switching themes re-colours it. A handful of
    // identity pieces (the tab bar, the home masthead) branch on Playful.
    public static class AppLayout
    {
        public static LayoutMode Mode { get; private set; } = LayoutMode.Native;
        public static bool Playful => Mode == LayoutMode.Playful;

        public static void ApplyCurrent()
            => Mode = Services.AppSettings.LayoutId == "playful" ? LayoutMode.Playful : LayoutMode.Native;

        // ---- fonts ----
        // Rounded Baloo headlines in BOTH layouts — the old serif read like a
        // cooking magazine, not an encyclopedia for curious kids. Native stays
        // the denser, more factual layout; Playful stays the big-and-bright
        // one. Body text is Nunito everywhere.
        public static string DisplayFont => "Baloo";
        public static string DisplayItalicFont => "Baloo";
        public static string BodyFont => "Nunito";

        // ---- shape & scale ----
        public static double CardRadius => Playful ? 24 : 20;
        public static double ButtonRadius => Playful ? 22 : 18;
        public static double HeroRadius => Playful ? 26 : 24;

        // Playful nudges headline type up a touch for a bolder, friendlier feel.
        public static double HeadlineScale => Playful ? 1.05 : 1.0;

        // Chunky title + accent underline headers in both layouts (the old
        // ALL-CAPS-with-hairline header was the other half of the recipe look).
        public static bool FriendlyHeaders => true;

        // Both layouts use the rounded bubble behind the active tab; Native's
        // bar is just a little more compact.
        public static bool BubbleTabs => true;
    }
}
