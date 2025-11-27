using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class PlayerColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush[] PLAYER_BRUSHES =
        {
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEF476F")), 
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF06D6A0")), 
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF118AB2")), 
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFD166"))  
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int userId && PLAYER_BRUSHES.Length > 0)
            {
                int index = Math.Abs(userId) % PLAYER_BRUSHES.Length;
                return PLAYER_BRUSHES[index];
            }

            return PLAYER_BRUSHES.Length > 0
                ? PLAYER_BRUSHES[0]
                : Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
