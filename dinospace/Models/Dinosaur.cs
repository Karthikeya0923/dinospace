namespace dinospace.Models
{
    // One encyclopedia entry for a prehistoric creature.
    // Stat strings keep a leading number so they can be parsed for the
    // visual stat bars and for "biggest / fastest" AI lookups:
    //   Length/Height in feet, Weight in kg, Speed in km/h.
    public class Dinosaur
    {
        public string Name { get; set; } = "";
        public string Pronunciation { get; set; } = "";
        public string Meaning { get; set; } = "";
        public string Era { get; set; } = "";
        public string Group { get; set; } = "";
        public string Diet { get; set; } = "";
        public string Category { get; set; } = "";     // Land / Sea / Flying
        public string ShortDescription { get; set; } = "";

        public string Length { get; set; } = "";
        public string Height { get; set; } = "";
        public string Width { get; set; } = "";
        public string Weight { get; set; } = "";
        public string Speed { get; set; } = "";

        // Bite force in PSI, e.g. "12,800 PSI". Empty for creatures with no
        // meaningful/known bite (most herbivores, toothless flyers).
        public string BiteForce { get; set; } = "";

        // 0-100 internal danger score. Drives the "Most Dangerous" collection
        // and the battle winner calculation. Never shown as a raw "Power" label.
        public int Strength { get; set; }

        public string ImageFile { get; set; } = "";

        public string AboutText { get; set; } = "";
        public string KeyFeaturesText { get; set; } = "";
        public string LifeEnvironmentText { get; set; } = "";
        public string BehaviourText { get; set; } = "";
        public string FunFactsText { get; set; } = "";

        // Nicknames, kid spellings, and common misnomers used by search and
        // by NovaSaur's retrieval ("trike" -> Triceratops). Optional.
        public string[] Aliases { get; set; } = System.Array.Empty<string>();
    }
}
