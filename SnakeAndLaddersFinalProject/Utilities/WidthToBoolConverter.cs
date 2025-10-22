using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SnakeAndLaddersFinalProject.Utilities
{
    /// <summary>
    /// Devuelve true si el ancho (double) es menor al umbral. Permite override por ConverterParameter.
    /// </summary>
    [ValueConversion(typeof(double), typeof(bool))]
    public sealed class WidthToBoolConverter : IValueConverter
    {
        /// <summary>Umbral por defecto cuando no se especifica ConverterParameter.</summary>
        public double DefaultThreshold { get; set; } = 900;

        /// <summary>
        /// Si es true, aplica comparación <= (en vez de <). Útil para bordes exactos del breakpoint.
        /// </summary>
        public bool Inclusive { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Ancho actual
            double width = 0.0;
            if (value is double d)
                width = double.IsNaN(d) || double.IsInfinity(d) ? 0.0 : Math.Max(0.0, d);

            // Umbral efectivo: parameter (double/string) o DefaultThreshold
            double threshold = DefaultThreshold;

            if (parameter is double pd)
            {
                threshold = pd;
            }
            else if (parameter is string ps &&
                     double.TryParse(ps, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                threshold = parsed;
            }

            // Comparación (elige < o <= según Inclusive)
            return Inclusive ? (width <= threshold) : (width < threshold);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("One-way converter.");
        }
    }
}
