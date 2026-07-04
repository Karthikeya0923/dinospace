using System;
using System.Collections.Generic;
using System.Linq;

namespace dinospace.Services
{
    // Deterministic safety + smalltalk layer that runs around the model.
    //
    // Two design goals, learned from the old version:
    //   1. Stay genuinely safe for kids (personal info, self-harm, violence at
    //      people, weapons, adult topics) — handled with plain string logic
    //      that a prompt can't jailbreak.
    //   2. Be GENEROUS about what counts as answerable. The old gate refused
    //      far too much, which is why Nova "couldn't answer simple questions".
    //      Anything science / nature / space / history flavoured gets through;
    //      only clearly unrelated chit-chat is redirected.
    public static class NovaGuard
    {
        public const string OffTopic =
            "I'm your dinosaur and space guide, so that one's outside what I know. " +
            "Try me on something prehistoric or cosmic — like how big a T. Rex was, or why Mars is red!";

        private const string PersonalInfo =
            "I never ask for or share private information like names, addresses, phone numbers, or passwords. " +
            "Let's stick to dinosaurs and space — want to hear about the fastest dinosaur?";

        private const string Care =
            "That sounds really important, and it's best to talk it through with a parent, teacher, or another trusted adult — not an app. " +
            "I'm right here whenever you'd like to explore dinosaurs and space.";

        private const string GentleNo =
            "That's not something I can help with. I'm all about dinosaurs and space — ask me anything about those!";

        private const string BadAnswer =
            "Hmm, that came out a bit garbled. Try asking it a different way and I'll give it another go.";

        // ---------- smalltalk (instant, no model) ----------

        public static string? SmallTalk(string q)
        {
            if (string.IsNullOrEmpty(q)) return null;

            string[] greetings = { "hi", "hello", "hey", "yo", "hiya", "sup", "hi there", "hello there", "hey there", "good morning", "good afternoon", "good evening", "howdy" };
            if (greetings.Contains(q))
                return "Hey! I'm NovaSaur. Ask me anything about dinosaurs or space — I love both.";

            if (q is "bye" or "goodbye" or "see you" or "good night" or "goodnight" or "cya")
                return "See you later! Come back any time you're curious about dinosaurs or space.";

            // Short acknowledgements ("ok", "cool", "nice") get an instant, on
            // -brand reply so trivial one-word messages never bother the model.
            string[] acks =
            {
                "ok", "okay", "okie", "k", "kk", "cool", "nice", "great", "awesome",
                "wow", "woah", "whoa", "lol", "lmao", "haha", "hah", "oh", "ohh",
                "got it", "i see", "makes sense", "interesting", "neat", "sweet",
                "amazing", "fair", "true", "right", "yeah", "yea", "yep", "yup",
                "no way", "damn", "crazy", "fr", "bet", "alright", "aight"
            };
            if (acks.Contains(q))
                return "Glad you think so! What else would you like to know about dinosaurs or space?";

            if (Phrase(q, "thank you") || Word(q, "thanks") || Word(q, "thx") || Word(q, "ty"))
                return "You're welcome! Want to ask another dinosaur or space question?";

            if (Phrase(q, "who are you") || Phrase(q, "what are you") || Phrase(q, "your name") || Phrase(q, "who made you"))
                return "I'm NovaSaur, the dinosaur and space guide inside DinoSpace. I run right on your device and I can answer questions about prehistoric creatures and outer space.";

            if (Phrase(q, "what can you do") || Phrase(q, "how do you work") || Phrase(q, "help") && q.Length <= 6)
                return "I answer questions about dinosaurs, prehistoric creatures, and space. Try me — how big was a Brachiosaurus? Why is Mars red?";

            if (Phrase(q, "how are you") || Phrase(q, "how are u"))
                return "I'm great, thanks for asking! Ready to talk dinosaurs and space whenever you are.";

            return null;
        }

        // ---------- pre-model safety ----------

        // Returns a caring/safe reply if the question needs special handling,
        // else null to let it proceed.
        public static string? Screen(string q)
        {
            string p = " " + (q ?? "") + " ";

            string[] personal =
            {
                "your address", "my address", "home address", "phone number", "password",
                "where do you live", "where i live", "what school", "my school is",
                "credit card", "my last name", "my full name", "meet me", "meet up",
                "send me a picture", "your instagram", "your snapchat", "my instagram", "my snapchat"
            };
            if (personal.Any(x => p.Contains(" " + x))) return PersonalInfo;

            string[] selfHarm = { "kill myself", "hurt myself", "want to die", "suicide", "self harm", "cut myself", "end my life" };
            if (selfHarm.Any(x => p.Contains(x))) return Care;

            // Violence at a PERSON (dino-vs-dino battles are fine, so this only
            // fires when a person is the target).
            string[] harmVerbs = { "kill", "hurt", "attack", "beat up", "punch", "stab", "shoot" };
            string[] people = { "my ", " me ", "myself", "someone", "somebody", "people", "him ", "her ", "them ", "friend", "brother", "sister", "mom", "dad", "teacher", "kid ", "kids ", "student", "classmate" };
            if (harmVerbs.Any(v => p.Contains(" " + v)) && people.Any(t => p.Contains(t))) return Care;

            string[] danger = { "bomb", "explosive", "gun ", "guns ", "firearm", "weapon", "make poison", "poison someone" };
            if (danger.Any(x => p.Contains(" " + x))) return GentleNo;

            string[] adult = { "sex", "sexy", "naked", "nude", "porn", "drugs", "vape", "vaping", "weed", "cocaine", "alcohol", "beer", "cigarette", "gambling" };
            if (adult.Any(x => Word(p.Trim(), x))) return GentleNo;

            return null;
        }

        // The topic gate. Deliberately generous — see class notes. Returns true
        // if the question is worth sending to the model.
        public static bool OnTopic(string q, bool hasEntity, bool hasKnowledge, bool hasCarryover)
        {
            if (hasEntity || hasKnowledge) return true;

            string p = " " + (q ?? "") + " ";
            int words = p.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            // Any broadly science/nature/space/history word lets it through.
            foreach (var t in p.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string tok = t.Length > 3 && t.EndsWith("s") ? t[..^1] : t;
                if (Vocab.Contains(tok)) return true;
            }
            foreach (var ph in VocabPhrases)
                if (p.Contains(" " + ph)) return true;

            // Short follow-ups with a pronoun, when we know what "it" is.
            if (hasCarryover)
            {
                string[] pronouns = { " it ", " its ", " they ", " them ", " their ", " he ", " she ", " his ", " her ", " this ", " that ", " those ", " one " };
                if (words <= 12 && pronouns.Any(pr => p.Contains(pr))) return true;
                if (words <= 3) return true;
            }

            return false;
        }

        // ---------- post-model safety ----------

        public static string? CheckAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return BadAnswer;
            string a = " " + answer.ToLowerInvariant() + " ";

            if (a.Contains("http://") || a.Contains("https://") || a.Contains("www.") || a.Contains("@gmail") || a.Contains("@yahoo"))
                return BadAnswer;

            string[] blocked = { "sex", "sexy", "naked", "nude", "porn", "suicide", "kill yourself", "cocaine", "heroin" };
            if (blocked.Any(b => Word(a.Trim(), b))) return BadAnswer;

            if (a.Contains("as an ai language model") || a.Contains("system prompt"))
                return BadAnswer;

            return null;
        }

        // ---------- vocab ----------

        private static readonly HashSet<string> Vocab = new()
        {
            "dino","dinosaur","prehistoric","fossil","fossils","extinct","extinction","dig","paleontologist","paleontology",
            "cretaceous","jurassic","triassic","reptile","predator","prey","herbivore","carnivore","omnivore","raptor",
            "claw","teeth","tooth","horn","horns","tail","bite","scales","feather","feathers","egg","eggs","bone","bones","skeleton",
            "pterosaur","sauropod","theropod","mammoth","sabertooth","trilobite","amber","volcano","meteor","meteorite",
            "asteroid","comet","space","planet","planets","star","stars","galaxy","galaxies","universe","cosmos","cosmic",
            "moon","moons","sun","solar","orbit","orbiting","gravity","rocket","rockets","astronaut","astronauts","astronomy",
            "nasa","telescope","nebula","supernova","constellation","alien","aliens","mars","earth","venus","jupiter","saturn",
            "mercury","neptune","uranus","pluto","eclipse","crater","spaceship","spacecraft","satellite","light","lightyear",
            "wingspan","meteoroid","supergiant","dwarf","meteors","exoplanet","rover","probe","comets","stargazing","night","sky",
            "science","dinosaurs","creature","creatures","ancient","evolution","evolved","species","habitat","era","period",
            "big","biggest","fast","fastest","strong","strongest","tall","tallest","heavy","heaviest","small","smallest","long","longest"
        };

        private static readonly string[] VocabPhrases =
        {
            "black hole", "milky way", "light year", "big bang", "ice age", "saber tooth", "sabre tooth",
            "shooting star", "north star", "solar system", "outer space", "red planet", "night sky",
            "how big", "how fast", "how heavy", "how tall", "how long", "how strong", "how old", "how far", "how hot"
        };

        // ---------- helpers ----------
        private static bool Word(string hay, string word) => (" " + hay + " ").Contains(" " + word + " ");
        private static bool Phrase(string hay, string phrase) => (" " + hay + " ").Contains(phrase);
    }
}
