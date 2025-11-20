using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Controls;


namespace SnakeAndLaddersFinalProject.Animation
{
    public sealed class SpriteSheetAnimator
    {
        private const int MILLISECONDS_IN_SECOND = 1000;
        public const int SPRITE_COLUMNS = 4;
        public const int TOTAL_FRAMES = 16;

        private readonly Image targetImage;
        private readonly BitmapSource spriteSheet;
        private readonly int frameWidth;
        private readonly int frameHeight;
        private readonly DispatcherTimer timer;

        private int currentFrameIndex;

        public SpriteSheetAnimator(
            Image targetImage,
            BitmapSource spriteSheet,
            int frameWidth,
            int frameHeight,
            double framesPerSecond)
        {
            if (targetImage == null) throw new ArgumentNullException(nameof(targetImage));
            if (spriteSheet == null) throw new ArgumentNullException(nameof(spriteSheet));
            if (frameWidth <= 0) throw new ArgumentOutOfRangeException(nameof(frameWidth));
            if (frameHeight <= 0) throw new ArgumentOutOfRangeException(nameof(frameHeight));
            if (framesPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(framesPerSecond));

            this.targetImage = targetImage;
            this.spriteSheet = spriteSheet;
            this.frameWidth = frameWidth;
            this.frameHeight = frameHeight;

            timer = new DispatcherTimer();

            timer.Interval = TimeSpan.FromMilliseconds(
                MILLISECONDS_IN_SECOND / framesPerSecond);
            timer.Tick += OnTimerTick;
        }
        public void Start()
        {
            currentFrameIndex = 0;
            timer.Start();
        }
        public void Stop()
        {
            timer.Stop();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {

            Int32Rect sourceRect = CalculateSourceRect(currentFrameIndex);
            CroppedBitmap frame = new CroppedBitmap(spriteSheet, sourceRect);
            targetImage.Source = frame;
            currentFrameIndex = (currentFrameIndex + 1) % TOTAL_FRAMES;
        }
        private Int32Rect CalculateSourceRect(int frameIndex)
        {
            int columnIndex = frameIndex % SPRITE_COLUMNS;
            int rowIndex = frameIndex / SPRITE_COLUMNS;

            int x = columnIndex * frameWidth;
            int y = rowIndex * frameHeight;

            return new Int32Rect(x, y, frameWidth, frameHeight);
        }
    }
}
