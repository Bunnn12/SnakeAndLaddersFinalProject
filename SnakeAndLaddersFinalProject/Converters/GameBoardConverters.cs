using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class IsSnakeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSnake = value is bool b && b;
            return isSnake
                ? new SolidColorBrush(Color.FromRgb(220, 38, 38))   
                : new SolidColorBrush(Color.FromRgb(56, 161, 105)); 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public sealed class IsSnakeToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSnake = value is bool b && b;

            return isSnake ? new DoubleCollection { 4, 2 } : new DoubleCollection();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
