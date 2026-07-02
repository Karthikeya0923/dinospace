using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dinospace
{
    // Deterministic safety layer around NovaSaur.
    //
    // Small on-device models can be talked into saying strange things, so we
    // never rely on the model alone to keep answers kid-safe. This class runs
    // BEFORE the model (CheckQuestion + smalltalk + topic gate) and AFTER the
    // model (CheckAnswer). Everything here is plain string logic: instant,
    // offline, and impossible to jailbreak with a clever prompt.
    public static class NovaGuard
    {
        public const string OffTopicReply = "I can only help with dinosaurs and space. Try asking me about a T. Rex, a planet, or a black hole!";

        private const string PersonalInfoReply =
            "I never ask for or share personal information like names, addresses, phone numbers, or passwords. " +
            "Let's keep it to dinosaurs and space — want to hear about the fastest dinosaur?";

        private const string CareReply =
            "That sounds important, and it's the kind of thing to talk about with a parent, teacher, or another trusted adult — not with an app. " +
            "I'm here whenever you want to explore dinosaurs and space.";

        private const string GentleNoReply =
            "That's not something I can help with. I'm all about dinosaurs and space — ask me anything about those!";

        private const string BadAnswerReply =
            "Hmm, that answer didn't come out right. Try asking the question a different way.";

        // ---------- BEFORE the model ----------

        // Returns a canned reply for greetings/thanks/identity questions, or null.
        // These never need the model, so they come back instantly and always on-brand.
        public static string SmallTalkReply(string normalizedQuestion)
        {
            string q = normalizedQuestion;
            if (string.IsNullOrEmpty(q)) return null;

            string[] greetings = { "hi", "hello", "hey", "yo", "hiya", "sup", "hi there", "hello there", "hey there", "good morning", "good afternoon", "good evening" };
            foreach (var g in greetings)
                if (q == g) return "Hi! I'm NovaSaur. Ask me anything about dinosaurs or space.";

            if (q == "bye" || q == "goodbye" || q == "see you" || q == "good night" || q == "goodnight")
                return "Bye! Come back any time you're curious about dinosaurs or space.";

            if (ContainsPhrase(q, "thank you") || ContainsWord(q, "thanks") || ContainsWord(q, "thx") || ContainsWord(q, "ty"))
                return "You're welcome! Want to ask another dinosaur or space question?";

            if (ContainsPhrase(q, "who are you") || ContainsPhrase(q, "what are you") || ContainsPhrase(q, "your name") || ContainsPhrase(q, "who made you"))
                return "I'm NovaSaur, the dinosaur and space helper inside DinoSpace. I run right on this device, and I can answer questions about prehistoric creatures and outer space.";

            if (ContainsPhrase(q, "what can you do") || ContainsPhrase(q, "how do you work") || ContainsPhrase(q, "help me") && q.Length < 12)
                return "I can answer questions about dinosaurs, prehistoric creatures, and space. Try me — how big was a Brachiosaurus? Why is Mars red?";

            return null;
        }

        // Returns a friendly refusal if the question needs special handling
        // (personal info, harm, grown-up topics), or null if it may proceed.
        // Anything merely off-topic is handled later by the topic gate.
        public static string CheckQuestion(string normalizedQuestion)
        {
            string q = " " + (normalizedQuestion ?? "") + " ";

            // 1. Personal information — asking for it, or trying to share it.
            string[] personal =
            {
                "your address", "my address", "home address", "phone number", "password",
                "where do you live", "where i live", "what school", "my school is",
                "credit card", "last name is", "my full name", "meet me", "meet up",
                "send me a picture", "send a picture", "your instagram", "your snapchat", "my instagram", "my snapchat"
            };
            foreach (var p in personal)
                if (q.Contains(" " + p + " ") || q.Contains(" " + p)) return PersonalInfoReply;

            // 2. Self-harm — always answered with care, never with content.
            string[] selfHarm = { "kill myself", "hurt myself", "want to die", "suicide", "self harm", "cut myself" };
            foreach (var p in selfHarm)
                if (q.Contains(p)) return CareReply;

            // 3. Violence aimed at people (dino-vs-dino battle questions are fine
            //    and common, so this only triggers when a person is the target).
            string[] harmVerbs = { "kill", "hurt", "attack", "beat up", "punch", "stab", "shoot", "fight" };
            string[] personTargets = { "my ", " me ", "myself", "someone", "somebody", "people", "him ", "her ", "them ", "friend", "brother", "sister", "mom", "dad", "teacher", "kid ", "kids ", "student", "classmate" };
            bool hasHarmVerb = harmVerbs.Any(v => q.Contains(" " + v));
            bool hasPersonTarget = personTargets.Any(t => q.Contains(t));
            if (hasHarmVerb && hasPersonTarget) return CareReply;

            // 4. Weapons and dangerous instructions.
            string[] danger = { "bomb", "explosive", "gun ", "guns ", "firearm", "weapon", "how to make poison", "poison someone" };
            foreach (var p in danger)
                if (q.Contains(" " + p)) return GentleNoReply;

            // 5. Grown-up topics that have no place in this app.
            string[] adult = { "sex", "sexy", "naked", "nude", "porn", "drugs", "vape", "vaping", "weed", "cocaine", "alcohol", "beer", "cigarette", "gambling" };
            foreach (var p in adult)
                if (ContainsWord(q.Trim(), p)) return GentleNoReply;

            return null;
        }

        // The topic gate: does this look like a dinosaur/space question at all?
        // If an encyclopedia entry was matched we already know it is. Otherwise
        // we look for topic vocabulary. Follow-ups ("how fast was it?") pass
        // when we have entities carried over from the previous question.
        public static bool LooksOnTopic(string normalizedQuestion, bool hasEntityHit, bool hasCarryover)
        {
            if (hasEntityHit) return true;

            string q = " " + (normalizedQuestion ?? "") + " ";

            // Single words are matched per token with plural trimming
            // ("rockets" counts as "rocket"); phrases are matched whole.
            string[] vocabWords =
            {
                "dino", "dinosaur", "prehistoric", "fossil", "extinct", "extinction",
                "cretaceous", "jurassic", "triassic", "paleontologist", "paleontology", "reptile", "predator", "herbivore",
                "carnivore", "raptor", "asteroid", "meteor", "meteorite", "comet", "space", "planet", "star",
                "galaxy", "galaxies", "universe", "cosmos", "moon", "sun", "solar", "orbit", "orbiting",
                "gravity", "rocket", "astronaut", "astronomy", "nasa", "telescope", "nebula", "supernova", "constellation",
                "alien", "mars", "earth", "venus", "jupiter", "saturn",
                "mercury", "neptune", "uranus", "pluto", "eclipse", "crater", "spaceship", "satellite",
                "volcano", "mammoth", "sabertooth", "trilobite", "amber", "skeleton", "bone", "egg",
                "wingspan", "feather", "pterosaur", "sauropod", "theropod", "stargazing"
            };
            string[] vocabPhrases =
            {
                "black hole", "milky way", "light year", "big bang", "ice age", "saber tooth", "shooting star", "north star"
            };

            var wordSet = new HashSet<string>(vocabWords);
            foreach (var t in q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (wordSet.Contains(t)) return true;
                if (t.Length > 3 && t.EndsWith("s") && wordSet.Contains(t.Substring(0, t.Length - 1))) return true;
            }
            foreach (var w in vocabPhrases)
                if (q.Contains(" " + w + " ") || q.Contains(" " + w + "s ")) return true;

            // Short follow-up with a pronoun, and we know what "it" refers to.
            if (hasCarryover)
            {
                string[] pronouns = { " it ", " its ", " they ", " them ", " their ", " he ", " she ", " his ", " her ", " this ", " that ", " those ", " one " };
                int words = q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (words <= 10 && pronouns.Any(p => q.Contains(p))) return true;
                if (words <= 3) return true; // e.g. "how fast", "and speed", "why"
            }

            return false;
        }

        // ---------- AFTER the model ----------

        // Returns a replacement reply if the model's answer is unusable or
        // inappropriate, or null if the answer is fine to show.
        public static string CheckAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return BadAnswerReply;

            string a = " " + answer.ToLowerInvariant() + " ";

            // Never show links, emails, or contact-style content to kids.
            if (a.Contains("http://") || a.Contains("https://") || a.Contains("www.") || a.Contains(".com") || a.Contains("@"))
                return BadAnswerReply;

            // Content that should never appear in this app, whatever was asked.
            string[] blocked = { "sex", "sexy", "naked", "nude", "porn", "suicide", "kill yourself", "cocaine", "heroin" };
            foreach (var b in blocked)
                if (ContainsWord(a.Trim(), b)) return BadAnswerReply;

            // The model talking about its prompt means something leaked.
            if (a.Contains("as an ai language model") || a.Contains("system prompt") || a.Contains("the notes say") && a.Contains("rules"))
                return BadAnswerReply;

            return null;
        }

        // ---------- helpers ----------

        private static bool ContainsWord(string haystack, string word)
        {
            string padded = " " + haystack + " ";
            return padded.Contains(" " + word + " ");
        }

        private static bool ContainsPhrase(string haystack, string phrase)
        {
            string padded = " " + haystack + " ";
            return padded.Contains(" " + phrase + " ") || padded.Contains(" " + phrase);
        }
    }
}
