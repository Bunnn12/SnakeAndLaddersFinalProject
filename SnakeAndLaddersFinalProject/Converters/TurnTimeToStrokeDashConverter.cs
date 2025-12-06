using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    /// <summary>
    /// Convierte el texto "mm:ss" en un patrón de StrokeDashArray
    /// para dibujar un arco proporcional (0–1) alrededor del avatar.
    /// </summary>
    public sealed class TurnTimeToStrokeDashConverter : IValueConverter
    {
        private const int TOTAL_SECONDS = 30;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string timeText = value as string;

            if (string.IsNullOrWhiteSpace(timeText) ||
                !TimeSpan.TryParse(timeText, out TimeSpan timeSpan))
            {
                // Círculo completo si no hay dato válido
                return new DoubleCollection { 1.0, 0.0 };
            }

            int remainingSeconds = (int)timeSpan.TotalSeconds;
            if (remainingSeconds < 0)
            {
                remainingSeconds = 0;
            }

            double progress = Math.Min(1.0, Math.Max(0.0, remainingSeconds / (double)TOTAL_SECONDS));

            // Un solo “dash” (arco visible) y luego el “gap” (resto del círculo)
            return new DoubleCollection
            {
                progress,          // parte pintada
                1.0 - progress     // parte vacía
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
