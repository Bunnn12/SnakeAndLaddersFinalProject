using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class PlayerColorConverter : IValueConverter
    {
        private const string PLAYER_COLOR_1_HEX = "#FFEF476F";
        private const string PLAYER_COLOR_2_HEX = "#FF06D6A0";
        private const string PLAYER_COLOR_3_HEX = "#FF118AB2";
        private const string PLAYER_COLOR_4_HEX = "#FFFFD166";
        private const int MIN_BRUSHES_COUNT = 0;
        private const int FIRST_BRUSH_INDEX = 0;

        private static readonly SolidColorBrush _fallbackBrush = Brushes.White;

        private static readonly SolidColorBrush[] _playerBrushes =
        {
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(PLAYER_COLOR_1_HEX)),
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(PLAYER_COLOR_2_HEX)),
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(PLAYER_COLOR_3_HEX)),
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(PLAYER_COLOR_4_HEX))
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int userId && _playerBrushes.Length > MIN_BRUSHES_COUNT)
            {
                int playerBrushIndex = Math.Abs(userId) % _playerBrushes.Length;
                return _playerBrushes[playerBrushIndex];
            }

            return _playerBrushes.Length > MIN_BRUSHES_COUNT ? _playerBrushes[FIRST_BRUSH_INDEX] :
                _fallbackBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
