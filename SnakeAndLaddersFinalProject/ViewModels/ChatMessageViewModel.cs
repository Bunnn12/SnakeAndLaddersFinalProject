using System;
using SnakeAndLaddersFinalProject.ChatService;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ChatMessageViewModel
    {
        private const string EMPTY_TEXT = "";
        private const int MIN_VALID_STICKER_ID = 1;
        private const int SENDER_MIN_LENGTH = 1;
        private const int SENDER_MAX_LENGTH = 90;
        private const int CHAT_TEXT_MIN_LENGTH = 0;
        private const int CHAT_TEXT_MAX_LENGTH = 300; 

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

            string rawSender = chatMessageDto.Sender ?? EMPTY_TEXT;
            string normalizedSender = InputValidator.Normalize(rawSender);

            if (!InputValidator.IsIdentifierText(normalizedSender, SENDER_MIN_LENGTH, SENDER_MAX_LENGTH))
            {
                Sender = EMPTY_TEXT;
            }
            else
            {
                Sender = normalizedSender;
            }

            AvatarId = chatMessageDto.SenderAvatarId ?? EMPTY_TEXT;
            SentAt = chatMessageDto.TimestampUtc.ToLocalTime();

            IsMine = !string.IsNullOrWhiteSpace(currentUserName) && string.Equals(Sender, currentUserName, StringComparison.OrdinalIgnoreCase);
            Header = Sender;

            bool hasSticker = chatMessageDto.StickerId > MIN_VALID_STICKER_ID && !string.IsNullOrWhiteSpace(chatMessageDto.StickerCode);
            IsSticker = hasSticker;

            if (hasSticker)
            {
                Text = EMPTY_TEXT;
                StickerImagePath = ChatViewModel.BuildStickerAssetPath(chatMessageDto.StickerCode);
            }
            else
            {
                string rawText = chatMessageDto.Text ?? EMPTY_TEXT;
                string normalizedText = InputValidator.Normalize(rawText);

                if (!InputValidator.IsSafeText(normalizedText, CHAT_TEXT_MIN_LENGTH, CHAT_TEXT_MAX_LENGTH, allowNewLines: true))
                {
                    Text = EMPTY_TEXT;
                }
                else
                {
                    Text = normalizedText;
                }

                StickerImagePath = EMPTY_TEXT;
            }
        }
    }
}
