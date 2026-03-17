using System;

namespace NetProve.Models
{
    public sealed class ChatMessage
    {
        public string Text { get; init; } = "";
        public bool IsUser { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.Now;
    }
}
