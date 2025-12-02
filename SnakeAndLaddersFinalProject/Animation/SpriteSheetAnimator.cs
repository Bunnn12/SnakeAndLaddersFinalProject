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

        private readonly Image _targetImage;
        private readonly BitmapSource _spriteSheet;
        private readonly int _frameWidth;
        private readonly int _frameHeight;
        private readonly DispatcherTimer _timer;

        private int _currentFrameIndex;

        public SpriteSheetAnimator(
            Image targetImage,
            BitmapSource spriteSheet,
            int frameWidth,
            int frameHeight,
            double framesPerSecond)
        {
            if (targetImage == null)
            {
                throw new ArgumentNullException(nameof(targetImage));
            }
            if (spriteSheet == null)
            {
                throw new ArgumentNullException(nameof(spriteSheet));
            }
            if (frameWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameWidth));
            }
            if (frameHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameHeight));
            }
            if (framesPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
            }

            this._targetImage = targetImage;
            this._spriteSheet = spriteSheet;
            this._frameWidth = frameWidth;
            this._frameHeight = frameHeight;

            _timer = new DispatcherTimer();

            _timer.Interval = TimeSpan.FromMilliseconds(
                MILLISECONDS_IN_SECOND / framesPerSecond);
            _timer.Tick += OnTimerTick;
        }
        public void Start()
        {
            _currentFrameIndex = 0;
            _timer.Start();
        }
        public void Stop()
        {
            _timer.Stop();
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
