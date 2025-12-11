using System;
using System.Globalization;
using System.Windows.Data;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class SnakeHeadOffsetConverter : IValueConverter
    {
        private const double SNAKE_HEAD_RADIUS_OFFSET = 0.20;
        private const double DEFAULT_OFFSET = 0.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double headCoordinate)
            {
                return headCoordinate - SNAKE_HEAD_RADIUS_OFFSET;
            }
            return DEFAULT_OFFSET;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
