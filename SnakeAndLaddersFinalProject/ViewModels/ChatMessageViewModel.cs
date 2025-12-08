using System;
using SnakeAndLaddersFinalProject.ChatService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ChatMessageViewModel
    {
        private const string EMPTY_TEXT = "";
        private const int MIN_VALID_STICKER_ID = 1;
        public string Sender { get; }
        public string Text { get; }
        public DateTime SentAt { get; }
        public bool IsMine { get; }
        public string Header { get; }
        public string AvatarId { get; }
        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarId);

        public bool IsSticker { get; }
        public string StickerImagePath { get; }

        public ChatMessageViewModel(ChatMessageDto chatMessageDto, string currentUserName)
        {
            if (chatMessageDto == null)
            {
                throw new ArgumentNullException(nameof(chatMessageDto));
            }

            Sender = chatMessageDto.Sender ?? EMPTY_TEXT;
            AvatarId = chatMessageDto.SenderAvatarId ?? EMPTY_TEXT;
            SentAt = chatMessageDto.TimestampUtc.ToLocalTime();

            IsMine = !string.IsNullOrWhiteSpace(currentUserName)
                     && string.Equals(Sender, currentUserName, StringComparison.OrdinalIgnoreCase);

            Header = Sender;

            bool hasSticker = chatMessageDto.StickerId > MIN_VALID_STICKER_ID
                              && !string.IsNullOrWhiteSpace(chatMessageDto.StickerCode);

            IsSticker = hasSticker;

            if (hasSticker)
            {
                Text = EMPTY_TEXT;
                StickerImagePath = ChatViewModel.BuildStickerAssetPath(chatMessageDto.StickerCode);
            }
            else
            {
                Text = (chatMessageDto.Text ?? EMPTY_TEXT).Trim();
                StickerImagePath = EMPTY_TEXT;
            }
        }
    }
}
