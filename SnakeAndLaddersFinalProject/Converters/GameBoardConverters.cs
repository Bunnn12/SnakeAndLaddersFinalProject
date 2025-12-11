using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class IsSnakeToColorConverter : IValueConverter
    {
        private static readonly Color _snakeColor = Color.FromRgb(220, 38, 38);
        private static readonly Color _nonSnakeColor = Color.FromRgb(56, 161, 105);
        private static readonly SolidColorBrush _snakeBrush = new SolidColorBrush(_snakeColor);
        private static readonly SolidColorBrush _nonSnakeBrush =
            new SolidColorBrush(_nonSnakeColor);

        static IsSnakeToColorConverter()
        {
            _snakeBrush.Freeze();
            _nonSnakeBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSnake = value is bool booleanValue && booleanValue;
            return isSnake ? _snakeBrush : _nonSnakeBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
            => throw new NotSupportedException("Not supported exception");
    }

    public sealed class IsSnakeToStyleConverter : IValueConverter
    {
        private const double SNAKE_DASH_LENGTH = 4.0;
        private const double SNAKE_GAP_LENGTH = 2.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSnake = value is bool booleanValue && booleanValue;
            return isSnake
                ? new DoubleCollection { SNAKE_DASH_LENGTH, SNAKE_GAP_LENGTH }
                : new DoubleCollection();
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
            => throw new NotSupportedException("Not supported exception");
    }
}
