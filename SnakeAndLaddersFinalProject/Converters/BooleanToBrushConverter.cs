using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class BooleanToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush UnlockedBrush =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE7BC"));

        private static readonly SolidColorBrush LockedBrush =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C7B29A"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isUnlocked = value is bool boolean && boolean;

            return isUnlocked ? UnlockedBrush : LockedBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
