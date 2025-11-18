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
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
            {
                return null;
            }

            var tokens = values[0] as IEnumerable<PlayerTokenViewModel>;
            if (tokens == null)
            {
                return null;
            }

            if (!(values[1] is int cellIndex))
            {
                return null;
            }

            return tokens.Where(t => t.CurrentCellIndex == cellIndex).ToList();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
