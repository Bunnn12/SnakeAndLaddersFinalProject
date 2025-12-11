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

        private const int DICE_MIN_FACE_VALUE = 1;
        private const int DICE_MAX_FACE_VALUE = 6;

        private const string DEFAULT_FILE_EXTENSION = ".png";

        private const string FACE_NUMBER_FORMAT = "00";
        private const string DICE_FILE_NAME_SUFFIX = "Dice";

        private readonly IReadOnlyList<BitmapSource> _rollingFrames;
        private readonly string _finalFaceBasePath;
        private readonly string _finalFaceFileExtension;
        private readonly Dispatcher _uiDispatcher;

        private BitmapSource _currentFrame;
        private bool _isRolling;

        public event PropertyChangedEventHandler PropertyChanged;

        public BitmapSource CurrentFrame
        {
            get => _currentFrame;
            private set => SetProperty(ref _currentFrame, value);
        }

        public bool IsRolling
        {
            get => _isRolling;
            private set => SetProperty(ref _isRolling, value);
        }

        public DiceSpriteAnimator(string spriteSheetPath, string finalFaceBasePath)
            : this(spriteSheetPath, finalFaceBasePath, DEFAULT_FILE_EXTENSION)
        {
        }

        public DiceSpriteAnimator(string spriteSheetPath, string finalFaceBasePath,
            string finalFaceFileExtension)
        {
            _uiDispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            this._finalFaceBasePath = finalFaceBasePath ?? throw new ArgumentNullException(
                nameof(finalFaceBasePath));
            this._finalFaceFileExtension = string.IsNullOrWhiteSpace(finalFaceFileExtension)
                ? DEFAULT_FILE_EXTENSION
                : finalFaceFileExtension;

            _rollingFrames = LoadSpriteSheetFrames(spriteSheetPath, SPRITE_COLUMNS, SPRITE_ROWS);

            if (_rollingFrames.Count > 0)
            {
                CurrentFrame = _rollingFrames[0];
            }
        }

        public async Task PlayRollAnimationAsync(int diceValue, int loopCount = DEFAULT_LOOP_COUNT)
        {
            if (IsRolling)
            {
                return;
            }

            if (diceValue < DICE_MIN_FACE_VALUE || diceValue > DICE_MAX_FACE_VALUE)
            {
                throw new ArgumentOutOfRangeException(nameof(diceValue),
                    "Dice value must be between 1 and 6.");
            }

            IsRolling = true;

            try
            {
                for (int loopIndex = 0; loopIndex < loopCount; loopIndex++)
                {
                    foreach (BitmapSource frame in _rollingFrames)
                    {
                        UpdateCurrentFrameOnUiThread(frame);
                        await Task.Delay(FRAME_DELAY_MILLISECONDS).ConfigureAwait(false);
                    }
                }

                BitmapSource finalFace = LoadFinalDiceFaceImage(diceValue);
                UpdateCurrentFrameOnUiThread(finalFace);
            }
            finally
            {
                IsRolling = false;
            }
        }

        private void UpdateCurrentFrameOnUiThread(BitmapSource frame)
        {
            if (_uiDispatcher.CheckAccess())
            {
                CurrentFrame = frame;
            }
            else
            {
                _uiDispatcher.Invoke(() => CurrentFrame = frame);
            }
        }

        private static IReadOnlyList<BitmapSource> LoadSpriteSheetFrames(
            string spriteSheetPath, int columns, int rows)
        {
            if (string.IsNullOrWhiteSpace(spriteSheetPath))
            {
                throw new ArgumentException("Sprite sheet path cannot be null or empty.",
                    nameof(spriteSheetPath));
            }

            var spriteSheet = new BitmapImage(
                new Uri(spriteSheetPath, UriKind.RelativeOrAbsolute));

            int spriteCellSize = Math.Min(
                spriteSheet.PixelWidth / columns,
                spriteSheet.PixelHeight / rows);

            int spriteGridWidth = spriteCellSize * columns;
            int spriteGridHeight = spriteCellSize * rows;

            int offsetX = (spriteSheet.PixelWidth - spriteGridWidth) / 2;
            int offsetY = (spriteSheet.PixelHeight - spriteGridHeight) / 2;

            var spriteFrames = new List<BitmapSource>();

            for (int rowIndex = 0; rowIndex < rows; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < columns; columnIndex++)
                {
                    var frameRect = new Int32Rect(
                        offsetX + (columnIndex * spriteCellSize),
                        offsetY + (rowIndex * spriteCellSize),
                        spriteCellSize,
                        spriteCellSize);

                    var frame = new CroppedBitmap(spriteSheet, frameRect);
                    frame.Freeze();
                    spriteFrames.Add(frame);
                }
            }

            return spriteFrames;
        }

        private BitmapSource LoadFinalDiceFaceImage(int diceValue)
        {
            string formattedFaceNumber = diceValue.ToString(FACE_NUMBER_FORMAT,
                CultureInfo.InvariantCulture);
            string finalFaceFileName = string.Concat(formattedFaceNumber, DICE_FILE_NAME_SUFFIX,
                _finalFaceFileExtension);
            string finalFaceFullPath = string.Concat(_finalFaceBasePath, finalFaceFileName);

            var finalFaceBitmap = new BitmapImage();
            finalFaceBitmap.BeginInit();
            finalFaceBitmap.UriSource = new Uri(finalFaceFullPath, UriKind.RelativeOrAbsolute);
            finalFaceBitmap.CacheOption = BitmapCacheOption.OnLoad;
            finalFaceBitmap.EndInit();
            finalFaceBitmap.Freeze();

            return finalFaceBitmap;
        }

        private void SetProperty<T>(ref T backingField, T value, [CallerMemberName]
        string propertyName = "")
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
