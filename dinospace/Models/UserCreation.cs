using System;

namespace dinospace.Models
{
    public enum CreationKind { Dinosaur, Space }

    // A creature or space object the user drew and described themselves. It
    // carries every field a real encyclopedia entry has, and converts into the
    // same Dinosaur / SpaceObject types the rest of the app already renders —
    // so a creation battles, lists, and opens a detail page just like a
    // built-in entry (minus the Ask-NovaSaur button, since Nova can't know it).
    public class UserCreation
    {
        public string Id { get; set; } = "";
        public CreationKind Kind { get; set; }
        public string ImagePath { get; set; } = "";     // absolute path to the drawing PNG
        public long CreatedTicks { get; set; }

        // Shared
        public string Name { get; set; } = "";
        public string Pronunciation { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string About { get; set; } = "";
        public string FunFacts { get; set; } = "";

        // Dinosaur
        public string Meaning { get; set; } = "";
        public string Era { get; set; } = "";           // Triassic / Jurassic / Cretaceous
        public string Diet { get; set; } = "";           // Carnivore / Herbivore / Omnivore
        public string Category { get; set; } = "Land";   // Land / Sea / Flying
        public string Length { get; set; } = "";         // plain number (feet)
        public string Height { get; set; } = "";         // plain number (feet)
        public string Weight { get; set; } = "";         // plain number (kg)
        public string Speed { get; set; } = "";          // plain number (km/h)
        public string BiteForce { get; set; } = "";      // plain number (PSI)

        // Space
        public string Subtitle { get; set; } = "";
        public string TypeLabel { get; set; } = "";      // Planet / Star / Galaxy / ...
        public string SpaceCategory { get; set; } = "Solar System";
        public string Stat1Label { get; set; } = "";
        public string Stat1Value { get; set; } = "";
        public string Stat2Label { get; set; } = "";
        public string Stat2Value { get; set; } = "";
        public string Stat3Label { get; set; } = "";
        public string Stat3Value { get; set; } = "";
        public string Stat4Label { get; set; } = "";
        public string Stat4Value { get; set; } = "";

        // A number the user typed gets its unit appended for display; anything
        // they wrote with letters is left exactly as-is.
        private static string WithUnit(string raw, string unit)
        {
            raw = (raw ?? "").Trim();
            if (raw.Length == 0) return "";
            bool bareNumber = true;
            foreach (char c in raw.Replace(",", "").Replace(".", ""))
                if (!char.IsDigit(c)) { bareNumber = false; break; }
            return bareNumber ? $"{raw} {unit}" : raw;
        }

        public Dinosaur ToDinosaur() => new()
        {
            Name = Name,
            Pronunciation = Pronunciation,
            Meaning = Meaning,
            Era = Era,
            Group = "Your creation",
            Diet = string.IsNullOrWhiteSpace(Diet) ? "Carnivore" : Diet,
            Category = string.IsNullOrWhiteSpace(Category) ? "Land" : Category,
            ShortDescription = ShortDescription,
            Length = WithUnit(Length, "feet"),
            Height = WithUnit(Height, "feet"),
            Weight = WithUnit(Weight, "kg"),
            Speed = WithUnit(Speed, "km/h"),
            BiteForce = WithUnit(BiteForce, "PSI"),
            ImageFile = ImagePath,
            AboutText = About,
            FunFactsText = FunFacts,
        };

        public SpaceObject ToSpaceObject() => new()
        {
            Name = Name,
            Pronunciation = Pronunciation,
            Subtitle = Subtitle,
            TypeLabel = string.IsNullOrWhiteSpace(TypeLabel) ? "Space object" : TypeLabel,
            Category = string.IsNullOrWhiteSpace(SpaceCategory) ? "Solar System" : SpaceCategory,
            ShortDescription = ShortDescription,
            Stat1Label = Stat1Label, Stat1Value = Stat1Value,
            Stat2Label = Stat2Label, Stat2Value = Stat2Value,
            Stat3Label = Stat3Label, Stat3Value = Stat3Value,
            Stat4Label = Stat4Label, Stat4Value = Stat4Value,
            ImageFile = ImagePath,
            AboutText = About,
            FunFactsText = FunFacts,
        };

        // What shows on the gallery/list card under the drawing.
        public string MetaLine => Kind == CreationKind.Dinosaur
            ? (string.IsNullOrWhiteSpace(Era) ? "Your dinosaur" : "Your dinosaur · " + Era)
            : (string.IsNullOrWhiteSpace(TypeLabel) ? "Your space object" : "Your " + TypeLabel.ToLowerInvariant());
    }
}
