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
        private const int MILLISECONDS_PER_SECOND = 1000;
        public const int SPRITE_COLUMNS = 4;
        public const int TOTAL_FRAMES = 16;
        public const int FIRST_FRAME_INDEX = 0;
        private const int MIN_FRAME_SIZE = 1;
        private const double MIN_FRAMES_PER_SECOND = 1.0;

        private readonly Image _targetImage;
        private readonly BitmapSource _spriteSheet;
        private readonly int _frameWidth;
        private readonly int _frameHeight;
        private readonly DispatcherTimer _frameTimer;

        private int _currentFrameIndex;

        public SpriteSheetAnimator(Image targetImage, BitmapSource spriteSheet,
            int frameWidth, int frameHeight, double framesPerSecond)
        {
            if (targetImage == null)
            {
                throw new ArgumentNullException(nameof(targetImage));
            }
            if (spriteSheet == null)
            {
                throw new ArgumentNullException(nameof(spriteSheet));
            }
            if (frameWidth < MIN_FRAME_SIZE)
            {
                throw new ArgumentOutOfRangeException(nameof(frameWidth));
            }
            if (frameHeight < MIN_FRAME_SIZE)
            {
                throw new ArgumentOutOfRangeException(nameof(frameHeight));
            }
            if (framesPerSecond < MIN_FRAMES_PER_SECOND)
            {
                throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
            }

            this._targetImage = targetImage;
            this._spriteSheet = spriteSheet;
            this._frameWidth = frameWidth;
            this._frameHeight = frameHeight;

            _frameTimer = new DispatcherTimer();

            _frameTimer.Interval = TimeSpan.FromMilliseconds(
                MILLISECONDS_PER_SECOND / framesPerSecond);
            _frameTimer.Tick += OnTimerTick;
        }
        public void Start()
        {
            _currentFrameIndex = FIRST_FRAME_INDEX;
            _frameTimer.Start();
        }
        public void Stop()
        {
            _frameTimer.Stop();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {

            Int32Rect sourceRect = CalculateSourceRect(_currentFrameIndex);
            CroppedBitmap frame = new CroppedBitmap(_spriteSheet, sourceRect);
            _targetImage.Source = frame;
            _currentFrameIndex = (_currentFrameIndex + 1) % TOTAL_FRAMES;
        }
        private Int32Rect CalculateSourceRect(int frameIndex)
        {
            int columnIndex = frameIndex % SPRITE_COLUMNS;
            int rowIndex = frameIndex / SPRITE_COLUMNS;

            int x = columnIndex * _frameWidth;
            int y = rowIndex * _frameHeight;

            return new Int32Rect(x, y, _frameWidth, _frameHeight);
        }
    }
}
