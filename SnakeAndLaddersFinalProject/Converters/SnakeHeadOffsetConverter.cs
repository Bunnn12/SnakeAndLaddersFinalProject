using System;
using System.Globalization;
using System.Windows.Data;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class SnakeHeadOffsetConverter : IValueConverter
    {
        private const double HEAD_RADIUS = 0.20; 

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double coord)
            {
                
                return coord - HEAD_RADIUS;
            }

            return 0d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
