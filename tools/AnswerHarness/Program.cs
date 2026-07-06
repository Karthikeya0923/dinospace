using dinospace;
using dinospace.Services;
using dinospace.Models;

// Interrogates NovaSaur's answer pipeline exactly the way the app does:
// PromptBuilder.Build -> InstantReply (good) or Prompt (needs the model).
// Every question a normal person types should come back INSTANT.
var carryover = new List<string>();
var history = new List<ChatMessage>();

string[] questions =
{
    // the exact questions that failed on-device
    "what is a smilodon",
    "how strong is a t rex",
    "how strong is the t rex",
    // every suggestion chip
    "How big was the T. Rex?", "Could a Spinosaurus beat a T. Rex?", "What was the biggest dinosaur ever?",
    "What is the fastest dinosaur?", "Did dinosaurs have feathers?", "Why did the dinosaurs go extinct?",
    "How fast was a Velociraptor?", "What did Stegosaurus eat?", "How many horns did Triceratops have?",
    "Could a T. Rex beat a Triceratops?", "How big were Therizinosaurus claws?", "Was Giganotosaurus bigger than T. Rex?",
    "How did Ankylosaurus defend itself?", "How big was Quetzalcoatlus?", "Are birds really dinosaurs?",
    "How big was the Megalodon?", "How long was Titanoboa?",
    "What phase is the moon tonight?", "What planets can I see tonight?", "When is the next full moon?",
    "What time is sunset today?",
    "What is a black hole?", "How do stars form?", "Why is Mars red?", "How hot is the Sun?",
    "Why does the Moon change shape?", "How big is the universe?", "What is a light-year?",
    "How many planets are there?", "Why is Venus the hottest planet?", "What is the Milky Way?",
    "Could people live on Mars?", "How old is the universe?", "What is a shooting star?",
    "Why does Uranus spin on its side?", "How fast does the ISS travel?", "Do aliens exist?",
    // typed-style questions, typos, casual phrasing
    "tell me about the woolly mammoth", "whats a dunkleosteus", "what is saturn", "how big is jupiter",
    "how far is neptune", "what is europa", "tell me about pluto", "waht is a velociraptor",
    "how heavy was argentinosaurus", "spinosarus vs giganotosaurus", "who would win trex or spino",
    "smilodon", "titanoboa", "how tall was brachiosaurus", "what did trex eat", "where did stegosaurus live",
    "when did the megalodon live", "is the moon out tonight", "whats in the sky tonight",
    "what does carnotaurus mean", "fastest dino", "biggest carnivore", "smallest dinosaur",
    "how many moons does saturn have", "how hot is venus", "does jupiter have rings",
    "what is the andromeda galaxy", "what is sagittarius a", "tell me about halleys comet",
    "why is the sky dark at night", "what are fossils", "can trex swim", "did raptors hunt in packs",
};

int instant = 0, model = 0, blocked = 0;
var needsModel = new List<string>();
foreach (var q in questions)
{
    var turn = PromptBuilder.Build(q, history, carryover);
    if (turn.Entities.Count > 0) carryover = new List<string>(turn.Entities);
    if (turn.InstantReply != null)
    {
        bool offTopic = turn.InstantReply == NovaGuard.OffTopic;
        if (offTopic) { blocked++; Console.WriteLine($"BLOCKED  {q}"); }
        else { instant++; Console.WriteLine($"instant  {q}  ->  {Snip(turn.InstantReply)}"); }
    }
    else { model++; needsModel.Add(q); Console.WriteLine($"MODEL    {q}"); }
}

Console.WriteLine($"\n=== {instant} instant, {model} need the model, {blocked} blocked as off-topic ===");
if (needsModel.Count > 0) { Console.WriteLine("Model-dependent:"); foreach (var q in needsModel) Console.WriteLine("  - " + q); }

static string Snip(string s) => s.Length <= 80 ? s : s[..80] + "...";

// round 2: the new knowledge-base topics, phrased like real people type
string[] round2 =
{
    "why is pluto not a planet", "do black holes suck everything in", "what is gravity",
    "why are planets round", "what is a supernova", "what is a neutron star",
    "what is dark matter", "are the stars we see already dead", "why do stars twinkle",
    "how fast does the earth spin", "will the sun die", "how cold is space",
    "can you breathe in space", "whats the difference between a comet and an asteroid",
    "what is the kuiper belt", "why does the moon cause tides", "who was the first person on the moon",
    "what was the first animal in space", "what do astronauts eat", "how do rockets work",
    "what is the biggest volcano", "were there dinosaurs in the sea", "is a pterodactyl a dinosaur",
    "can we clone dinosaurs", "what colour were dinosaurs", "how long did dinosaurs live",
    "when did dinosaurs live", "are sharks older than dinosaurs", "what killed the megalodon",
    "what was the smartest dinosaur", "what is a galaxy", "how fast is light", "what is a wormhole",
    "Could we bring dinosaurs back?", "How long did a T. Rex live?",
    // live-computed distances between bodies (the neptune-from-venus bug)
    "how far is neptune from venus", "how far is mars from earth",
    "distance between jupiter and saturn", "how far is the sun from earth",
    "how far is the andromeda galaxy from the milky way",
};
int i2 = 0, m2 = 0;
foreach (var q in round2)
{
    var turn = PromptBuilder.Build(q, history, carryover);
    if (turn.Entities.Count > 0) carryover = new List<string>(turn.Entities);
    if (turn.InstantReply != null && turn.InstantReply != NovaGuard.OffTopic) i2++;
    else { m2++; Console.WriteLine($"R2-MISS  {q}"); }
}
Console.WriteLine($"=== round 2: {i2} instant, {m2} missed ===");

// CI-friendly: fail the run if anything regressed.
Environment.Exit(model + blocked + m2 > 0 ? 1 : 0);
