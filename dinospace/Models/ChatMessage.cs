namespace dinospace.Models
{
    // One bubble in the Ask Nova chat: who said it and what they said.
    // Text mutates in place while an answer streams in token by token,
    // so the bubble grows live instead of appearing all at once.
    public class ChatMessage
    {
        public bool IsUser { get; set; }
        public string Text { get; set; } = "";
    }
}
