using System;
using SnakeAndLaddersFinalProject.ChatService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ChatMessageViewModel
    {
        private const string EMPTY_TEXT = "";

        public string Sender { get; }
        public string Text { get; }
        public DateTime SentAt { get; }
        public bool IsMine { get; }
        public string Header { get; }
        public string AvatarId { get; }
        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarId);

        public bool IsSticker { get; }
        public string StickerImagePath { get; }

        public ChatMessageViewModel(ChatMessageDto dto, string currentUserName)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            Sender = dto.Sender ?? EMPTY_TEXT;
            AvatarId = dto.SenderAvatarId ?? EMPTY_TEXT;
            SentAt = dto.TimestampUtc.ToLocalTime();

            IsMine = !string.IsNullOrWhiteSpace(currentUserName)
                     && string.Equals(Sender, currentUserName, StringComparison.OrdinalIgnoreCase);

            Header = Sender;

            bool hasSticker = dto.StickerId > 0
                              && !string.IsNullOrWhiteSpace(dto.StickerCode);

            IsSticker = hasSticker;

            if (hasSticker)
            {
                Text = EMPTY_TEXT;
                StickerImagePath = ChatViewModel.BuildStickerAssetPath(dto.StickerCode);
            }
            else
            {
                Text = (dto.Text ?? EMPTY_TEXT).Trim();
                StickerImagePath = string.Empty;
            }
        }
    }
}
