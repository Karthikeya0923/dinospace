using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dinospace.Services
{
    // What the chat page gets back. Exactly one of Prompt / InstantReply is set.
    public class NovaTurn
    {
        public string? Prompt;         // send this to the model
        public string? InstantReply;   // show immediately, no model needed
        public List<string> Entities = new();
    }

    // Builds the model prompt. Compared to the old version this is much
    // leaner: one tight instruction, the grounded FACTS up front, a single
    // short example, and a small history window. Less prompt overhead leaves
    // the small on-device model more room to actually answer.
    public static class PromptBuilder
    {
        public static NovaTurn Build(string question, IReadOnlyList<ChatMessage> history, IReadOnlyList<string> carryover)
        {
            var turn = new NovaTurn();
            question = (question ?? "").Trim();
            if (question.Length > 300) question = question[..300];
            string q = Retriever.Normalize(question);

            // 1. smalltalk
            var small = NovaGuard.SmallTalk(q);
            if (small != null) { turn.InstantReply = small; return turn; }

            // 2. safety
            var screened = NovaGuard.Screen(q);
            if (screened != null) { turn.InstantReply = screened; return turn; }

            // 3. grounding
            var g = Retriever.Ground(q, carryover);
            turn.Entities = g.Entities;

            // 3.5 Answer straight from the vetted encyclopedia whenever we can.
            //     This is instant and always accurate, and it means the slow
            //     on-device model is only used for genuinely open questions —
            //     so NovaSaur stops getting stuck on "thinking…".
            var direct = LocalAnswer.TryAnswer(question, q, g, carryover);
            if (direct != null) { turn.InstantReply = direct; return turn; }

            // 4. topic gate (generous)
            bool hasCarryover = carryover is { Count: > 0 };
            if (!NovaGuard.OnTopic(q, g.HasEntity, g.HasKnowledge, hasCarryover))
            {
                turn.InstantReply = NovaGuard.OffTopic;
                return turn;
            }

            // 5. compose
            turn.Prompt = Compose(question, g.Notes, history);
            return turn;
        }

        // Kept deliberately tiny: on a phone CPU, prompt length is the main
        // driver of how long the user stares at "thinking…". Every line here
        // costs real seconds on-device.
        private static string Compose(string question, string notes, IReadOnlyList<ChatMessage> history)
        {
            bool grounded = !string.IsNullOrEmpty(notes);
            var sb = new StringBuilder();

            sb.Append("You are NovaSaur, a friendly dinosaur and space expert for kids. Answer in 2 to 3 short, clear, accurate sentences a 10-year-old understands. No emojis, no lists.");
            if (grounded)
                sb.Append(" Trust these facts and copy their exact numbers: ");
            sb.AppendLine();

            if (grounded)
            {
                sb.AppendLine(notes.Trim());
            }

            // The last two exchanges ride along so follow-ups ("so what made
            // it shine brighter?") actually connect to what was just said.
            string hist = History(history);
            if (!string.IsNullOrEmpty(hist))
            {
                sb.AppendLine("The chat so far:");
                sb.AppendLine(hist);
                sb.AppendLine("Answer the next question in the context of that chat.");
            }

            sb.AppendLine("Q: " + question);
            sb.Append("A:");
            return sb.ToString();
        }

        // The last two Q/A pairs, compressed.
        private static string History(IReadOnlyList<ChatMessage> messages)
        {
            if (messages == null || messages.Count == 0) return "";
            var pairs = new List<string>();
            int i = messages.Count - 1;
            while (i >= 1 && pairs.Count < 2)
            {
                if (!messages[i].IsUser && messages[i - 1].IsUser)
                {
                    pairs.Insert(0, "Q: " + Snip(messages[i - 1].Text, 100) + "\nA: " + Snip(messages[i].Text, 150));
                    i -= 2;
                }
                else i--;
            }
            return string.Join("\n", pairs);
        }

        // ---------- answer cleanup ----------

        public static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string text = raw.Trim();

            // Unwrap Message(text=..., role=...) if a library stringifies it.
            if (text.StartsWith("Message(") && text.EndsWith(")"))
            {
                int t = text.IndexOf("text=", StringComparison.OrdinalIgnoreCase);
                if (t >= 0)
                {
                    string inner = text[(t + 5)..];
                    int stop = inner.LastIndexOf(", role=", StringComparison.OrdinalIgnoreCase);
                    if (stop < 0) stop = inner.LastIndexOf(')');
                    if (stop > 0) text = inner[..stop].Trim();
                }
            }

            text = text.Replace("<end_of_turn>", " ").Replace("<start_of_turn>", " ")
                       .Replace("<eos>", " ").Replace("<bos>", " ").Replace("<pad>", " ");

            // Stop if the model started a fresh turn.
            foreach (var marker in new[] { "\nQ:", "\nQuestion:", "Q:", "Question:" })
            {
                int cut = text.IndexOf(marker, 1, StringComparison.OrdinalIgnoreCase);
                if (cut > 0) { text = text[..cut]; break; }
            }

            text = text.Trim();
            foreach (var prefix in new[] { "model", "A:", "Answer:", "NovaSaur:" })
                if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    text = text[prefix.Length..].Trim();

            text = text.Replace("**", "").Replace("`", "").Replace("*", "");
            var lines = text.Split('\n')
                            .Select(l => l.TrimStart().TrimStart('-', '•').Trim())
                            .Where(l => l.Length > 0);
            text = string.Join(" ", lines);

            var sb = new StringBuilder(); bool lastSpace = false;
            foreach (char c in text)
            {
                bool space = char.IsWhiteSpace(c);
                if (space && lastSpace) continue;
                sb.Append(space ? ' ' : c);
                lastSpace = space;
            }
            text = sb.ToString().Trim().Trim('"').Trim();
            return ClampSentences(text, 5, 640);
        }

        private static string ClampSentences(string text, int maxSentences, int maxChars)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int sentences = 0, end = text.Length;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c is '.' or '!' or '?')
                {
                    bool dec = c == '.' && i + 1 < text.Length && char.IsDigit(text[i + 1]) && i > 0 && char.IsDigit(text[i - 1]);
                    bool initial = c == '.' && i > 0 && char.IsUpper(text[i - 1]) && (i < 2 || text[i - 2] == ' ');
                    if (dec || initial) continue;
                    sentences++;
                    if (sentences >= maxSentences || i + 1 >= maxChars) { end = i + 1; break; }
                }
            }
            string outText = text[..Math.Min(end, text.Length)].Trim();
            if (outText.Length > maxChars)
            {
                outText = outText[..maxChars];
                int last = Math.Max(outText.LastIndexOf('.'), Math.Max(outText.LastIndexOf('!'), outText.LastIndexOf('?')));
                if (last > 40) outText = outText[..(last + 1)];
            }
            return outText.Trim();
        }

        private static string Snip(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Trim();
            return text.Length <= max ? text : text[..max] + "...";
        }
    }
}
