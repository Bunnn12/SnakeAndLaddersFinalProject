using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class TokensForCellConverter : IMultiValueConverter
    {
        private const int MIN_EXPECTED_VALUES_COUNT = 2;
        private const int IDX_TOKENS = 0;
        private const int IDX_CELL_INDEX = 1;

        public object Convert(object[] values, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (values == null || values.Length < MIN_EXPECTED_VALUES_COUNT)
            {
                return Enumerable.Empty<PlayerTokenViewModel>();
            }

            if (!(values[IDX_TOKENS] is IEnumerable<PlayerTokenViewModel> allTokens))
            {
                return Enumerable.Empty<PlayerTokenViewModel>();
            }

            if (!(values[IDX_CELL_INDEX] is int cellIndex))
            {
                return Enumerable.Empty<PlayerTokenViewModel>();
            }

            return allTokens.Where(token => token.CurrentCellIndex == cellIndex).ToList();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
