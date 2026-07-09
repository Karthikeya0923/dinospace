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
        // Two genuinely different voices: Playful gets big rounded Baloo
        // headlines; Native gets the quiet editorial serif. Body text is
        // Nunito everywhere.
        public static string DisplayFont => Playful ? "Baloo" : "Serif";
        public static string DisplayItalicFont => Playful ? "Baloo" : "SerifItalic";
        public static string BodyFont => "Nunito";

        // ---- shape & scale ----
        // Native keeps corners tight and understated; Playful goes round.
        public static double CardRadius => Playful ? 24 : 14;
        public static double ButtonRadius => Playful ? 22 : 12;
        public static double HeroRadius => Playful ? 26 : 16;

        // Playful nudges headline type up a touch for a bolder, friendlier feel.
        public static double HeadlineScale => Playful ? 1.05 : 1.0;

        // Playful: chunky title + accent underline headers.
        // Native: tight ALL-CAPS with a hairline rule — plain and grown-up.
        public static bool FriendlyHeaders => Playful;

        // The rounded colour bubble behind the active tab is a Playful thing;
        // Native keeps a flat bar where only the tint marks the selection.
        public static bool BubbleTabs => Playful;
    }
}
