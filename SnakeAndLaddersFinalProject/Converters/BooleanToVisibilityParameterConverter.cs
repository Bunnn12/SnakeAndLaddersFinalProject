using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class BooleanToVisibilityParameterConverter : IValueConverter
    {
        private const string INVERT_PARAMETER = "Invert";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool boolean && boolean;

            bool invert = string.Equals(
                parameter as string,
                INVERT_PARAMETER,
                StringComparison.OrdinalIgnoreCase);

            if (invert)
            {
                flag = !flag;
            }

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
