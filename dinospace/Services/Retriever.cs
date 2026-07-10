using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dinospace.Services
{
    // The result of grounding one question: the facts to show the model, plus
    // which named entities matched (so follow-up questions know what "it" is).
    public class Grounding
    {
        public string Notes = "";
        public List<string> Entities = new();
        public bool HasEntity;
        public bool HasKnowledge;
    }

    // Finds the encyclopedia entries and curated knowledge that a question is
    // about, and assembles them into a compact NOTES block. This is the "RAG"
    // that was broken before: now it covers both named entities AND general
    // topics (extinction, how stars form, ...) via the KnowledgeBase.
    public static class Retriever
    {
        // Small on purpose: notes are the biggest chunk of the prompt, and
        // prompt length is what makes on-device answers slow.
        private const int MaxNotesChars = 700;

        public static Grounding Ground(string normalizedQuestion, IReadOnlyList<string> carryover)
        {
            var g = new Grounding();
            var hits = FindEntities(normalizedQuestion);

            // Superlatives ("biggest dinosaur") resolve before follow-up
            // carryover: "fastest dino?" right after chatting about another
            // creature is a new question, not a pronoun follow-up.
            if (hits.Count == 0)
            {
                var champ = Superlative(normalizedQuestion);
                if (champ != null) hits.Add(champ);
            }

            // Follow-up with no name of its own -> reuse the previous entities.
            if (hits.Count == 0 && carryover is { Count: > 0 } && LooksLikeFollowUp(normalizedQuestion))
            {
                foreach (var name in carryover)
                {
                    var d = DinoData.ByName(name);
                    if (d != null) { hits.Add(new Hit(d.Name, d, null, 1)); continue; }
                    var s = SpaceData.ByName(name);
                    if (s != null) hits.Add(new Hit(s.Name, null, s, 1));
                }
            }

            bool comparison = HasAny(normalizedQuestion, "vs", "versus", "beat", "beats", "fight", "battle", "compare", "stronger", "bigger", "faster", "against", "win", "or",
                                     "between", "apart", "from", "to");   // "how far is X from Y" needs both entities kept
            int keep = comparison ? 2 : 1;
            var top = hits.OrderByDescending(h => h.Score).ThenBy(h => h.Name)
                          .GroupBy(h => h.Name).Select(x => x.First())
                          .Take(keep).ToList();

            var sb = new StringBuilder();
            foreach (var h in top)
            {
                g.Entities.Add(h.Name);
                sb.AppendLine(h.Dino != null ? DinoNote(h.Dino) : SpaceNote(h.Space!));
                sb.AppendLine();
            }
            g.HasEntity = top.Count > 0;

            // Curated knowledge nuggets for general topics.
            int budgetLeft = MaxNotesChars - sb.Length;
            if (BestNuggetMatch(normalizedQuestion).nugget is KnowledgeNugget kn && budgetLeft >= 120)
            {
                sb.AppendLine("• " + kn.Fact);
                g.HasKnowledge = true;
            }

            g.Notes = sb.ToString().Trim();
            if (g.Notes.Length > MaxNotesChars) g.Notes = g.Notes[..MaxNotesChars];
            return g;
        }

        // ---------- entities ----------

        private record Hit(string Name, Dinosaur? Dino, SpaceObject? Space, int Score);

        private static readonly HashSet<string> StopKeys = new()
        { "black", "hole", "star", "planet", "giant", "great", "super", "space", "little", "big", "the" };

        private static List<Hit> FindEntities(string q)
        {
            var hits = new List<Hit>();
            var tokens = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var d in DinoData.All)
            {
                int score = Score(q, tokens, Keys(d.Name, d.Aliases));
                if (score > 0) hits.Add(new Hit(d.Name, d, null, score));
            }
            foreach (var s in SpaceData.All)
            {
                int score = Score(q, tokens, Keys(s.Name, s.Aliases));
                if (score > 0) hits.Add(new Hit(s.Name, null, s, score));
            }
            return hits;
        }

        private static List<string> Keys(string name, string[] aliases)
        {
            var keys = new List<string>();
            string full = Normalize(name);
            if (full.Length > 0) keys.Add(full);
            if (full.Contains(' '))
                foreach (var w in full.Split(' '))
                    if (w.Length >= 5 && !StopKeys.Contains(w) && !keys.Contains(w)) keys.Add(w);
            foreach (var a in aliases)
            {
                var n = Normalize(a);
                if (n.Length > 0 && !keys.Contains(n)) keys.Add(n);
            }
            return keys;
        }

        private static int Score(string q, string[] tokens, List<string> keys)
        {
            string padded = " " + q + " ";
            int best = 0;
            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (padded.Contains(" " + key + " ")) { if (key.Length > best) best = key.Length; continue; }
                if (key.Contains(' ')) continue;

                foreach (var t in tokens)
                {
                    string tok = t.Length > 3 && t.EndsWith("s") ? t[..^1] : t;
                    if (tok == key || t == key) { if (key.Length > best) best = key.Length; break; }
                    // kid abbreviation: 5+ letter start of an 8+ letter name
                    if (key.Length >= 8 && tok.Length >= 5 && tok.Length < key.Length && key.StartsWith(tok))
                    { if (tok.Length > best) best = tok.Length; break; }
                    // Typo tolerance. The RAW token is checked as well as the
                    // de-pluralised one: dinosaur names end in "s", so stripping
                    // it turned near-misses like "trisaratops" into far misses.
                    if (key.Length >= 5 && Math.Abs(t.Length - key.Length) <= 2)
                    {
                        int allowed = key.Length >= 7 ? 2 : 1;
                        if ((EditDistanceAtMost(t, key, allowed) || EditDistanceAtMost(tok, key, allowed)
                             // doubled-letter typos ("neptoon", "satturn") sit
                             // just past the edit budget — squash runs first
                             || EditDistanceAtMost(Squash(t), Squash(key), allowed))
                            && key.Length - 1 > best) best = key.Length - 1;
                    }
                    // very short names ("sun") still deserve one slip — "the
                    // son" — but only when the first letter agrees, so common
                    // words can't drift onto planets.
                    if (key.Length is 3 or 4 && t.Length == key.Length && t[0] == key[0]
                        && EditDistanceAtMost(t, key, 1) && key.Length - 1 > best) best = key.Length - 1;
                }

                // Split-typo repair: kids write "tirano sarus" or "veloci
                // raptor". Adjacent tokens are joined and matched against the
                // long names. A fuzzy joined hit scores well BELOW an exact
                // name (-4), because question words glued to a name ("did
                // spinosaurus") can lie within edit distance of some OTHER
                // dinosaur — the real name must always outrank the mirage.
                if (key.Length >= 8)
                    for (int i = 0; i + 1 < tokens.Length; i++)
                    {
                        string joined = tokens[i] + tokens[i + 1];
                        if (joined == key) { if (key.Length > best) best = key.Length; break; }
                        if (joined.Length >= 10 && Math.Abs(joined.Length - key.Length) <= 3)
                        {
                            int allowed = key.Length >= 12 ? 4 : 2;
                            if (EditDistanceAtMost(joined, key, allowed)
                                && key.Length - 4 > best) best = key.Length - 4;
                        }
                    }
            }
            return best;
        }

        // ---------- notes ----------

        private static string DinoNote(Dinosaur d)
        {
            var sb = new StringBuilder();
            sb.Append("[").Append(d.Name).Append("] ");
            sb.Append(d.Meaning).Append(", a ").Append(d.Diet.ToLowerInvariant()).Append(' ')
              .Append(d.Group.ToLowerInvariant()).Append(" from the ").Append(d.Era).Append(". ");
            var stats = new List<string>();
            if (Has(d.Length)) stats.Add("length " + d.Length);
            if (Has(d.Height)) stats.Add("height " + d.Height);
            if (Has(d.Weight)) stats.Add("weight " + d.Weight);
            if (Has(d.Speed)) stats.Add("top speed " + d.Speed);
            if (stats.Count > 0) sb.Append("Size: ").Append(string.Join(", ", stats)).Append(". ");
            sb.Append(Snip(d.AboutText, 150));
            string extra = FirstFunFact(d.FunFactsText);
            if (extra.Length > 0) sb.Append(" Fact: ").Append(extra);
            return sb.ToString();
        }

        private static string SpaceNote(SpaceObject s)
        {
            var sb = new StringBuilder();
            sb.Append("[").Append(s.Name).Append("] ").Append(s.TypeLabel).Append(". ");
            var stats = new List<string>();
            void Add(string l, string v) { if (Has(v)) stats.Add(l.ToLowerInvariant() + " " + v); }
            Add(s.Stat1Label, s.Stat1Value); Add(s.Stat2Label, s.Stat2Value);
            Add(s.Stat3Label, s.Stat3Value); Add(s.Stat4Label, s.Stat4Value);
            if (stats.Count > 0) sb.Append(string.Join(", ", stats)).Append(". ");
            sb.Append(Snip(s.AboutText, 150));
            string extra = FirstFunFact(s.FunFactsText);
            if (extra.Length > 0) sb.Append(" Fact: ").Append(extra);
            return sb.ToString();
        }

        private static string FirstFunFact(string funFacts)
        {
            if (string.IsNullOrWhiteSpace(funFacts)) return "";
            var first = funFacts.Split('\n').FirstOrDefault(l => l.Trim().Length > 0) ?? "";
            return first.TrimStart('•', ' ').Trim();
        }

        // ---------- knowledge nuggets ----------

        // The single best curated fact for a question, if any — used by
        // LocalAnswer to reply to general topics without the model.
        public static KnowledgeNugget? BestNugget(string normalizedQuestion)
            => BestNuggetMatch(normalizedQuestion).nugget;

        // The best nugget plus the longest keyword phrase that matched it.
        // Callers use the phrase length to let a real topic ("first on the
        // moon") outrank a passing mention of an entity ("moon").
        public static (KnowledgeNugget? nugget, string keyword) BestNuggetMatch(string q)
        {
            string padded = " " + q + " ";
            KnowledgeNugget? best = null;
            int bestScore = 0;
            string bestKw = "";
            foreach (var n in KnowledgeBase.Nuggets)
            {
                int score = 0;
                string longest = "";
                foreach (var kw in n.Keywords)
                {
                    string k = Normalize(kw);
                    if (k.Length == 0) continue;
                    if (padded.Contains(" " + k + " ") || padded.Contains(" " + k))
                    {
                        score += k.Split(' ').Length + k.Length / 6;
                        if (longest.Length == 0 || k.Split(' ').Length > longest.Split(' ').Length)
                            longest = k;
                    }
                }
                if (score > bestScore) { bestScore = score; best = n; bestKw = longest; }
            }
            return (best, bestKw);
        }

        // ---------- superlatives ----------

        // Groups in DinoData that are true dinosaurs. The encyclopedia also
        // holds pterosaurs, marine reptiles, mammals and sharks — great
        // entries, but "fastest dinosaur" must not answer with a pterosaur.
        private static readonly HashSet<string> DinosaurGroups = new(StringComparer.OrdinalIgnoreCase)
        {
            "Theropod", "Sauropod", "Raptor", "Ankylosaur", "Ceratopsian",
            "Hadrosaur", "Ornithomimid", "Ornithopod", "Pachycephalosaur", "Stegosaur"
        };

        private static Hit? Superlative(string q)
        {
            string p = " " + q + " ";
            bool dinoOnly = p.Contains(" dinosaur ") || p.Contains(" dino ") || p.Contains(" dinosaurs ") || p.Contains(" dinos ");
            if (!dinoOnly
                && !p.Contains(" creature ") && !p.Contains(" animal ")
                && !p.Contains(" carnivore ") && !p.Contains(" herbivore ") && !p.Contains(" predator ")
                && !p.Contains(" meat eater ") && !p.Contains(" plant eater "))
                return null;

            Func<Dinosaur, string> stat; bool max;
            if (HasAny(q, "biggest", "largest", "longest")) { stat = d => d.Length; max = true; }
            else if (HasAny(q, "smallest", "shortest", "tiniest")) { stat = d => d.Length; max = false; }
            else if (HasAny(q, "heaviest")) { stat = d => d.Weight; max = true; }
            else if (HasAny(q, "lightest")) { stat = d => d.Weight; max = false; }
            else if (HasAny(q, "tallest")) { stat = d => d.Height; max = true; }
            else if (HasAny(q, "fastest", "quickest")) { stat = d => d.Speed; max = true; }
            else if (HasAny(q, "slowest")) { stat = d => d.Speed; max = false; }
            else if (HasAny(q, "strongest")) { stat = d => d.Strength.ToString(); max = true; }
            else return null;

            Func<Dinosaur, bool> diet = _ => true;
            if (HasAny(q, "carnivore", "carnivores") || p.Contains(" meat eater ") || p.Contains(" meat eating "))
                diet = d => d.Diet.ToLowerInvariant().Contains("carnivore");
            else if (HasAny(q, "herbivore", "herbivores") || p.Contains(" plant eater ") || p.Contains(" plant eating "))
                diet = d => d.Diet.ToLowerInvariant().Contains("herbivore");

            Dinosaur? best = null;
            double bestVal = max ? double.MinValue : double.MaxValue;
            foreach (var d in DinoData.All)
            {
                if (dinoOnly && !DinosaurGroups.Contains(d.Group)) continue;
                if (!diet(d)) continue;
                var v = LeadingNumber(stat(d));
                if (v == null) continue;
                if ((max && v > bestVal) || (!max && v < bestVal)) { bestVal = v.Value; best = d; }
            }
            return best == null ? null : new Hit(best.Name, best, null, 1);
        }

        // ---------- helpers ----------

        private static bool LooksLikeFollowUp(string q)
        {
            // "what is love", "whats a modem" — a what/who question that names
            // something NEW (not a pronoun) is a fresh question, even a short
            // one. Reusing the carryover here answered "what is love" with a
            // summary of whatever creature was discussed last.
            foreach (var intro in new[] { "what is ", "what are ", "whats ", "who is ", "whos " })
                if (q.StartsWith(intro, StringComparison.Ordinal))
                {
                    string first = q[intro.Length..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                    string[] refersBack = { "it", "its", "that", "this", "he", "she", "they", "those", "these", "them" };
                    if (first.Length > 0 && !refersBack.Contains(first)) return false;
                }

            string p = " " + q + " ";
            int words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (words <= 3) return true;
            string[] pronouns = { " it ", " its ", " they ", " them ", " their ", " he ", " she ", " his ", " her ", " this ", " that ", " those ", " one " };
            return words <= 12 && pronouns.Any(p.Contains);
        }

        private static bool Has(string v) => !string.IsNullOrWhiteSpace(v);

        private static bool HasAny(string q, params string[] words)
        {
            string p = " " + q + " ";
            return words.Any(w => p.Contains(" " + w + " "));
        }

        private static double? LeadingNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var sb = new StringBuilder(); bool started = false;
            foreach (char c in text.Replace(",", ""))
            {
                if (char.IsDigit(c) || (c == '.' && started)) { sb.Append(c); started = true; }
                else if (started) break;
            }
            return sb.Length > 0 && double.TryParse(sb.ToString(), out var v) ? v : null;
        }

        private static string Snip(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Trim();
            if (text.Length <= max) return text;
            string cut = text[..max];
            int stop = Math.Max(cut.LastIndexOf('.'), Math.Max(cut.LastIndexOf('!'), cut.LastIndexOf('?')));
            if (stop > max / 2) return cut[..(stop + 1)];
            int sp = cut.LastIndexOf(' ');
            return (sp > 0 ? cut[..sp] : cut) + "...";
        }

        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder();
            foreach (char c in text.ToLowerInvariant())
                sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            return string.Join(" ", sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        // Collapses runs of the same letter ("neptoon" -> "nepton") so a
        // doubled-letter typo costs nothing before the edit-distance check.
        private static string Squash(string s)
        {
            var sb = new StringBuilder(s.Length);
            char prev = '\0';
            foreach (char c in s)
            {
                if (c != prev) sb.Append(c);
                prev = c;
            }
            return sb.ToString();
        }

        private static bool EditDistanceAtMost(string a, string b, int max)
        {
            if (Math.Abs(a.Length - b.Length) > max) return false;
            int n = a.Length, m = b.Length;
            var prev = new int[m + 1]; var curr = new int[m + 1];
            for (int j = 0; j <= m; j++) prev[j] = j;
            for (int i = 1; i <= n; i++)
            {
                curr[0] = i; int rowMin = curr[0];
                for (int j = 1; j <= m; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                    if (curr[j] < rowMin) rowMin = curr[j];
                }
                if (rowMin > max) return false;
                (prev, curr) = (curr, prev);
            }
            return prev[m] <= max;
        }
    }
}
