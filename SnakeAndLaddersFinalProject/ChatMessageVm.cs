using System;
using SnakeAndLaddersFinalProject.ChatService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ChatMessageVm
    {
        public string Sender { get; }
        public string Text { get; }
        public DateTime TimestampUtc { get; }

        // Extras para tu XAML
        public bool IsMine { get; }
        public string Header => $"{Sender}";
        public DateTime SentAt => TimestampUtc.ToLocalTime();

        // Avatares si luego los manejas
        public string AvatarPath { get; }
        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarPath);

        public ChatMessageVm(ChatMessageDto dto, string currentUserName, string avatarPath = "")
        {
            Sender = dto?.Sender ?? "";
            Text = dto?.Text ?? "";
            TimestampUtc = dto?.TimestampUtc ?? DateTime.UtcNow;
            AvatarPath = avatarPath ?? "";
            // Considera iguales si coincide (case-insensitive)
            IsMine = !string.IsNullOrWhiteSpace(currentUserName) &&
                     string.Equals(Sender, currentUserName, StringComparison.OrdinalIgnoreCase);
        }

        public ChatMessageDto ToDto() => new ChatMessageDto
        {
            Sender = Sender,
            Text = Text,
            TimestampUtc = TimestampUtc
        };
    }
}
