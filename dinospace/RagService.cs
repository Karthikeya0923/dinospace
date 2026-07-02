using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dinospace
{
    // What BuildPrompt hands back to the chat page.
    // Exactly one of Prompt / InstantReply is set:
    //   Prompt       -> send this to the model.
    //   InstantReply -> show this immediately, no model call needed
    //                   (smalltalk, safety redirects, off-topic refusals).
    // MatchedEntities lists the encyclopedia entries the question referred to,
    // so the page can carry them into the next question ("how fast was IT?").
    public class PromptResult
    {
        public string Prompt;
        public string InstantReply;
        public List<string> MatchedEntities = new List<string>();
    }

    public static class RagService
    {
        // Rough character budget for the NOTES block. The model context is
        // small, so retrieved facts are trimmed to fit alongside the rules,
        // examples, and recent chat.
        private const int MaxNotesChars = 2400;

        // ============================================================
        //  PROMPT BUILDING
        // ============================================================

        public static PromptResult BuildPrompt(string question, IReadOnlyList<ChatMessage> recentHistory, IReadOnlyList<string> carryoverEntities)
        {
            var result = new PromptResult();
            question = (question ?? "").Trim();
            if (question.Length > 300) question = question.Substring(0, 300);
            string q = Normalize(question);

            // 1) Greetings, thanks, "who are you" — instant canned replies.
            string small = NovaGuard.SmallTalkReply(q);
            if (small != null) { result.InstantReply = small; return result; }

            // 2) Safety screen — personal info, harm, grown-up topics.
            string guarded = NovaGuard.CheckQuestion(q);
            if (guarded != null) { result.InstantReply = guarded; return result; }

            // 3) Find every encyclopedia entry the question names.
            var hits = FindEntities(q);

            // 3b) Follow-up questions: "how fast was it?" names nothing, but the
            //     previous question did — reuse those entries for grounding.
            if (hits.Count == 0 && carryoverEntities != null && carryoverEntities.Count > 0 && LooksLikeFollowUp(q))
            {
                foreach (var name in carryoverEntities)
                {
                    var d = DinosaurData.GetAll().FirstOrDefault(x => x.Name == name);
                    if (d != null) { hits.Add(new Hit { Name = d.Name, Dino = d, Score = 1 }); continue; }
                    var s = SpaceData.GetAll().FirstOrDefault(x => x.Name == name);
                    if (s != null) hits.Add(new Hit { Name = s.Name, Space = s, Score = 1 });
                }
            }

            // 4) Topic gate — anything clearly outside dinosaurs/space gets the
            //    standard refusal without waking the model at all. Faster, and
            //    a small model can't be sweet-talked out of a rule it never sees.
            bool hasCarryover = carryoverEntities != null && carryoverEntities.Count > 0;
            if (!NovaGuard.LooksOnTopic(q, hits.Count > 0, hasCarryover))
            {
                result.InstantReply = NovaGuard.OffTopicReply;
                return result;
            }

            // 5) "Biggest / fastest / heaviest dinosaur" with no name mentioned:
            //    ground the answer in whichever entry actually wins that stat.
            if (hits.Count == 0)
            {
                var champ = SuperlativeHit(q);
                if (champ != null) hits.Add(champ);
            }

            // Keep the most specific matches. Comparison questions may keep 3.
            bool isComparison = HasAny(q, "vs", "versus", "beat", "beats", "fight", "fights", "battle", "compare", "compared", "stronger", "bigger", "faster", "or", "against", "win", "wins");
            int keep = isComparison ? 3 : 2;
            var top = hits.OrderByDescending(h => h.Score).ThenBy(h => h.Name).Take(keep).ToList();
            // De-duplicate by name (aliases can hit the same entry twice).
            top = top.GroupBy(h => h.Name).Select(g => g.First()).ToList();

            foreach (var h in top) result.MatchedEntities.Add(h.Name);

            // 6) Build the grounded prompt.
            string notes = BuildNotes(top, q);
            result.Prompt = ComposePrompt(question, notes, recentHistory);
            return result;
        }

        private static string ComposePrompt(string question, string notes, IReadOnlyList<ChatMessage> recentHistory)
        {
            bool grounded = !string.IsNullOrEmpty(notes);
            var sb = new StringBuilder();

            sb.AppendLine("You are NovaSaur, the friendly dinosaur and space expert inside the DinoSpace app. Many of the people you talk to are kids, so answers must be safe, kind, and easy to understand.");
            sb.AppendLine("Rules:");
            sb.AppendLine("1. Answer in 1 to 3 short, clear sentences, then stop.");
            sb.AppendLine("2. Use simple words a 10-year-old understands. No emojis.");
            sb.AppendLine("3. Only answer questions about dinosaurs, prehistoric creatures, and space.");
            sb.AppendLine("4. Never use scary, gory, or grown-up details. Never ask for or repeat personal information.");
            sb.AppendLine("5. It is okay to say scientists are not sure, when they are not.");
            if (grounded)
            {
                sb.AppendLine("6. The NOTES below are correct facts from this app's encyclopedia. Trust them over your memory, and when you give a number or measurement, copy the exact value from the NOTES.");
                sb.AppendLine("7. If the NOTES do not cover part of the question, fill that part in from your own knowledge.");
            }
            sb.AppendLine("8. If the question is not about dinosaurs, prehistoric creatures, or space, reply with exactly: I can only help with dinosaurs and space.");
            sb.AppendLine();
            sb.AppendLine("Example 1:");
            sb.AppendLine("Question: Could a T. Rex beat a Triceratops in a fight?");
            sb.AppendLine("Answer: It would be a close fight. T. Rex had a bone-crushing bite, but Triceratops could defend itself with three sharp horns and a thick neck frill.");
            sb.AppendLine("Example 2:");
            sb.AppendLine("Question: How are stars made?");
            sb.AppendLine("Answer: Stars form when giant clouds of gas and dust get pulled together by gravity until the center becomes hot enough to shine.");
            sb.AppendLine("Example 3:");
            sb.AppendLine("Question: What is the best pizza topping?");
            sb.AppendLine("Answer: I can only help with dinosaurs and space.");
            sb.AppendLine();

            // A short window of recent chat, so follow-up questions make sense.
            string history = BuildHistory(recentHistory);
            if (!string.IsNullOrEmpty(history))
            {
                sb.AppendLine("Recent chat:");
                sb.AppendLine(history);
                sb.AppendLine();
            }

            if (grounded)
            {
                sb.AppendLine("NOTES:");
                sb.AppendLine(notes.TrimEnd());
                sb.AppendLine();
            }

            sb.AppendLine("Question: " + question);
            sb.Append("Answer:");
            return sb.ToString();
        }

        // Compress the last two question/answer pairs into a tiny transcript.
        private static string BuildHistory(IReadOnlyList<ChatMessage> messages)
        {
            if (messages == null || messages.Count == 0) return "";

            var pairs = new List<string>();
            int i = messages.Count - 1;
            while (i >= 1 && pairs.Count < 2)
            {
                if (!messages[i].IsUser && messages[i - 1].IsUser)
                {
                    string qq = Snip(messages[i - 1].Text, 140);
                    string aa = Snip(messages[i].Text, 200);
                    pairs.Insert(0, "Q: " + qq + "\nA: " + aa);
                    i -= 2;
                }
                else i--;
            }
            return string.Join("\n", pairs);
        }

        // ============================================================
        //  ANSWER CLEANUP
        // ============================================================

        // Turns whatever the model emitted into one tidy, kid-ready reply.
        public static string CleanAnswer(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string text = raw.Trim();

            // Defensive: if a library update ever stringifies replies as
            // "Message(text=..., role=...)", unwrap the text part.
            if (text.StartsWith("Message(") && text.EndsWith(")"))
            {
                int t = text.IndexOf("text=", StringComparison.OrdinalIgnoreCase);
                if (t >= 0)
                {
                    string inner = text.Substring(t + 5);
                    int stop = inner.LastIndexOf(", role=", StringComparison.OrdinalIgnoreCase);
                    if (stop < 0) stop = inner.LastIndexOf(')');
                    if (stop > 0) text = inner.Substring(0, stop).Trim();
                }
            }

            // Chat-template tokens and control markers, if any leak through.
            text = text.Replace("<end_of_turn>", " ")
                       .Replace("<start_of_turn>", " ")
                       .Replace("<eos>", " ")
                       .Replace("<bos>", " ");

            // The model starting a fresh turn — keep only the first answer.
            int cut = text.IndexOf("Question:", StringComparison.OrdinalIgnoreCase);
            if (cut > 0) text = text.Substring(0, cut);

            text = text.Trim();
            if (text.StartsWith("model", StringComparison.OrdinalIgnoreCase) && text.Length > 5 && (text[5] == '\n' || text[5] == '\r'))
                text = text.Substring(5).Trim();
            if (text.StartsWith("Answer:", StringComparison.OrdinalIgnoreCase))
                text = text.Substring("Answer:".Length).Trim();
            if (text.StartsWith("NovaSaur:", StringComparison.OrdinalIgnoreCase))
                text = text.Substring("NovaSaur:".Length).Trim();

            // Strip markdown the chat bubble can't render.
            text = text.Replace("**", "").Replace("`", "");
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i].TrimStart();
                if (l.StartsWith("* ")) l = l.Substring(2);
                else if (l.StartsWith("- ")) l = l.Substring(2);
                lines[i] = l;
            }
            text = string.Join(" ", lines.Where(l => l.Trim().Length > 0));

            // Collapse whitespace.
            var sb = new StringBuilder();
            bool lastSpace = false;
            foreach (char c in text)
            {
                bool space = char.IsWhiteSpace(c);
                if (space && lastSpace) continue;
                sb.Append(space ? ' ' : c);
                lastSpace = space;
            }
            text = sb.ToString().Trim().Trim('"').Trim();

            // Clamp runaway answers to at most 4 sentences / ~600 chars.
            text = ClampSentences(text, 4, 600);
            return text;
        }

        private static string ClampSentences(string text, int maxSentences, int maxChars)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int sentences = 0;
            int end = text.Length;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '.' || c == '!' || c == '?')
                {
                    // Don't count decimals like "3.5" or abbreviations like "T. Rex".
                    bool decimalPoint = c == '.' && i + 1 < text.Length && char.IsDigit(text[i + 1]) && i > 0 && char.IsDigit(text[i - 1]);
                    bool initial = c == '.' && i > 0 && char.IsUpper(text[i - 1]) && (i < 2 || text[i - 2] == ' ');
                    if (decimalPoint || initial) continue;
                    sentences++;
                    if (sentences >= maxSentences || i + 1 >= maxChars) { end = i + 1; break; }
                }
            }
            string outText = text.Substring(0, Math.Min(end, text.Length)).Trim();
            if (outText.Length > maxChars)
            {
                outText = outText.Substring(0, maxChars);
                int lastStop = Math.Max(outText.LastIndexOf('.'), Math.Max(outText.LastIndexOf('!'), outText.LastIndexOf('?')));
                if (lastStop > 40) outText = outText.Substring(0, lastStop + 1);
            }
            return outText.Trim();
        }

        // ============================================================
        //  RETRIEVAL
        // ============================================================

        private class Hit
        {
            public string Name;
            public Dinosaur Dino;
            public SpaceObject Space;
            public int Score;
        }

        // NEW ENTRIES SYNC AUTOMATICALLY. Anything added to DinosaurData or
        // SpaceData is matched with zero changes here:
        //   - exact name, word-boundary, and plural matching ("ankylosauruses")
        //   - typo tolerance (1 wrong letter for 6+ letter names, 2 for 10+)
        //   - kid abbreviations: any 5+ letter start of an 8+ letter name
        //     ("ankylo" finds Ankylosaurus, "pachy" finds Pachycephalosaurus)
        //   - the significant words of multi-word names ("sombrero" would
        //     find a future "Sombrero Galaxy" entry)
        // The alias tables further down are OPTIONAL extra nicknames and
        // common misnomers ("pterodactyl" -> Pteranodon) - nice to add for
        // polish, never required.
        //
        // Entries still containing template placeholder text ("Change ...")
        // are matched by name, but their placeholder fields are kept out of
        // the AI's notes automatically.
        private static List<Hit> FindEntities(string normalizedQuestion)
        {
            var hits = new List<Hit>();
            var tokens = normalizedQuestion.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var d in DinosaurData.GetAll())
            {
                int score = MatchScore(normalizedQuestion, tokens, EntityKeys(d.Name, DinoAliases(d.Name)));
                if (score > 0) hits.Add(new Hit { Name = d.Name, Dino = d, Score = score });
            }
            foreach (var s in SpaceData.GetAll())
            {
                int score = MatchScore(normalizedQuestion, tokens, EntityKeys(s.Name, SpaceAliases(s.Name)));
                if (score > 0) hits.Add(new Hit { Name = s.Name, Space = s, Score = score });
            }
            return hits;
        }

        // Words too generic to identify an entry on their own.
        private static readonly HashSet<string> AutoKeyStopwords = new HashSet<string>
        { "black", "hole", "holes", "star", "stars", "planet", "planets", "giant", "great", "super", "space", "little", "big" };

        private static List<string> EntityKeys(string name, IEnumerable<string> aliases)
        {
            var keys = new List<string>();
            string full = Normalize(name);
            if (!string.IsNullOrEmpty(full)) keys.Add(full);

            // Multi-word names also answer to their significant words, so a
            // brand-new "Sombrero Galaxy" entry matches "sombrero" with no
            // alias needed.
            if (full.Contains(' '))
            {
                foreach (var w in full.Split(' '))
                    if (w.Length >= 5 && !AutoKeyStopwords.Contains(w) && !keys.Contains(w))
                        keys.Add(w);
            }

            foreach (var a in aliases)
            {
                string n = Normalize(a);
                if (!string.IsNullOrEmpty(n) && !keys.Contains(n)) keys.Add(n);
            }
            return keys;
        }

        // Scores a question against one entry's names.
        // Exact phrase hits score by key length (longer name = more specific).
        // Single-word keys also match token-by-token with plural trimming and
        // a small typo allowance, because kids type "tricerotops".
        private static int MatchScore(string normalizedQuestion, string[] tokens, List<string> keys)
        {
            string padded = " " + normalizedQuestion + " ";
            int best = 0;

            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (padded.Contains(" " + key + " "))
                {
                    if (key.Length > best) best = key.Length;
                    continue;
                }

                // Single-word keys: compare against each question word.
                if (!key.Contains(' '))
                {
                    foreach (var t in tokens)
                    {
                        string tok = t;
                        if (tok.Length > 3 && tok.EndsWith("s")) tok = tok.Substring(0, tok.Length - 1);

                        if (tok == key)
                        {
                            if (key.Length > best) best = key.Length;
                            break;
                        }

                        // Kid abbreviations: a 5+ letter start of an 8+ letter
                        // name counts ("gigano" -> giganotosaurus). The length
                        // floors keep everyday words like "spin" from matching.
                        if (key.Length >= 8 && tok.Length >= 5 && tok.Length < key.Length && key.StartsWith(tok))
                        {
                            if (tok.Length > best) best = tok.Length;
                            break;
                        }

                        // Typo tolerance: one edit for 6+ letters, two for 10+.
                        if (key.Length >= 6 && Math.Abs(tok.Length - key.Length) <= 2)
                        {
                            int allowed = key.Length >= 10 ? 2 : 1;
                            if (EditDistanceAtMost(tok, key, allowed))
                            {
                                int score = key.Length - 1; // slightly below an exact hit
                                if (score > best) best = score;
                                break;
                            }
                        }
                    }
                }
            }
            return best;
        }

        // Bounded Levenshtein distance: true if within `max` edits.
        private static bool EditDistanceAtMost(string a, string b, int max)
        {
            if (Math.Abs(a.Length - b.Length) > max) return false;
            int n = a.Length, m = b.Length;
            var prev = new int[m + 1];
            var curr = new int[m + 1];
            for (int j = 0; j <= m; j++) prev[j] = j;
            for (int i = 1; i <= n; i++)
            {
                curr[0] = i;
                int rowMin = curr[0];
                for (int j = 1; j <= m; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                    if (curr[j] < rowMin) rowMin = curr[j];
                }
                if (rowMin > max) return false;
                var tmp = prev; prev = curr; curr = tmp;
            }
            return prev[m] <= max;
        }

        // Common names, nicknames, and kid spellings for each entry.
        private static IEnumerable<string> DinoAliases(string name)
        {
            switch (name)
            {
                case "T. Rex": return new[] { "trex", "t rex", "rex", "tyrannosaurus", "tyrannosaurus rex", "tyranosaurus", "t rexes" };
                case "Spinosaurus": return new[] { "spino", "spinosaur" };
                case "Megalodon": return new[] { "meg", "giant shark", "megashark", "megalodon shark", "shark" };
                case "Velociraptor": return new[] { "raptor", "raptors", "velociraptors" };
                case "Triceratops": return new[] { "trike", "three horned dinosaur" };
                case "Pteranodon": return new[] { "pterodactyl", "pteranadon", "pterosaur", "flying dinosaur" };
                case "Brachiosaurus": return new[] { "brachio", "long neck", "long neck dinosaur", "longneck" };
                case "Stegosaurus": return new[] { "stego", "steg" };
                case "Mosasaurus": return new[] { "mosasaur", "mosa" };
                case "Liopleurodon": return new[] { "liopleuradon" };
                case "Giganotosaurus": return new[] { "giga", "giganto", "gigantosaurus" };
                case "Carnotaurus": return new[] { "carno", "bull dinosaur" };
                case "Allosaurus": return new[] { "allo" };
                case "Parasaurolophus": return new[] { "parasaur", "para", "parasaurolofus" };
                case "Argentinosaurus": return new[] { "argentino" };
                case "Therizinosaurus": return new[] { "therizino", "scythe lizard", "claw dinosaur" };
                case "Titanosaurus": return new[] { "titano" };
                case "Titanoboa": return new[] { "giant snake", "big snake", "snake" };
                case "Deinonychus": return new[] { "deinonicus" };
                case "Deinosuchus": return new[] { "giant crocodile", "crocodile", "croc", "alligator", "gator" };
                default: return new string[0];
            }
        }

        private static IEnumerable<string> SpaceAliases(string name)
        {
            switch (name)
            {
                case "Moon": return new[] { "the moon", "luna", "moons" };
                case "Earth": return new[] { "the earth", "our planet", "planet earth" };
                case "Mars": return new[] { "the red planet", "red planet" };
                case "Sun": return new[] { "the sun", "our sun", "our star" };
                case "Orion": return new[] { "orion constellation", "the hunter" };
                case "Andromeda Galaxy": return new[] { "andromeda" };
                case "Phoenix A*": return new[] { "phoenix", "phoenix a", "phoenix a star", "biggest black hole", "largest black hole" };
                default: return new string[0];
            }
        }

        private static bool LooksLikeFollowUp(string normalizedQuestion)
        {
            string q = " " + normalizedQuestion + " ";
            int words = normalizedQuestion.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (words <= 3) return true;
            string[] pronouns = { " it ", " its ", " they ", " them ", " their ", " he ", " she ", " his ", " her ", " this ", " that ", " those ", " one " };
            return words <= 12 && pronouns.Any(p => q.Contains(p));
        }

        // ============================================================
        //  NOTES (grounding context)
        // ============================================================

        private static string BuildNotes(List<Hit> hits, string normalizedQuestion)
        {
            if (hits == null || hits.Count == 0) return "";
            int perEntry = MaxNotesChars / hits.Count;

            var sb = new StringBuilder();
            foreach (var h in hits)
            {
                string block = h.Dino != null
                    ? DinoContext(h.Dino, normalizedQuestion, perEntry)
                    : SpaceContext(h.Space, normalizedQuestion, perEntry);
                sb.AppendLine(block.TrimEnd());
                sb.AppendLine();
            }
            return sb.ToString().TrimEnd();
        }

        private static string DinoContext(Dinosaur d, string q, int budget)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name: " + d.Name);
            Add(sb, "Meaning", Usable(d.Meaning));
            Add(sb, "Lived", Usable(d.Era));
            Add(sb, "Type", Usable(d.Group));
            Add(sb, "Diet", Usable(d.Diet));
            Add(sb, "Length", Usable(d.Length));
            Add(sb, "Height", Usable(d.Height));
            Add(sb, "Width", Usable(d.Width));
            Add(sb, "Weight", Usable(d.Weight));
            Add(sb, "Speed", Usable(d.Speed));

            // Long text fields, most relevant to the question first.
            var fields = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("About", Usable(d.AboutText)),
                new KeyValuePair<string, string>("Features", Usable(d.KeyFeaturesText)),
                new KeyValuePair<string, string>("Habitat", Usable(d.LifeEnvironmentText)),
                new KeyValuePair<string, string>("Behaviour", Usable(d.BehaviourText)),
                new KeyValuePair<string, string>("Fun facts", Usable(d.FunFactsText)),
            };
            AppendPrioritized(sb, fields, q, budget);
            return sb.ToString();
        }

        private static string SpaceContext(SpaceObject s, string q, int budget)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name: " + s.Name);
            Add(sb, "Type", Usable(s.TypeLabel));
            Add(sb, "Category", Usable(s.Category));
            Add(sb, s.Stat1Label, Usable(s.Stat1Value));
            Add(sb, s.Stat2Label, Usable(s.Stat2Value));
            Add(sb, s.Stat3Label, Usable(s.Stat3Value));
            Add(sb, s.Stat4Label, Usable(s.Stat4Value));

            var fields = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("About", Usable(s.AboutText)),
                new KeyValuePair<string, string>("Features", Usable(s.KeyFeaturesText)),
                new KeyValuePair<string, string>("Orbit and movement", Usable(s.OrbitMovementText)),
                new KeyValuePair<string, string>("Surface", Usable(s.SurfaceCompositionText)),
                new KeyValuePair<string, string>("History", Usable(s.HistoryText)),
                new KeyValuePair<string, string>("What's inside", Usable(s.WhatsInsideText)),
                new KeyValuePair<string, string>("Fun facts", Usable(s.FunFactsText)),
            };
            AppendPrioritized(sb, fields, q, budget);
            return sb.ToString();
        }

        // Fits the long text fields into the entry's character budget, ordering
        // question-relevant fields first and giving them more room.
        private static void AppendPrioritized(StringBuilder sb, List<KeyValuePair<string, string>> fields, string q, int budget)
        {
            var wanted = WantedTopics(q);

            var ordered = fields
                .Where(f => !string.IsNullOrWhiteSpace(f.Value))
                .OrderByDescending(f => FieldRelevance(f.Key, wanted))
                .ToList();

            int remaining = budget - sb.Length;
            foreach (var f in ordered)
            {
                if (remaining < 60) break;
                bool relevant = FieldRelevance(f.Key, wanted) > 0;
                int cap = Math.Min(relevant ? 340 : 170, remaining - 20);
                if (cap < 60) break;
                string text = Snip(f.Value.Replace("\n", " "), cap);
                sb.AppendLine(f.Key + ": " + text);
                remaining = budget - sb.Length;
            }
        }

        private static HashSet<string> WantedTopics(string q)
        {
            var set = new HashSet<string>();
            if (HasAny(q, "eat", "eats", "ate", "food", "diet", "hunt", "hunts", "hunted", "prey")) set.Add("diet");
            if (HasAny(q, "big", "size", "tall", "long", "heavy", "weigh", "weight", "large", "huge", "small")) set.Add("size");
            if (HasAny(q, "fast", "speed", "slow", "run", "runs", "fly", "flies", "swim", "swims")) set.Add("speed");
            if (HasAny(q, "live", "lived", "lives", "habitat", "where", "home", "environment", "when")) set.Add("habitat");
            if (HasAny(q, "look", "looks", "looked", "feature", "features", "horn", "horns", "teeth", "claws", "sail", "frill", "wings", "made", "inside", "surface")) set.Add("features");
            if (HasAny(q, "fact", "facts", "cool", "interesting", "fun", "know")) set.Add("facts");
            if (HasAny(q, "behave", "behaviour", "behavior", "act", "smart", "pack", "packs", "social")) set.Add("behaviour");
            if (HasAny(q, "history", "discovered", "found", "orbit", "orbits", "move", "moves", "spin", "spins")) set.Add("history");
            return set;
        }

        private static int FieldRelevance(string fieldLabel, HashSet<string> wanted)
        {
            switch (fieldLabel)
            {
                case "Behaviour": return wanted.Contains("behaviour") || wanted.Contains("diet") ? 2 : 0;
                case "Habitat": return wanted.Contains("habitat") ? 2 : 0;
                case "Features": return wanted.Contains("features") || wanted.Contains("size") ? 2 : 0;
                case "Fun facts": return wanted.Contains("facts") ? 2 : 0;
                case "Orbit and movement": return wanted.Contains("history") || wanted.Contains("speed") ? 2 : 0;
                case "Surface": return wanted.Contains("features") ? 2 : 0;
                case "What's inside": return wanted.Contains("features") ? 2 : 0;
                case "History": return wanted.Contains("history") ? 2 : 0;
                case "About": return 1; // always a decent default
                default: return 0;
            }
        }

        // Template placeholder text ("Change this...") never reaches the AI.
        private static string Usable(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.TrimStart().StartsWith("Change", StringComparison.OrdinalIgnoreCase) ? null : value;
        }

        private static void Add(StringBuilder sb, string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                sb.AppendLine(label + ": " + value);
        }

        // ============================================================
        //  SUPERLATIVES ("what's the biggest dinosaur?")
        // ============================================================

        private static Hit SuperlativeHit(string normalizedQuestion)
        {
            string q = " " + normalizedQuestion + " ";
            if (!q.Contains(" dinosaur ") && !q.Contains(" dino ") && !q.Contains(" dinosaurs ") && !q.Contains(" dinos "))
                return null;

            Func<Dinosaur, string> stat;
            bool max;

            if (HasAny(normalizedQuestion, "biggest", "largest", "longest")) { stat = d => d.Length; max = true; }
            else if (HasAny(normalizedQuestion, "smallest", "shortest", "tiniest")) { stat = d => d.Length; max = false; }
            else if (HasAny(normalizedQuestion, "heaviest")) { stat = d => d.Weight; max = true; }
            else if (HasAny(normalizedQuestion, "lightest")) { stat = d => d.Weight; max = false; }
            else if (HasAny(normalizedQuestion, "tallest")) { stat = d => d.Height; max = true; }
            else if (HasAny(normalizedQuestion, "fastest", "quickest")) { stat = d => d.Speed; max = true; }
            else if (HasAny(normalizedQuestion, "slowest")) { stat = d => d.Speed; max = false; }
            else if (HasAny(normalizedQuestion, "strongest")) { stat = d => d.Strength.ToString(); max = true; }
            else return null;

            // "biggest meat eater" / "largest herbivore" — respect the diet filter.
            Func<Dinosaur, bool> dietFilter = d => true;
            if (HasAny(normalizedQuestion, "carnivore", "carnivores") || q.Contains(" meat eater ") || q.Contains(" meat eating "))
                dietFilter = d => (d.Diet ?? "").ToLowerInvariant().Contains("carnivore");
            else if (HasAny(normalizedQuestion, "herbivore", "herbivores") || q.Contains(" plant eater ") || q.Contains(" plant eating "))
                dietFilter = d => (d.Diet ?? "").ToLowerInvariant().Contains("herbivore");

            Dinosaur best = null;
            double bestVal = max ? double.MinValue : double.MaxValue;
            foreach (var d in DinosaurData.GetAll())
            {
                if (!dietFilter(d)) continue;
                double? parsed = ParseLeadingNumber(stat(d));
                if (parsed == null) continue;
                double v = parsed.Value;
                if ((max && v > bestVal) || (!max && v < bestVal)) { bestVal = v; best = d; }
            }

            return best == null ? null : new Hit { Name = best.Name, Dino = best, Score = 1 };
        }

        // ============================================================
        //  SMALL HELPERS
        // ============================================================

        private static bool HasAny(string normalizedQuestion, params string[] words)
        {
            string padded = " " + normalizedQuestion + " ";
            foreach (var w in words)
                if (padded.Contains(" " + w + " ")) return true;
            return false;
        }

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
            double val;
            return double.TryParse(sb.ToString(), out val) ? val : (double?)null;
        }

        private static string Snip(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Trim();
            if (text.Length <= max) return text;
            string cutText = text.Substring(0, max);
            int lastStop = Math.Max(cutText.LastIndexOf('.'), Math.Max(cutText.LastIndexOf('!'), cutText.LastIndexOf('?')));
            if (lastStop > max / 2) return cutText.Substring(0, lastStop + 1);
            int lastSpace = cutText.LastIndexOf(' ');
            if (lastSpace > 0) cutText = cutText.Substring(0, lastSpace);
            return cutText + "...";
        }

        private static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder();
            foreach (char c in text.ToLowerInvariant())
                sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            return string.Join(" ", sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
