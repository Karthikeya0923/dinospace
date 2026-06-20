using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dinospace
{
    public static class RagService
    {
        public static string BuildPrompt(string question)
        {
            question = (question ?? "").Trim();
            string facts = FindContext(question);
            bool grounded = !string.IsNullOrEmpty(facts);

            var sb = new StringBuilder();

            sb.AppendLine("You are NovaSaur, a helper inside a dinosaur and space app for kids.");
            sb.AppendLine("Follow these rules:");
            sb.AppendLine("1. Keep answers to 1 to 3 short sentences. Be brief and clear. Give one answer, then stop.");
            sb.AppendLine("2. Stay calm and factual. Do not use emojis or exclamation marks.");
            sb.AppendLine("3. Only answer questions about dinosaurs, prehistoric creatures, and space.");
            sb.AppendLine("4. For anything else, reply with exactly: I can only help with dinosaurs and space.");
            sb.AppendLine("5. You can compare, reason, and answer fun or creative questions using what you know about dinosaurs and space.");
            if (grounded)
                sb.AppendLine("6. The FACTS below are the correct source for any stats, numbers, or details about the things mentioned. Keep those numbers exactly as written and never contradict them. Use your own knowledge for anything the facts do not cover.");
            sb.AppendLine();
            sb.AppendLine("Example 1:");
            sb.AppendLine("Question: How big was the T. Rex?");
            sb.AppendLine("Answer: The T. Rex was about 42 feet long and weighed around 7500 kg.");
            sb.AppendLine("Example 2:");
            sb.AppendLine("Question: Could a T. Rex beat a Triceratops in a fight?");
            sb.AppendLine("Answer: The T. Rex had a powerful bone-crushing bite, but the Triceratops had three sharp horns and a thick frill to defend itself. It would be a close fight, and a healthy Triceratops could often hold off a T. Rex.");
            sb.AppendLine("Example 3:");
            sb.AppendLine("Question: How are stars made?");
            sb.AppendLine("Answer: Stars form when giant clouds of gas and dust collapse under gravity until they get hot enough for nuclear fusion to begin, which makes them shine.");
            sb.AppendLine("Example 4:");
            sb.AppendLine("Question: What is the best pizza topping?");
            sb.AppendLine("Answer: I can only help with dinosaurs and space.");
            sb.AppendLine();

            if (grounded)
            {
                sb.AppendLine("FACTS:");
                sb.AppendLine(facts);
            }

            sb.AppendLine("Question: " + question);
            sb.Append("Answer:");

            return sb.ToString();
        }

        // Cleans the model's raw output: strips an echoed "Answer:" and cuts off
        // anything after it tries to start a new turn.
        public static string CleanAnswer(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string text = raw.Trim();

            int cut = text.IndexOf("Question:", StringComparison.OrdinalIgnoreCase);
            if (cut > 0) text = text.Substring(0, cut).Trim();

            if (text.StartsWith("Answer:", StringComparison.OrdinalIgnoreCase))
                text = text.Substring("Answer:".Length).Trim();

            return text;
        }

        // Collect EVERY entry named in the question (top 3, most specific first),
        // so comparison questions get all the relevant stats.
        private static string FindContext(string question)
        {
            string q = Normalize(question);
            var hits = new List<(int score, string text)>();

            foreach (var d in DinosaurData.GetAll())
            {
                int score = MatchScore(q, d.Name, DinoAliases(d.Name));
                if (score > 0) hits.Add((score, DinoContext(d)));
            }

            foreach (var s in SpaceData.GetAll())
            {
                int score = MatchScore(q, s.Name, SpaceAliases(s.Name));
                if (score > 0) hits.Add((score, SpaceContext(s)));
            }

            if (hits.Count == 0) return "";

            var top = hits.OrderByDescending(h => h.score).Take(3).Select(h => h.text);
            return string.Join("\n", top);
        }

        private static int MatchScore(string normalizedQuestion, string name, IEnumerable<string> aliases)
        {
            string padded = " " + normalizedQuestion + " ";
            int best = 0;

            var keys = new List<string> { Normalize(name) };
            keys.AddRange(aliases.Select(Normalize));

            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (padded.Contains(" " + key + " ") && key.Length > best)
                    best = key.Length;
            }
            return best;
        }

        private static IEnumerable<string> DinoAliases(string name) => name switch
        {
            "T. Rex" => new[] { "trex", "t rex", "tyrannosaurus", "tyrannosaurus rex", "rex" },
            _ => new string[0]
        };

        private static IEnumerable<string> SpaceAliases(string name) => name switch
        {
            "Andromeda Galaxy" => new[] { "andromeda" },
            "Phoenix A*" => new[] { "phoenix", "phoenix a" },
            _ => new string[0]
        };

        private static string DinoContext(Dinosaur d)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name: " + d.Name);
            Add(sb, "Meaning", d.Meaning);
            Add(sb, "Lived", d.Era);
            Add(sb, "Type", d.Group);
            Add(sb, "Diet", d.Diet);
            Add(sb, "Length", d.Length);
            Add(sb, "Height", d.Height);
            Add(sb, "Width", d.Width);
            Add(sb, "Weight", d.Weight);
            Add(sb, "Speed", d.Speed);
            Add(sb, "About", d.AboutText);
            Add(sb, "Features", d.KeyFeaturesText);
            Add(sb, "Habitat", d.LifeEnvironmentText);
            Add(sb, "Behaviour", d.BehaviourText);
            Add(sb, "Fun facts", d.FunFactsText);
            return sb.ToString();
        }

        private static string SpaceContext(SpaceObject s)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name: " + s.Name);
            Add(sb, "Type", s.TypeLabel);
            Add(sb, "Category", s.Category);
            Add(sb, s.Stat1Label, s.Stat1Value);
            Add(sb, s.Stat2Label, s.Stat2Value);
            Add(sb, s.Stat3Label, s.Stat3Value);
            Add(sb, s.Stat4Label, s.Stat4Value);
            Add(sb, "About", s.AboutText);
            Add(sb, "Features", s.KeyFeaturesText);
            Add(sb, "Orbit and movement", s.OrbitMovementText);
            Add(sb, "Surface", s.SurfaceCompositionText);
            Add(sb, "History", s.HistoryText);
            Add(sb, "What's inside", s.WhatsInsideText);
            Add(sb, "Fun facts", s.FunFactsText);
            return sb.ToString();
        }

        private static void Add(StringBuilder sb, string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                sb.AppendLine(label + ": " + value);
        }

        private static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder();
            foreach (char c in text.ToLowerInvariant())
                sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            return string.Join(" ", sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}