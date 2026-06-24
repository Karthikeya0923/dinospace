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
            sb.AppendLine("1. Keep answers to 1 to 3 short sentences. Give one answer, then stop.");
            sb.AppendLine("2. Stay calm and clear. Do not use emojis or exclamation marks.");
            sb.AppendLine("3. Only answer questions about dinosaurs, prehistoric creatures, and space.");
            sb.AppendLine("4. For anything else, reply with exactly: I can only help with dinosaurs and space.");
            sb.AppendLine("5. You can compare, reason, and answer fun or creative questions using what you know.");
            sb.AppendLine("6. Write the answer in your own words, like a person explaining what they learned. Never copy sentences.");
            if (grounded)
            {
                sb.AppendLine("7. The NOTES below are facts from the app's encyclopedia. Use them so your details stay correct, but do not repeat them word for word. When you give a measurement or number, use the exact value from the NOTES.");
                sb.AppendLine("8. If the NOTES do not cover something the question asks, fill it in from your own knowledge.");
            }
            sb.AppendLine();
            sb.AppendLine("Example 1:");
            sb.AppendLine("Question: How are stars made?");
            sb.AppendLine("Answer: Stars form when giant clouds of gas and dust pull together under gravity until they get hot enough to start shining.");
            sb.AppendLine("Example 2:");
            sb.AppendLine("Question: Could a T. Rex beat a Triceratops in a fight?");
            sb.AppendLine("Answer: It would be close. The T. Rex had a strong, crushing bite, but the Triceratops could fight back with three sharp horns and a thick neck frill.");
            sb.AppendLine("Example 3:");
            sb.AppendLine("Question: What is the best pizza topping?");
            sb.AppendLine("Answer: I can only help with dinosaurs and space.");
            sb.AppendLine();

            if (grounded)
            {
                sb.AppendLine("NOTES:");
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

            if (hits.Count == 0) return SuperlativeContext(q);

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

        // Handles "what is the biggest/fastest/etc. dinosaur" questions that name
        // no entry, by grounding them in the dinosaur that wins that stat.
        // Dinosaurs only - their stats share fields; space stats are too varied.
        private static string SuperlativeContext(string normalizedQuestion)
        {
            string q = " " + normalizedQuestion + " ";
            if (!q.Contains(" dinosaur ") && !q.Contains(" dino ") && !q.Contains(" dinosaurs "))
                return "";

            Func<Dinosaur, string> stat;
            bool max;

            if (HasWord(q, "biggest", "largest", "longest")) { stat = d => d.Length; max = true; }
            else if (HasWord(q, "smallest", "shortest")) { stat = d => d.Length; max = false; }
            else if (HasWord(q, "heaviest")) { stat = d => d.Weight; max = true; }
            else if (HasWord(q, "lightest")) { stat = d => d.Weight; max = false; }
            else if (HasWord(q, "tallest")) { stat = d => d.Height; max = true; }
            else if (HasWord(q, "fastest")) { stat = d => d.Speed; max = true; }
            else if (HasWord(q, "slowest")) { stat = d => d.Speed; max = false; }
            else if (HasWord(q, "strongest")) { stat = d => d.Strength.ToString(); max = true; }
            else return "";

            Dinosaur best = null;
            double bestVal = max ? double.MinValue : double.MaxValue;

            foreach (var d in DinosaurData.GetAll())
            {
                double? parsed = ParseLeadingNumber(stat(d));
                if (parsed == null) continue;
                double v = parsed.Value;
                if ((max && v > bestVal) || (!max && v < bestVal)) { bestVal = v; best = d; }
            }

            return best == null ? "" : DinoContext(best);
        }

        private static bool HasWord(string paddedQuestion, params string[] words)
        {
            foreach (var w in words)
                if (paddedQuestion.Contains(" " + w + " ")) return true;
            return false;
        }

        // Pulls the first number out of a stat string like "50 feet" or "7,500 kg".
        private static double? ParseLeadingNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var sb = new StringBuilder();
            bool started = false;
            foreach (char c in text.Replace(",", ""))
            {
                if (char.IsDigit(c) || (c == '.' && started)) { sb.Append(c); started = true; }
                else if (started) break;
            }
            if (sb.Length == 0) return null;
            return double.TryParse(sb.ToString(), out var val) ? val : (double?)null;
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