using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class CellTokenVisibleConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (values == null || values.Length < 2)
            {
                return Visibility.Collapsed;
            }

            if (!(values[0] is int currentCellIndex) ||
                !(values[1] is int cellIndex))
            {
                return Visibility.Collapsed;
            }

            return currentCellIndex == cellIndex
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
