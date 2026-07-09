using System;
using System.Collections.Generic;
using System.Linq;
using dinospace.Data;
using dinospace.Models;

namespace dinospace.Services
{
    // The offline "imagination" side of NovaSaur. LocalAnswer handles the
    // vetted facts; this handles everything open-ended — jokes, little stories,
    // poems, "what if", and the graceful catch-all for a question nothing else
    // recognised. It ALWAYS returns a friendly, on-brand reply, so the chat
    // works fully offline with no model download: a kid can ask anything and
    // NovaSaur answers.
    //
    // Everything here is templated from real encyclopedia data, so even the
    // playful answers stay accurate and safe.
    public static class NovaCreative
    {
        private static readonly Random _rng = new();

        // Instant, always-fun replies that shouldn't depend on the encyclopedia
        // or the model: jokes and "tell me a fact". Returns null if the question
        // isn't one of those. Called early by PromptBuilder so a running
        // conversation's carryover can't hijack "tell me a joke".
        public static string? QuickReply(string q)
        {
            if (IsJoke(q)) return Joke();
            if (WantsFact(q)) return "Here's a cool one: " + FactData.Random() + " Want another, or shall we dig into a dinosaur or planet?";
            return null;
        }

        // The entry point PromptBuilder uses for its offline fallback (open-ended
        // questions the encyclopedia didn't answer).
        public static string Answer(string question, string q, Grounding g, bool creative)
        {
            var quick = QuickReply(q);
            if (quick != null) return quick;

            if (creative)
            {
                if (Has(q, "poem", "poems", "rhyme")) return Poem(Subject(g));
                if (Has(q, "song", "sing", "rap", "rhyme")) return Song(Subject(g));
                if (Has(q, "story", "stories", "tale", "adventure")) return Story(Subject(g), Second(g));
                if (Has(q, "what if", "imagine", "pretend", "would happen")) return Imagine(question, Subject(g));
                // "make up", "invent", "write me..." — a story is the safest bet.
                return Story(Subject(g), Second(g));
            }

            // Not creative, not a recognised fact: answer as helpfully as we can
            // and keep the child pointed back at something we DO know well.
            return Fallback(g);
        }

        // ---------- subject picking ----------

        // The entity the question named, or a random one so "tell me a story"
        // with no subject still gets a hero.
        private static object Subject(Grounding g)
        {
            foreach (var name in g.Entities)
            {
                var d = DinoData.ByName(name); if (d != null) return d;
                var s = SpaceData.ByName(name); if (s != null) return s;
            }
            return _rng.Next(2) == 0
                ? (object)DinoData.All[_rng.Next(DinoData.All.Count)]
                : SpaceData.All[_rng.Next(SpaceData.All.Count)];
        }

        // A second, different hero for two-character stories.
        private static object Second(Grounding g)
        {
            foreach (var name in g.Entities.Skip(1))
            {
                var d = DinoData.ByName(name); if (d != null) return d;
                var s = SpaceData.ByName(name); if (s != null) return s;
            }
            // Pair a dino with a space place (and vice versa) for a fun contrast.
            return _rng.Next(2) == 0
                ? (object)DinoData.All[_rng.Next(DinoData.All.Count)]
                : SpaceData.All[_rng.Next(SpaceData.All.Count)];
        }

        private static string NameOf(object o) => o switch
        {
            Dinosaur d => d.Name,
            SpaceObject s => s.Name,
            _ => "a dinosaur"
        };

        // A short, true detail we can drop into a story to keep it grounded.
        private static string FactOf(object o) => o switch
        {
            Dinosaur d => FirstFact(d.FunFactsText, d.ShortDescription),
            SpaceObject s => FirstFact(s.FunFactsText, s.ShortDescription),
            _ => ""
        };

        // ---------- stories ----------

        private static string Story(object hero, object other)
        {
            string h = NameOf(hero), o = NameOf(other);
            if (h == o) o = "a shiny new rocket";
            string fact = FactOf(hero);
            string factLine = fact.Length > 0 ? $" (Did you know? {fact}) " : " ";

            string[] openers =
            {
                $"One bright morning, {h} woke up feeling extra brave.",
                $"Long, long ago — and also somehow in space — {h} set off on a big adventure.",
                $"Once upon a time, {h} found a glowing map that led straight past the stars.",
                $"It all started when {h} heard a mysterious rumble coming from beyond the clouds.",
            };
            string[] middles =
            {
                $"With a mighty leap, {h} zoomed off to meet {o}, dodging asteroids and giggling the whole way.",
                $"Along the way, {h} teamed up with {o}, and together they solved a puzzle written in twinkling starlight.",
                $"{h} and {o} raced comets, built a fort out of moon dust, and shared a snack of space berries.",
                $"When trouble bubbled up, {h} stayed calm, thought hard, and showed {o} a clever trick.",
            };
            string[] closers =
            {
                "They high-fived (well, high-clawed) and promised to explore again tomorrow. The end!",
                "And that's how the bravest explorer in the universe made a brand-new friend. The end!",
                "By bedtime, they'd mapped a whole new galaxy — and named a star after themselves. The end!",
                "Everyone cheered, the stars sparkled a little brighter, and home never felt so cosy. The end!",
            };
            return Pick(openers) + factLine + Pick(middles) + " " + Pick(closers);
        }

        // ---------- poems ----------

        private static string Poem(object subject)
        {
            string n = NameOf(subject);
            string[] poems =
            {
                $"Up in the dark where the comets fly,\n{n} goes dancing across the sky.\nStars for a blanket, the moon for a light —\nDreaming of wonders all through the night.",
                $"{n}, {n}, brave and bold,\nBrighter than treasure, more precious than gold.\nOff on a journey no map can show,\nWherever the sparkling star-winds blow.",
                $"A rumble, a roar, a flash of light —\n{n} takes off into the night.\nPast the planets, round the Sun,\nExploring space is ever so fun!",
            };
            return Pick(poems);
        }

        // ---------- songs ----------

        private static string Song(object subject)
        {
            string n = NameOf(subject);
            string[] songs =
            {
                $"🎵 Oh, {n}, {n}, stomping through the stars,\nWaving to the comets, saying hi to Mars!\nSpin around the galaxy, never say goodbye —\n{n}'s the coolest explorer in the sky! 🎵",
                $"🎵 We're going on an adventure, me and {n} too,\nUp past the Moon where the sky turns blue,\nCounting all the shooting stars, one, two, three —\nThere's a whole big universe for you and me! 🎵",
            };
            return Pick(songs);
        }

        // ---------- imagine / what-if ----------

        private static string Imagine(string question, object subject)
        {
            string n = NameOf(subject);
            string q = question.ToLowerInvariant();

            if (q.Contains("dinosaur") && (q.Contains("never") || q.Contains("didn't") || q.Contains("still")) && q.Contains("extinct"))
                return "Ooh, fun to imagine! If that asteroid had missed, some dinosaurs might have kept evolving for millions more years — maybe clever, warm-blooded ones. There might have been no room for big mammals like us, so the world would look VERY different. In a way they DID survive, though: birds are living dinosaurs!";

            // "live on mars/the moon/in space" — all three place-words must be
            // paired with the living part, or plain moon questions ("what if
            // the moon disappeared") wrongly got the bubble-dome speech.
            if ((q.Contains("mars") || q.Contains("moon") || q.Contains("space")) && q.Contains("live"))
                return "Imagine that! To live out there we'd need a bubble-dome full of air, water melted from ice, and food grown under bright lamps. It would be chilly, low-gravity, and the sky would be a different colour — but what an adventure. Scientists are working on it for real!";

            return $"What a brilliant 'what if'! Let's imagine {n} in that story: it might explore, make friends, and discover something no one has ever seen. The best part of space and dinosaurs is that there are still SO many mysteries left to solve. What do YOU think would happen?";
        }

        // ---------- jokes ----------

        private static int _jokeIndex = -1;
        private static string Joke()
        {
            // Rotate so you don't get the same joke twice in a row.
            int i = _jokeIndex;
            while (i == _jokeIndex) i = _rng.Next(Jokes.Length);
            _jokeIndex = i;
            return Jokes[i];
        }

        private static readonly string[] Jokes =
        {
            "What do you call a dinosaur that is sleeping? A dino-SNORE! 😴",
            "Why can't you hear a pterodactyl going to the bathroom? Because the 'P' is silent!",
            "What do you call a dinosaur with an amazing vocabulary? A thesaurus!",
            "How do you know if there's a dinosaur under your bed? Your nose touches the ceiling!",
            "Why did the astronaut break up with the star? She needed a little space.",
            "How does the Moon cut its hair? Eclipse it!",
            "What kind of music do planets like? Neptunes!",
            "Why did the Sun go to school? To get a little brighter!",
            "What do you call a dinosaur that never gives up? A try-try-try-ceratops!",
            "What's a dinosaur's least favourite reindeer? Comet — he keeps flying away!",
            "How do you throw a space party? You planet!",
            "What do you call a nervous T. Rex? A nervous-rex!",
            "Why don't dinosaurs ever pay for anything? Because they're all extinct... of money!",
            "What did the astronaut cook for lunch? Something un-FLAT-tering — it was out of this world!",
            "What do you get when a dinosaur walks through the strawberry patch? Strawberry jam!",
            "Why is the Moon so happy? It's going through a phase!",
            "What do you call a sleeping T. Rex on the Moon? A dino-snore in zero-snore gravity!",
        };

        // ---------- graceful catch-all ----------

        private static string Fallback(Grounding g)
        {
            // If a real entity was named, lean on it — better than a shrug.
            foreach (var name in g.Entities)
            {
                var d = DinoData.ByName(name);
                if (d != null) return $"Great question about {d.Name}! {Intro(d.AboutText, d.ShortDescription)} Want to know its size, its diet, or when it lived?";
                var s = SpaceData.ByName(name);
                if (s != null) return $"Great question about {s.Name}! {Intro(s.AboutText, s.ShortDescription)} Ask me how big it is, how far away, or what it's made of!";
            }

            // Truly unknown: stay honest, stay warm, hand back a real fact so the
            // reply is never a dead end.
            string[] leads =
            {
                "That's a tricky one, and I want to be honest — I'm best with dinosaurs and space, so I'm not totally sure about that.",
                "Hmm, that's outside my rocket range! I'm your dinosaur and space guide, so I might not have that exact answer.",
                "Ooh, good question! I stick to dinosaurs and space, so I'm not certain about that one.",
            };
            return $"{Pick(leads)} But here's something cool while you're here: {FactData.Random()} Want to ask me about a dinosaur or a planet?";
        }

        // ---------- helpers ----------

        private static bool IsJoke(string q) =>
            Has(q, "joke", "jokes", "funny", "make me laugh", "tell me something funny", "haha");

        private static bool WantsFact(string q) =>
            Has(q, "fun fact", "fun facts", "a fact", "another fact", "random fact", "cool fact",
                   "did you know", "something cool", "something interesting", "something amazing",
                   "surprise me", "tell me something");

        private static string Intro(string about, string fallback)
        {
            string s = FirstSentence(about);
            if (s.Length > 0) return s;
            return string.IsNullOrWhiteSpace(fallback) ? "" : fallback.Trim().TrimEnd('.') + ".";
        }

        private static string FirstFact(string funFacts, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(funFacts))
            {
                var first = funFacts.Split('\n').FirstOrDefault(l => l.Trim().Length > 0);
                if (first != null) return first.TrimStart('•', ' ').Trim();
            }
            return string.IsNullOrWhiteSpace(fallback) ? "" : fallback.Trim();
        }

        private static string FirstSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            int i = text.IndexOfAny(new[] { '.', '!', '?' });
            return (i > 0 ? text[..(i + 1)] : text).Trim();
        }

        private static bool Has(string q, params string[] words)
        {
            string p = " " + q + " ";
            return words.Any(w => w.Contains(' ') ? p.Contains(w) : p.Contains(" " + w + " "));
        }

        private static string Pick(string[] items) => items[_rng.Next(items.Length)];
    }
}
