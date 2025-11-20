using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SnakeAndLaddersFinalProject.Animation
{
    public sealed class DiceSpriteAnimator : INotifyPropertyChanged
    {
        private const int SPRITE_COLUMNS = 4;
        private const int SPRITE_ROWS = 4;

        private const int DEFAULT_LOOP_COUNT = 3;
        private const int FRAME_DELAY_MILLISECONDS = 40;

        private const string DEFAULT_FILE_EXTENSION = ".png";

        private readonly IReadOnlyList<BitmapSource> rollFrames;
        private readonly string finalFaceBasePath;
        private readonly string finalFaceFileExtension;
        private readonly Dispatcher dispatcher;

        private BitmapSource currentFrame;
        private bool isRolling;

        public event PropertyChangedEventHandler PropertyChanged;

        public BitmapSource CurrentFrame
        {
            get => currentFrame;
            private set => SetProperty(ref currentFrame, value);
        }

        public bool IsRolling
        {
            get => isRolling;
            private set => SetProperty(ref isRolling, value);
        }

        public DiceSpriteAnimator(string spriteSheetPath, string finalFaceBasePath)
            : this(spriteSheetPath, finalFaceBasePath, DEFAULT_FILE_EXTENSION)
        {
        }

        public DiceSpriteAnimator(string spriteSheetPath, string finalFaceBasePath, string finalFaceFileExtension)
        {
            dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            this.finalFaceBasePath = finalFaceBasePath ?? throw new ArgumentNullException(nameof(finalFaceBasePath));
            this.finalFaceFileExtension = string.IsNullOrWhiteSpace(finalFaceFileExtension)
                ? DEFAULT_FILE_EXTENSION
                : finalFaceFileExtension;

            rollFrames = LoadFramesFromSpriteSheet(spriteSheetPath, SPRITE_COLUMNS, SPRITE_ROWS);

            if (rollFrames.Count > 0)
            {
                CurrentFrame = rollFrames[0];
            }
        }

        public async Task RollAsync(int diceValue, int loopCount = DEFAULT_LOOP_COUNT)
        {
            if (IsRolling)
            {
                return;
            }

            if (diceValue < 1 || diceValue > 6)
            {
                throw new ArgumentOutOfRangeException(nameof(diceValue), "Dice value must be between 1 and 6.");
            }

            IsRolling = true;

            try
            {
                for (int loopIndex = 0; loopIndex < loopCount; loopIndex++)
                {
                    foreach (BitmapSource frame in rollFrames)
                    {
                        SetFrameOnUiThread(frame);
                        await Task.Delay(FRAME_DELAY_MILLISECONDS).ConfigureAwait(false);
                    }
                }

                BitmapSource finalFace = LoadFinalFaceImage(diceValue);
                SetFrameOnUiThread(finalFace);
            }
            finally
            {
                IsRolling = false;
            }
        }

        private void SetFrameOnUiThread(BitmapSource frame)
        {
            if (dispatcher.CheckAccess())
            {
                CurrentFrame = frame;
            }
            else
            {
                dispatcher.Invoke(() => CurrentFrame = frame);
            }
        }

        private static IReadOnlyList<BitmapSource> LoadFramesFromSpriteSheet(
    string spriteSheetPath,
    int columns,
    int rows)
        {
            if (string.IsNullOrWhiteSpace(spriteSheetPath))
            {
                throw new ArgumentException("Sprite sheet path cannot be null or empty.", nameof(spriteSheetPath));
            }

            var spriteSheet = new BitmapImage(
                new Uri(spriteSheetPath, UriKind.RelativeOrAbsolute));

            int cellSize = Math.Min(
                spriteSheet.PixelWidth / columns,
                spriteSheet.PixelHeight / rows);

            int totalWidth = cellSize * columns;
            int totalHeight = cellSize * rows;

            int offsetX = (spriteSheet.PixelWidth - totalWidth) / 2;
            int offsetY = (spriteSheet.PixelHeight - totalHeight) / 2;

            var frames = new List<BitmapSource>();

            for (int rowIndex = 0; rowIndex < rows; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < columns; columnIndex++)
                {
                    var frameRect = new Int32Rect(
                        offsetX + (columnIndex * cellSize),
                        offsetY + (rowIndex * cellSize),
                        cellSize,
                        cellSize);

                    var frame = new CroppedBitmap(spriteSheet, frameRect);
                    frame.Freeze();
                    frames.Add(frame);
                }
            }

            return frames;
        }

        private BitmapSource LoadFinalFaceImage(int diceValue)
        {
            string formattedNumber = diceValue.ToString("00", CultureInfo.InvariantCulture);
            string fileName = string.Concat(formattedNumber, "Dice", finalFaceFileExtension);
            string fullPath = string.Concat(finalFaceBasePath, fileName);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(fullPath, UriKind.RelativeOrAbsolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        private void SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
            {
                return;
            }

            backingField = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
