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
                return "Hey! I'm Nova. Ask me anything about dinosaurs or space — I love both.";

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

            // Only when the message is ESSENTIALLY nothing but thanks —
            // "canopus thanks" or "how far is Sirius thanks" carry a real
            // question and must not be brushed off with "you're welcome".
            {
                var tw = new HashSet<string> { "thank", "thanks", "thankyou", "thx", "ty", "you", "so", "much", "a", "lot", "very" };
                bool onlyThanks = (Phrase(q, "thank you") || Word(q, "thanks") || Word(q, "thx") || Word(q, "ty"))
                                  && q.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(tw.Contains);
                if (onlyThanks)
                    return "You're welcome! Want to ask another dinosaur or space question?";
            }

            if (Phrase(q, "who are you") || Phrase(q, "what are you") || Phrase(q, "your name") || Phrase(q, "who made you"))
                return "I'm Nova, the dinosaur and space guide inside DinoSpace. I run right on your device and I can answer questions about dinosaurs and outer space.";

            if (Phrase(q, "what can you do") || Phrase(q, "how do you work") || Phrase(q, "help") && q.Length <= 6)
                return "I answer questions about dinosaurs, prehistoric creatures, and space. Try me — how big was a Brachiosaurus? Why is Mars red?";

            // Note: q is already normalized (lowercased, punctuation and
            // apostrophes turned into spaces), so contractions arrive split —
            // "how's it going" -> "how s it going", "i'm bored" -> "i m bored".
            // The phrase literals below match those normalized forms.
            if (Phrase(q, "how are you") || Phrase(q, "how are u") || Phrase(q, "how s it going") || Phrase(q, "hows it going"))
                return "I'm great, thanks for asking! Ready to talk dinosaurs and space whenever you are.";

            if (Phrase(q, "are you real") || Phrase(q, "are you alive") || Phrase(q, "are you a robot") || Phrase(q, "are you human") || Phrase(q, "are you a person"))
                return "I'm a friendly helper that lives right inside this app — part dinosaur, part space explorer, all here to answer your questions! I'm not alive, but I love a good chat.";

            if (Phrase(q, "how old are you") || Phrase(q, "your age"))
                return "Older than the dinosaurs and younger than the stars — let's just say I'm timeless! Now, what would you like to explore?";

            if (Phrase(q, "where are you") || Phrase(q, "where do you live"))
                return "I live right here on your device — no internet needed! That means I'm always ready, wherever you are. Ask me anything about dinosaurs or space.";

            // A little personality: favourites. Kids love asking these.
            if (Phrase(q, "favourite dinosaur") || Phrase(q, "favorite dinosaur") || Phrase(q, "best dinosaur"))
                return "Tough call, but I have a soft spot for Triceratops — three horns, a giant frill, and totally chill munching plants. Who's YOUR favourite?";
            if (Phrase(q, "favourite planet") || Phrase(q, "favorite planet") || Phrase(q, "best planet"))
                return "Saturn, no contest — those rings are dazzling, and it's so light it would float in a giant bathtub! Which one do you like best?";
            if (Phrase(q, "favourite") || Phrase(q, "favorite"))
                return "I love it all — but if I had to pick, a Triceratops watching a Saturn-rise would be pretty magical. What's your favourite?";

            if (Phrase(q, "i love you") || Phrase(q, "love you") || Phrase(q, "you re cool") || Phrase(q, "youre cool") ||
                Phrase(q, "you re awesome") || Phrase(q, "youre awesome") || Phrase(q, "you re smart") || Phrase(q, "youre smart") ||
                Phrase(q, "you re the best") || Phrase(q, "youre the best") || Phrase(q, "you are cool") || Phrase(q, "you are awesome") ||
                Phrase(q, "you are the best") || Phrase(q, "i like you"))
                return "Aww, thank you! You're pretty stellar yourself. Let's discover something amazing together — ask me anything about dinosaurs or space!";

            if (Phrase(q, "i m bored") || Phrase(q, "im bored") || Phrase(q, "so bored") || Phrase(q, "entertain me"))
                return "Let's fix that! Ask me for a dinosaur joke, a space story, or something wild — like which dinosaur had the strongest bite, or why it rains diamonds on Neptune!";

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
