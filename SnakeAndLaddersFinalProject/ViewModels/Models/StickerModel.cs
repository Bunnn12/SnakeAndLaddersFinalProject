using System;

namespace SnakeAndLaddersFinalProject.Models
{
    public sealed class StickerModel
    {
        private const string EMPTY_TEXT = "";
        private const string EMPTY_IMAGE_PATH = "";

        public int StickerId { get; private set; }

        public string StickerCode { get; private set; }

        public string Name { get; private set; }

        public string ImagePath { get; private set; }

        public StickerModel(int stickerId, string stickerCode, string name, string imagePath)
        {
            if (stickerId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stickerId));
            }

            StickerId = stickerId;
            StickerCode = stickerCode ?? EMPTY_TEXT;
            Name = name ?? EMPTY_TEXT;
            ImagePath = imagePath ?? EMPTY_IMAGE_PATH;
        }
    }
}
