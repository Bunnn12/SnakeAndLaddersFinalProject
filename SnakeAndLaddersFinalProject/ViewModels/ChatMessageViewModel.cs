using System;
using SnakeAndLaddersFinalProject.ChatService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ChatMessageViewModel
    {
        private const string DEFAULT_SENDER_NAME = "Unknown";

        public string Sender { get; }

        public string Text { get; }

        public DateTime TimestampUtc { get; }

        public bool IsMine { get; }

        public string AvatarId { get; }

        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarId);

        public DateTime SentAt => TimestampUtc.ToLocalTime();
        public string Header => Sender;

        public ChatMessageViewModel(ChatMessageDto dto, string currentUserName)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            string safeSender = string.IsNullOrWhiteSpace(dto.Sender)
                ? DEFAULT_SENDER_NAME
                : dto.Sender;

            Sender = safeSender;
            Text = dto.Text ?? string.Empty;
            TimestampUtc = dto.TimestampUtc;
            AvatarId = dto.SenderAvatarId ?? string.Empty;

            IsMine =
                !string.IsNullOrWhiteSpace(currentUserName) &&
                string.Equals(
                    Sender,
                    currentUserName,
                    StringComparison.OrdinalIgnoreCase);
        }

        public ChatMessageDto ToDto()
        {
            return new ChatMessageDto
            {
                Sender = Sender ?? string.Empty,
                Text = Text ?? string.Empty,
                TimestampUtc = TimestampUtc,
                SenderAvatarId = AvatarId ?? string.Empty
            };
        }
    }
}
