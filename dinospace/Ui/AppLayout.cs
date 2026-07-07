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
        // Playful swaps the serif display for rounded Baloo everywhere headlines
        // appear; body text stays Nunito (already friendly and legible) in both.
        public static string DisplayFont => Playful ? "Baloo" : "Serif";
        public static string DisplayItalicFont => Playful ? "Baloo" : "SerifItalic";
        public static string BodyFont => "Nunito";

        // ---- shape & scale ----
        public static double CardRadius => Playful ? 24 : 16;
        public static double ButtonRadius => Playful ? 22 : 14;
        public static double HeroRadius => Playful ? 26 : 20;

        // Playful nudges headline type up a touch for a bolder, friendlier feel.
        public static double HeadlineScale => Playful ? 1.05 : 1.0;

        // Section headers: Native uses the tight ALL-CAPS + rule; Playful uses a
        // big rounded title with a chunky accent underline.
        public static bool FriendlyHeaders => Playful;

        // The tab bar: Native is a flat bar, Playful is a floating pill row with
        // a highlighted bubble behind the active tab.
        public static bool BubbleTabs => Playful;
    }
}
