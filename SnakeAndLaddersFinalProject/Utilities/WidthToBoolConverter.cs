using System;
using System.Globalization;
using System.Windows.Data;

namespace SnakeAndLaddersFinalProject.Utilities
{
    [ValueConversion(typeof(double), typeof(bool))]
    public sealed class WidthToBoolConverter : IValueConverter
    {
        private const double DEFAULT_WIDTH_THRESHOLD = 900.0;
        public double DefaultThreshold { get; set; } = DEFAULT_WIDTH_THRESHOLD;

        public bool Inclusive { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double width = 0.0;
            if (value is double widthValue)
            {
                width = double.IsNaN(widthValue) || double.IsInfinity(widthValue) ? 0.0 : Math.Max(0.0, widthValue);
            }

            double threshold = DefaultThreshold;

            if (parameter is double pd)
            {
                threshold = pd;
            }
            else if (parameter is string parameterText &&
                     double.TryParse(parameterText, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedThreshold))
            {
                threshold = parsedThreshold;
            }

            return Inclusive ? (width <= threshold) : (width < threshold);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("One-way converter.");
        }
    }
}
