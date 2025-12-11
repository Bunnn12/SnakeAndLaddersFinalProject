using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class CellTokenVisibleConverter : IMultiValueConverter
    {
        private const int IDX_CURRENT_CELL = 0;
        private const int IDX_TARGET_CELL = 1;
        private const int MIN_VALUES_COUNT = 2;

        public object Convert(object[] values, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (values == null || values.Length < MIN_VALUES_COUNT)
            {
                return Visibility.Collapsed;
            }

            if (!(values[IDX_CURRENT_CELL] is int currentCellIndex) ||
                !(values[IDX_TARGET_CELL] is int targetCellIndex))
            {
                return Visibility.Collapsed;
            }

            return currentCellIndex == targetCellIndex
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException("Not supported exception");
        }
    }
}
