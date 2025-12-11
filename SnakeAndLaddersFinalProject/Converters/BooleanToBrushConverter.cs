using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class BooleanToBrushConverter : IValueConverter
    {
        private const string UNLOCKED_COLOR_HEX = "#FEE7BC";
        private const string LOCKED_COLOR_HEX = "#C7B29A";

        private static readonly SolidColorBrush _unlockedBrush =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(UNLOCKED_COLOR_HEX));

        private static readonly SolidColorBrush _lockedBrush =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(LOCKED_COLOR_HEX));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isUnlocked = value is bool boolean && boolean;
            return isUnlocked ? _unlockedBrush : _lockedBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
