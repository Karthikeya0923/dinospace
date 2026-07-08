namespace dinospace.Models
{
    // One encyclopedia entry for a space object (planet, star, galaxy, ...).
    // The four stat slots are label/value pairs because different kinds of
    // objects have different meaningful stats.
    public class SpaceObject
    {
        public string Name { get; set; } = "";
        public string Pronunciation { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string TypeLabel { get; set; } = "";    // Planet / Star / Galaxy / ...
        public string Category { get; set; } = "";     // Solar System / Stars / Deep Space / Exploration
        public string ShortDescription { get; set; } = "";

        public string Stat1Label { get; set; } = "";
        public string Stat1Value { get; set; } = "";
        public string Stat2Label { get; set; } = "";
        public string Stat2Value { get; set; } = "";
        public string Stat3Label { get; set; } = "";
        public string Stat3Value { get; set; } = "";
        public string Stat4Label { get; set; } = "";
        public string Stat4Value { get; set; } = "";

        public string ImageFile { get; set; } = "";

        // Set only when this entry is a user drawing (converted UserCreation):
        // the canvas colour the drawing should be letterboxed on so it never
        // gets cropped. Empty for built-in entries.
        public string CreationBg { get; set; } = "";

        public string AboutText { get; set; } = "";
        public string KeyFeaturesText { get; set; } = "";
        public string OrbitMovementText { get; set; } = "";
        public string SurfaceCompositionText { get; set; } = "";
        public string HistoryText { get; set; } = "";
        public string WhatsInsideText { get; set; } = "";
        public string FunFactsText { get; set; } = "";

        // Nicknames and common names used by search and NovaSaur's retrieval.
        public string[] Aliases { get; set; } = System.Array.Empty<string>();
    }
}
