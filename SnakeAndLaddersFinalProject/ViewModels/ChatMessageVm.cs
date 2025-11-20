using System;
using SnakeAndLaddersFinalProject.ChatService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ChatMessageVm
    {
        public string Sender { get; }

        public string Text { get; }

        public DateTime TimestampUtc { get; }

        public bool IsMine { get; }

        public string Header => $"{Sender}";

        public DateTime SentAt => TimestampUtc.ToLocalTime();

        public string AvatarId { get; }

        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarId);

        public ChatMessageVm(ChatMessageDto dto, string currentUserName)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            Sender = dto.Sender ?? string.Empty;
            Text = dto.Text ?? string.Empty;
            TimestampUtc = dto.TimestampUtc;
            AvatarId = dto.SenderAvatarId;

            IsMine =
                !string.IsNullOrWhiteSpace(currentUserName) &&
                string.Equals(Sender, currentUserName, StringComparison.OrdinalIgnoreCase);
        }

        public ChatMessageDto ToDto()
        {
            return new ChatMessageDto
            {
                Sender = Sender,
                Text = Text,
                TimestampUtc = TimestampUtc,
                SenderAvatarId = AvatarId
            };
        }
    }
}
