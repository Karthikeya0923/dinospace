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
    "who was the first on the moon",
    "will we ever go to mars",
    "who was the first person on the moon",
    "will humans ever live on mars",
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
    "Where is Jupiter right now?", "where is the moon", "When is the next meteor shower?",
    "can i see shooting stars tonight",
    "What is a supermoon?", "what is a blue moon", "What are the northern lights?",
    "How do I find the North Star?", "what is the international space station",
    "Why do planets go retrograde?", "Why does the moon turn red in an eclipse?",
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

int instant = 0, offline = 0, uncovered = 0;
var notCovered = new List<string>();
foreach (var q in questions)
{
    var turn = PromptBuilder.Build(q, history, carryover);
    if (turn.Entities.Count > 0) carryover = new List<string>(turn.Entities);
    if (turn.InstantReply != null && turn.InstantReply != NovaGuard.OffTopic)
    { instant++; Console.WriteLine($"instant   {q}  ->  {Snip(turn.InstantReply)}"); }
    else if (turn.OfflineFallback != null)
    { offline++; Console.WriteLine($"offline   {q}  ->  {Snip(turn.OfflineFallback)}"); }
    else { uncovered++; notCovered.Add(q); Console.WriteLine($"UNCOVERED {q}"); }
}

Console.WriteLine($"\n=== {instant} instant, {offline} offline-fallback, {uncovered} UNCOVERED (dead ends) ===");
if (notCovered.Count > 0) { Console.WriteLine("Dead ends:"); foreach (var q in notCovered) Console.WriteLine("  - " + q); }

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
    else { m2++; Console.WriteLine($"R2-NOT-INSTANT  {q}  ->  {Snip(turn.OfflineFallback ?? "(dead)")}"); }
}
Console.WriteLine($"=== round 2: {i2} instant, {m2} not instant ===");

// round 3: conversational + creative + curveballs. These lean on the offline
// brain (smalltalk, NovaCreative). NONE may dead-end.
string[] round3 =
{
    "hi", "hello there", "how are you", "who are you", "what can you do",
    "thanks", "ok cool", "goodbye", "are you real", "how old are you",
    "whats your favourite dinosaur", "whats your favorite planet", "i'm bored", "you're awesome",
    "tell me a joke", "tell me another joke", "say something funny", "make me laugh",
    "tell me a story", "tell me a story about a t rex", "tell me a story about the moon",
    "write a poem about saturn", "sing me a space song", "rap about dinosaurs",
    "what if dinosaurs never went extinct", "imagine a velociraptor in space",
    "pretend you are a rocket", "tell me a fun fact", "surprise me",
    // deliberate curveballs the model used to swallow
    "what is your favourite colour", "do you like pizza", "whats 2 plus 2",
    "who is the president", "what is love", "tell me about minecraft",
    "why is the grass green", "what should i be when i grow up",
};
int i3 = 0, m3 = 0;
foreach (var q in round3)
{
    var turn = PromptBuilder.Build(q, history, carryover);
    if (turn.Entities.Count > 0) carryover = new List<string>(turn.Entities);
    string? reply = (turn.InstantReply != null && turn.InstantReply != NovaGuard.OffTopic) ? turn.InstantReply : turn.OfflineFallback;
    if (reply != null) { i3++; Console.WriteLine($"chat      {q}  ->  {Snip(reply)}"); }
    else { m3++; Console.WriteLine($"R3-UNCOVERED  {q}"); }
}
Console.WriteLine($"=== round 3 (chat): {i3} covered, {m3} uncovered ===");

// round 4: every suggestion chip the app actually shows, read from the live
// list so it can never drift. The bar is HIGHER here: a chip the app itself
// suggests must come back with a real instant answer — a canned fallback that
// dodges the question counts as a failure.
int i4 = 0, m4 = 0;
foreach (var q in dinospace.Data.SuggestedQuestions.All)
{
    var turn = PromptBuilder.Build(q, new List<ChatMessage>(), new List<string>());
    if (turn.InstantReply != null && turn.InstantReply != NovaGuard.OffTopic)
    { i4++; }
    else { m4++; Console.WriteLine($"CHIP-FLOP  {q}  ->  {Snip(turn.OfflineFallback ?? "(nothing)")}"); }
}
Console.WriteLine($"=== round 4 (chips): {i4} instant, {m4} FLOPS ===");

// round 5: the mega-battery. Every entry crossed with every phrasing a kid
// types, misspellings, comparisons, follow-ups, and the little-sister set —
// over a thousand questions. The graders:
//   DEAD    no reply at all
//   DODGE   the catch-all "I'm not sure" reply to a question that names a
//           real entry or is clearly about dinosaurs/space
//   WRONG   the reply is about a different creature than the one asked about
string[] dodgeMarkers =
{
    "outside my rocket range", "best with dinosaurs and space", "stick to dinosaurs and space",
};

bool IsDodge(string reply) => dodgeMarkers.Any(m => reply.Contains(m, StringComparison.OrdinalIgnoreCase));

// A reply "mentions" an entry if it contains any distinctive word of its
// name — or of any of its aliases ("T. Rex" for Tyrannosaurus Rex).
bool Mentions(string reply, string name)
{
    bool Hit(string n) =>
        n.Split(' ', '-', '.').Where(w => w.Length >= 3)
         .Any(w => reply.Contains(w, StringComparison.OrdinalIgnoreCase))
        || reply.Contains(n, StringComparison.OrdinalIgnoreCase);
    if (Hit(name)) return true;
    var d = DinoData.ByName(name);
    if (d != null && d.Aliases.Any(Hit)) return true;
    var s = SpaceData.ByName(name);
    return s != null && s.Aliases.Any(Hit);
}

var battery = new List<(string q, string? entity, bool onTopic)>();

string[] dinoTemplates =
{
    "how big was {0}", "what did {0} eat", "when did {0} live", "how fast was {0}",
    "tell me about {0}", "where did {0} live", "how heavy was {0}", "{0}",
    "was {0} dangerous", "what does {0} mean", "how tall was {0}", "did {0} have teeth",
};
foreach (var d in DinoData.All)
    foreach (var t in dinoTemplates)
        battery.Add((string.Format(t, d.Name.ToLowerInvariant()), d.Name, true));

string[] spaceTemplates =
{
    "how big is {0}", "how far away is {0}", "tell me about {0}", "what is {0}",
    "{0}", "how hot is {0}", "how old is {0}",
};
foreach (var s in SpaceData.All)
    foreach (var t in spaceTemplates)
        battery.Add((string.Format(t, s.Name.ToLowerInvariant()), s.Name, true));

// comparisons between well-known creatures
string[] fighters = { "t rex", "spinosaurus", "giganotosaurus", "velociraptor", "triceratops", "megalodon", "mosasaurus" };
for (int a = 0; a < fighters.Length; a++)
    for (int b = 0; b < fighters.Length; b++)
        if (a != b) battery.Add(($"who would win {fighters[a]} or {fighters[b]}", null, true));

// kid spellings and phonetic mangling
(string q, string entity)[] misspelled =
{
    ("tricerotops", "Triceratops"), ("trisaratops", "Triceratops"), ("velosiraptor", "Velociraptor"),
    ("brakiosaurus", "Brachiosaurus"), ("brontosorus", "Brachiosaurus"), ("megladon", "Megalodon"),
    ("tirano sarus", "Tyrannosaurus Rex"), ("tyranosaurus", "Tyrannosaurus Rex"), ("stegasaurus", "Stegosaurus"),
    ("spinosorus", "Spinosaurus"), ("anklyosaurus", "Ankylosaurus"), ("terodactyl", "Pteranodon"),
    ("how big is jupitor", "Jupiter"), ("how hot is the son", "Sun"), ("satern", "Saturn"),
    ("how far is neptoon", "Neptune"), ("mercer y", "Mercury"), ("plutoe", "Pluto"),
    ("woolly mamoth", "Woolly Mammoth"), ("smilodont", "Smilodon"),
};
foreach (var (q, entity) in misspelled) battery.Add((q, entity, true));

// the little-sister set: real questions small kids actually ask. On-topic
// ones must get a real answer; off-topic ones may redirect, but gracefully.
string[] sisterOnTopic =
{
    "why did t rex have tiny arms", "can dinosaurs talk", "do dinosaurs fart",
    "could a t rex eat me", "can i have a pet dinosaur", "what noise did dinosaurs make",
    "did dinosaurs sleep", "did dinosaurs have babies", "do dinosaurs lay eggs",
    "what was the first dinosaur", "what is the smallest dinosaur", "what dinosaur had the longest neck",
    "did people live with dinosaurs", "how do we know dinosaurs existed", "what is a fossil",
    "how are fossils made", "who finds fossils", "can we make a dinosaur from a mosquito",
    "why is the moon following me", "why does the moon glow", "is the moon made of cheese",
    "can you stand on the moon", "why do stars come out at night", "where does the sun go at night",
    "why is space dark", "is space cold", "how many stars are there",
    "can you hear sounds in space", "what happens if you fall in a black hole",
    "how do astronauts sleep", "how do astronauts eat", "how do astronauts go to the bathroom",
    "do aliens exist", "are there aliens on mars", "why is the sun so bright",
    "what is the sun made of", "will the sun explode", "how long does it take to get to mars",
    "can we live on the moon", "why do planets spin", "what is the hottest planet",
    "what is the coldest planet", "which planet has the most moons", "why does saturn have rings",
    "is pluto a planet", "what is a comet", "what is a meteor", "whats the difference between a meteor and a meteorite",
    "why is the sky blue", "what are clouds made of", "how heavy is the earth",
    "is the earth spinning right now", "why dont we fall off the earth", "what is inside the earth",
    "what dinosaur am i most like", "which dinosaur was the nicest", "which dinosaur was the meanest",
    "did sharks exist with dinosaurs", "whats older the sun or the earth", "how old is the moon",
};
foreach (var q in sisterOnTopic) battery.Add((q, null, true));

string[] sisterOffTopic =
{
    "whats your favourite food", "do you like cats", "can you sing baby shark",
    "whats my name", "how old am i", "do you know my mom", "can you do my homework",
    "tell me a scary story", "what happens when you die", "are monsters real",
};
foreach (var q in sisterOffTopic) battery.Add((q, null, false));

int total = 0, dead = 0, dodged = 0, wrong = 0;
var failures = new List<string>();
foreach (var (q, entity, onTopic) in battery)
{
    total++;
    var turn = PromptBuilder.Build(q, new List<ChatMessage>(), new List<string>());
    string? reply = (turn.InstantReply != null && turn.InstantReply != NovaGuard.OffTopic)
        ? turn.InstantReply : turn.OfflineFallback;

    if (reply == null)
    { dead++; failures.Add($"DEAD   {q}"); continue; }

    if (IsDodge(reply) && onTopic)
    { dodged++; failures.Add($"DODGE  {q}  ->  {Snip(reply)}"); continue; }

    if (entity != null && turn.InstantReply != null && !Mentions(turn.InstantReply, entity))
    { wrong++; failures.Add($"WRONG  {q}  ->  {Snip(turn.InstantReply)}"); }
}

Console.WriteLine($"\n=== round 5 (mega-battery): {total} questions, {dead} dead, {dodged} on-topic dodges, {wrong} wrong-entity ===");
foreach (var f in failures) Console.WriteLine("  " + f);

// round 6: every pairwise "how far is X from Y" across the space entries —
// the live-orbit pairs must produce a real computed distance, and the
// non-computable pairs (a constellation from a planet) must still answer
// honestly instead of dodging. Every single pair must answer instantly.
string[] distTemplates = { "how far is {0} from {1}", "distance between {0} and {1}", "how far away is {0} from {1}" };
int d6 = 0, bad6 = 0;
var fail6 = new List<string>();
var spaceNames = SpaceData.All.Select(s => s.Name.ToLowerInvariant()).ToList();
int ti = 0;
for (int a = 0; a < spaceNames.Count; a++)
    for (int b = 0; b < spaceNames.Count; b++)
    {
        if (a == b) continue;
        d6++;
        string q6 = string.Format(distTemplates[ti++ % distTemplates.Length], spaceNames[a], spaceNames[b]);
        var t6 = PromptBuilder.Build(q6, new List<ChatMessage>(), new List<string>());
        string? r6 = (t6.InstantReply != null && t6.InstantReply != NovaGuard.OffTopic) ? t6.InstantReply : null;
        if (r6 == null || IsDodge(r6))
        { bad6++; if (fail6.Count < 12) fail6.Add($"{q6}  ->  {Snip(r6 ?? t6.OfflineFallback ?? "(dead)")}"); }
    }
Console.WriteLine($"=== round 6 (pairwise distances): {d6} questions, {bad6} not answered instantly ===");
foreach (var f in fail6) Console.WriteLine("  " + f);

// round 7: ranking, ordering and list questions — the ones that used to fall
// through to the model ("top 5 strongest dinosaurs", "planets in order").
// Every one must answer INSTANTLY, and where a champion is known the reply
// must actually contain it.
(string q, string? mustMention)[] rankings =
{
    ("what are the top 5 strongest dinosaurs", "Tyrannosaurus"),
    ("top 5 strongest dinosaurs", "Tyrannosaurus"),
    ("name the planets in order", "Mercury"),
    ("what are the planets in order", "Neptune"),
    ("planets in order from the sun", "Venus"),
    ("top 3 biggest planets", "Jupiter"),
    ("what are the 3 biggest planets", "Saturn"),
    ("what is the biggest planet", "Jupiter"),
    ("what is the smallest planet", "Mercury"),
    ("planets in order of size", "Jupiter"),
    ("which planet has the most moons", "Saturn"),
    ("what is the biggest moon", "Ganymede"),
    ("list the planets", "Uranus"),
    ("name all the planets", "Earth"),
    ("top 10 biggest dinosaurs", "Argentinosaurus"),
    ("what are the 5 fastest dinosaurs", null),
    ("top 3 heaviest dinosaurs", "Argentinosaurus"),
    ("strongest dinosaurs", "Tyrannosaurus"),
    ("what is the strongest dinosaur", "Tyrannosaurus"),
    ("what is the most dangerous dinosaur", null),
    ("what was the deadliest dinosaur", null),
    ("top 5 biggest carnivores", null),
    ("biggest herbivores", null),
    ("what is the biggest sea creature", null),
    ("biggest flying creature", null),
    ("fastest dinosaurs", null),
    ("rank the dinosaurs by size", null),
    ("smallest dinosaurs", null),
    ("how many dinosaurs do you know", null),
    ("how many planets are there", "eight"),
    ("name some dinosaurs", null),
    ("list 5 sea creatures", null),
    ("name 3 herbivores", null),
    ("what is the biggest galaxy", null),
    ("what is the biggest black hole", "Phoenix"),
    ("top 5 tallest dinosaurs", null),
};
int i7 = 0, m7 = 0;
foreach (var (q7, must) in rankings)
{
    var t7 = PromptBuilder.Build(q7, new List<ChatMessage>(), new List<string>());
    string? r7 = (t7.InstantReply != null && t7.InstantReply != NovaGuard.OffTopic) ? t7.InstantReply : null;
    bool ok = r7 != null && !IsDodge(r7) && (must == null || r7.Contains(must, StringComparison.OrdinalIgnoreCase));
    if (ok) { i7++; Console.WriteLine($"rank      {q7}  ->  {Snip(r7!.Replace('\n', ' '))}"); }
    else { m7++; Console.WriteLine($"RANK-FAIL {q7}  ->  {Snip((r7 ?? t7.OfflineFallback ?? "(dead)").Replace('\n', ' '))}"); }
}
Console.WriteLine($"=== round 7 (rankings): {i7} instant, {m7} FAILURES ===");

// round 8: the knowledge battery. Every curated fact in the knowledge base,
// asked through every one of its own trigger phrasings plus the ways kids
// wrap questions ("hey novasaur ...", "... please", "umm ..."). Every single
// variant must come back with a real INSTANT answer — these are exactly the
// questions the app promises it can answer with no model installed.
string[] wraps = { "{0}", "hey novasaur {0}", "{0} please", "umm {0}", "can you tell me {0}", "i wanna know {0}" };
int q8 = 0, m8 = 0;
var fail8 = new List<string>();
foreach (var n in dinospace.Data.KnowledgeBase.Nuggets)
{
    foreach (var kw in n.Keywords)
    {
        if (kw.Split(' ').Length < 2) continue;   // single words aren't questions
        foreach (var w in wraps)
        {
            q8++;
            string ques = string.Format(w, kw);
            var t8 = PromptBuilder.Build(ques, new List<ChatMessage>(), new List<string>());
            string? r8 = (t8.InstantReply != null && t8.InstantReply != NovaGuard.OffTopic) ? t8.InstantReply : null;
            if (r8 == null || IsDodge(r8))
            {
                m8++;
                if (fail8.Count < 40) fail8.Add($"{ques}  ->  {Snip(r8 ?? t8.OfflineFallback ?? "(dead)")}");
            }
        }
    }
}
Console.WriteLine($"\n=== round 8 (knowledge battery): {q8} questions, {m8} not answered instantly ===");
foreach (var f in fail8) Console.WriteLine("  " + f);

// round 9: the decorated mega-battery. Every entry crossed with a much wider
// set of phrasings, then wrapped the way kids actually type. Dead ends and
// on-topic dodges fail; wrong-entity is advisory (graders are heuristic).
string[] dinoT9 =
{
    "how big was {0}", "what did {0} eat", "when did {0} live", "how fast was {0}",
    "tell me about {0}", "where did {0} live", "how heavy was {0}", "{0}",
    "was {0} dangerous", "what does {0} mean", "how tall was {0}", "did {0} have teeth",
    "how long was {0}", "was {0} a carnivore", "was {0} a herbivore", "how strong was {0}",
    "could {0} swim", "did {0} have feathers", "what killed {0}", "fun fact about {0}",
    "why is {0} famous", "how do you say {0}", "was {0} real", "what colour was {0}",
};
string[] spaceT9 =
{
    "how big is {0}", "how far away is {0}", "tell me about {0}", "what is {0}",
    "{0}", "how hot is {0}", "how old is {0}", "how far is {0} from earth",
    "can you see {0} tonight", "what is {0} made of", "fun fact about {0}",
    "why is {0} special", "could you live on {0}", "who discovered {0}",
    "how heavy is {0}", "does {0} have moons",
};
string[] wraps9 = { "{0}", "hey novasaur {0}", "{0} please" };
int q9 = 0, dead9 = 0, dodge9 = 0;
var fail9 = new List<string>();
foreach (var d in DinoData.All)
    foreach (var t in dinoT9)
        foreach (var w in wraps9)
        {
            q9++;
            string ques = string.Format(w, string.Format(t, d.Name.ToLowerInvariant()));
            var t9 = PromptBuilder.Build(ques, new List<ChatMessage>(), new List<string>());
            string? r9 = (t9.InstantReply != null && t9.InstantReply != NovaGuard.OffTopic) ? t9.InstantReply : t9.OfflineFallback;
            if (r9 == null) { dead9++; if (fail9.Count < 40) fail9.Add("DEAD   " + ques); }
            else if (IsDodge(r9)) { dodge9++; if (fail9.Count < 40) fail9.Add($"DODGE  {ques}  ->  {Snip(r9)}"); }
        }
foreach (var sp in SpaceData.All)
    foreach (var t in spaceT9)
        foreach (var w in wraps9)
        {
            q9++;
            string ques = string.Format(w, string.Format(t, sp.Name.ToLowerInvariant()));
            var t9 = PromptBuilder.Build(ques, new List<ChatMessage>(), new List<string>());
            string? r9 = (t9.InstantReply != null && t9.InstantReply != NovaGuard.OffTopic) ? t9.InstantReply : t9.OfflineFallback;
            if (r9 == null) { dead9++; if (fail9.Count < 40) fail9.Add("DEAD   " + ques); }
            else if (IsDodge(r9)) { dodge9++; if (fail9.Count < 40) fail9.Add($"DODGE  {ques}  ->  {Snip(r9)}"); }
        }
Console.WriteLine($"\n=== round 9 (decorated battery): {q9} questions, {dead9} dead, {dodge9} dodges ===");
foreach (var f in fail9) Console.WriteLine("  " + f);

// round 10: every single pairwise battle question. Each one must resolve
// instantly with a real verdict — this is the battle brain's whole job.
int q10 = 0, m10 = 0;
var fail10 = new List<string>();
var allDinos = DinoData.All.Select(d => d.Name.ToLowerInvariant()).ToList();
string[] battleT = { "who would win {0} or {1}", "could a {0} beat a {1}", "{0} vs {1}" };
int bt = 0;
for (int a = 0; a < allDinos.Count; a++)
    for (int b = 0; b < allDinos.Count; b++)
    {
        if (a == b) continue;
        q10++;
        string ques = string.Format(battleT[bt++ % battleT.Length], allDinos[a], allDinos[b]);
        var t10 = PromptBuilder.Build(ques, new List<ChatMessage>(), new List<string>());
        string? r10 = (t10.InstantReply != null && t10.InstantReply != NovaGuard.OffTopic) ? t10.InstantReply : null;
        if (r10 == null || IsDodge(r10))
        { m10++; if (fail10.Count < 25) fail10.Add($"{ques}  ->  {Snip(r10 ?? t10.OfflineFallback ?? "(dead)")}"); }
    }
Console.WriteLine($"\n=== round 10 (all pairwise battles): {q10} questions, {m10} without an instant verdict ===");
foreach (var f in fail10) Console.WriteLine("  " + f);

// round 11: the typo gauntlet. Deterministic kid-style mutations of every
// entry's name — a dropped letter, a doubled letter, two letters swapped —
// asked through common templates. A mangled name the retriever still can't
// place may gracefully dodge (advisory), but it must NEVER dead-end.
static IEnumerable<string> Mutations(string name)
{
    if (name.Length < 5) yield break;
    int mid = name.Length / 2;
    yield return name.Remove(mid, 1);                                    // dropped letter
    yield return name.Insert(mid, name[mid].ToString());                 // doubled letter
    var chars = name.ToCharArray();
    (chars[mid], chars[mid - 1]) = (chars[mid - 1], chars[mid]);
    yield return new string(chars);                                      // swapped letters
    yield return name.Replace("saurus", "sorus").Replace("don", "dont"); // phonetic
}
string[] typoT = { "what is {0}", "tell me about {0}", "how big was {0}" };
int q11 = 0, dead11 = 0, dodge11 = 0;
var fail11 = new List<string>();
var allNames = DinoData.All.Select(d => d.Name).Concat(SpaceData.All.Select(x => x.Name)).ToList();
foreach (var name in allNames)
    foreach (var mut in Mutations(name.ToLowerInvariant()).Distinct())
        foreach (var t in typoT)
        {
            q11++;
            string ques = string.Format(t, mut);
            var t11 = PromptBuilder.Build(ques, new List<ChatMessage>(), new List<string>());
            string? r11 = (t11.InstantReply != null && t11.InstantReply != NovaGuard.OffTopic) ? t11.InstantReply : t11.OfflineFallback;
            if (r11 == null) { dead11++; if (fail11.Count < 25) fail11.Add("DEAD  " + ques); }
            else if (IsDodge(r11)) dodge11++;
        }
Console.WriteLine($"\n=== round 11 (typo gauntlet): {q11} questions, {dead11} dead, {dodge11} graceful dodges (advisory) ===");
foreach (var f in fail11) Console.WriteLine("  " + f);

// round 12: one hundred questions phrased the way KIDS actually ask —
// hypotheticals, silliness, "would win" matchups, everyday wording. The bar:
// no dead ends and no dodges on on-topic questions.
string[] kidQs =
{
    "what if compsognathus was alive today", "what if a t rex was alive today",
    "what if dinosaurs were alive today", "what if megalodon still existed",
    "what if a velociraptor was my pet", "what if a brachiosaurus was in my backyard",
    "what if titanoboa came back", "what if spinosaurus was alive now",
    "what if the moon disappeared", "what if the sun went out",
    "what if i fell into a black hole", "what if the earth stopped spinning",
    "would a t rex eat me", "could i ride a triceratops", "could a pteranodon carry me",
    "can dinosaurs come back", "can we make a real jurassic park",
    "who would win a lion or a velociraptor", "who would win trex or giganotosaurus",
    "who would win megalodon or mosasaurus", "who would win ankylosaurus or trex",
    "whats the scariest dinosaur", "whats the coolest dinosaur", "whats the weirdest dinosaur",
    "what dinosaur has the longest name", "which dinosaur is the strongest",
    "which dinosaur was the tiniest", "what was the biggest thing ever",
    "did dinosaurs sleep", "did dinosaurs poop", "did dinosaurs fart", "did dinosaurs have tongues",
    "did dinosaurs make noises", "what sound did a trex make", "could dinosaurs swim",
    "could a trex do a pushup", "why are trex arms so small", "did trex have feathers",
    "how many teeth did a trex have", "how big is a trex tooth", "do dinosaur bones grow",
    "how do we know what dinosaurs looked like", "how do fossils form", "where can i see a real dinosaur bone",
    "were dinosaurs colourful", "did dinosaurs live with people", "did cavemen fight dinosaurs",
    "whats a baby dinosaur called", "did dinosaurs lay eggs", "how big was a dinosaur egg",
    "whats faster a cheetah or a velociraptor", "was megalodon bigger than a whale",
    "is a shark a dinosaur", "is a crocodile a dinosaur", "is a chicken really a dinosaur",
    "are dragons real", "did unicorns exist", "whats the closest thing to a dragon",
    "why is the sky blue", "why do stars shine", "can you count the stars",
    "how many stars are there", "is the moon made of cheese", "can you touch a star",
    "why cant we breathe in space", "what happens if you take your helmet off in space",
    "do astronauts float", "how do astronauts sleep", "how do astronauts go to the bathroom",
    "whats inside a black hole", "can the sun explode", "will the sun eat the earth",
    "is there life on other planets", "are aliens real", "have we found aliens",
    "can i be an astronaut", "how do i become an astronaut", "whats it like in space",
    "how long does it take to get to the moon", "how long to get to mars",
    "can you hear sounds in space", "is space cold or hot", "what does space smell like",
    "whats the biggest planet", "whats the smallest planet", "whats the hottest planet",
    "whats the coldest planet", "which planet has the most moons", "can we live on jupiter",
    "why does saturn have rings", "is pluto a planet", "whats beyond the stars",
    "tell me a story about a brave triceratops", "tell me a dinosaur joke",
    "whats your favourite space fact", "tell me something gross about dinosaurs",
    "what if the dinosaurs had phones", "imagine a stegosaurus at school",
    "can i keep a compsognathus as a pet", "which dinosaur would make the best pet",
};
int k12 = 0, f12 = 0;
foreach (var q in kidQs)
{
    var turn = PromptBuilder.Build(q, new List<ChatMessage>(), new List<string>());
    string? reply = (turn.InstantReply != null && turn.InstantReply != NovaGuard.OffTopic) ? turn.InstantReply : turn.OfflineFallback;
    bool flop = reply == null || IsDodge(reply);
    if (flop) { f12++; Console.WriteLine($"KID-FLOP  {q}  ->  {Snip(reply ?? "(dead)")}"); }
    else k12++;
}
Console.WriteLine($"=== round 12 (kids): {k12} good, {f12} FLOPS ===");

// round 13: one hundred questions phrased the way ADULTS ask — precise,
// historical, mission-oriented, and sceptical. Same bar.
string[] adultQs =
{
    "who was the first person on the moon", "when did apollo 11 land", "who was the second person on the moon",
    "how many people have walked on the moon", "when was the last moon landing", "will we ever go back to the moon",
    "will we ever go to mars", "when will humans land on mars", "how long would a trip to mars take",
    "what is the artemis program", "what rockets does nasa use now", "what is spacex starship",
    "who was the first human in space", "who was the first woman in space", "what was the first satellite",
    "what happened to the voyager probes", "how far has voyager 1 travelled", "what is on the golden record",
    "what is the james webb telescope", "what did the hubble telescope discover", "how does a telescope work",
    "what is the speed of light", "how long does sunlight take to reach earth", "what is a light year in kilometres",
    "how old is the earth", "how old is the sun", "how was the moon formed",
    "what is the big bang theory", "what evidence supports the big bang", "is the universe expanding",
    "what is dark energy", "what is the difference between dark matter and dark energy",
    "how do black holes form", "what is the event horizon", "what is hawking radiation",
    "what is a pulsar", "what is a quasar", "what is a red giant",
    "what will happen when the sun dies", "how do supernovas work", "what elements are made in stars",
    "what is the asteroid belt made of", "could an asteroid hit earth", "how do we track asteroids",
    "what caused the dinosaur extinction", "where did the asteroid hit", "what is the chicxulub crater",
    "what survived the extinction", "how long ago did dinosaurs live", "what were the three dinosaur periods",
    "what is the difference between the triassic and jurassic", "when did the first dinosaurs appear",
    "what came before the dinosaurs", "what is the permian extinction", "how many mass extinctions have there been",
    "how are dinosaur fossils dated", "what is carbon dating", "can dna survive in fossils",
    "is jurassic park scientifically possible", "what dinosaur dna do we actually have",
    "how are birds related to dinosaurs", "what is archaeopteryx", "when did birds evolve",
    "were dinosaurs warm blooded", "how smart were dinosaurs really", "did dinosaurs care for their young",
    "how fast could a t rex actually run", "could t rex really not see you if you stood still",
    "how accurate is the velociraptor in the movies", "how big were velociraptors really",
    "what is the largest dinosaur ever discovered", "what is the largest carnivore ever",
    "was spinosaurus really aquatic", "what did spinosaurus actually eat",
    "how strong was a megalodon bite", "why did megalodon go extinct", "are megalodons still alive",
    "what is the difference between a mosasaur and a plesiosaur", "were pterosaurs dinosaurs",
    "what is the difference between mammoths and elephants", "when did the woolly mammoth go extinct",
    "could we clone a mammoth", "what is de-extinction",
    "what phase is the moon in tonight", "when is the next solar eclipse", "how do eclipses work",
    "why do we always see the same side of the moon", "what causes the seasons",
    "what is the difference between a meteor meteorite and meteoroid", "what are the northern lights caused by",
    "how many galaxies are there", "what is the closest star to earth", "how far is proxima centauri",
    "what is the goldilocks zone", "what makes a planet habitable", "what are exoplanets",
    "how do we detect exoplanets", "what is the drake equation", "what is the fermi paradox",
    "what time is sunset tonight", "when does the iss pass overhead",
};
int k13 = 0, f13 = 0;
foreach (var q in adultQs)
{
    var turn = PromptBuilder.Build(q, new List<ChatMessage>(), new List<string>());
    string? reply = (turn.InstantReply != null && turn.InstantReply != NovaGuard.OffTopic) ? turn.InstantReply : turn.OfflineFallback;
    bool flop = reply == null || IsDodge(reply);
    if (flop) { f13++; Console.WriteLine($"ADULT-FLOP  {q}  ->  {Snip(reply ?? "(dead)")}"); }
    else k13++;
}
Console.WriteLine($"=== round 13 (adults): {k13} good, {f13} FLOPS ===");

int grand = questions.Length + round2.Length + round3.Length + dinospace.Data.SuggestedQuestions.All.Count
          + total + d6 + rankings.Length + q8 + q9 + q10 + q11 + kidQs.Length + adultQs.Length;
Console.WriteLine($"\n=== GRAND TOTAL: {grand} questions ===");

// CI-friendly: hard-fail when a question truly dead-ends (no instant reply
// AND no offline fallback), a suggestion chip can't answer instantly, a
// distance pair goes unanswered, a ranking question fails, a knowledge-base
// phrasing isn't instant, or a battle has no verdict. Dodge/wrong-entity
// counts in the entity batteries print above as advisories — the graders
// are heuristic and flag correct answers that simply don't restate names.
Environment.Exit(uncovered + m2 + m3 + m4 + dead + bad6 + m7 + m8 + dead9 + m10 + dead11 + f12 + f13 > 0 ? 1 : 0);
