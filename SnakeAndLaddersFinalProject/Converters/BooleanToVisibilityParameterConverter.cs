using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class BooleanToVisibilityParameterConverter : IValueConverter
    {
        private const string INVERT_PARAMETER = "Invert";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = value is bool boolean && boolean;

            bool shouldInvert = string.Equals(
                parameter as string,
                INVERT_PARAMETER,
                StringComparison.OrdinalIgnoreCase);

            if (shouldInvert)
            {
                isVisible = !isVisible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
