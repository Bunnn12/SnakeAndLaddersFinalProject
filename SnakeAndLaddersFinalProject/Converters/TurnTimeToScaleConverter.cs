using System;
using System.Globalization;
using System.Windows.Data;

namespace SnakeAndLaddersFinalProject.Converters
{
    /// <summary>
    /// Convierte el texto "mm:ss" de TurnTimerText a un factor de escala (0–1)
    /// para representar el progreso del turno.
    /// </summary>
    public sealed class TurnTimeToScaleConverter : IValueConverter
    {
        private const int TOTAL_SECONDS = 30;
        private const double MIN_SCALE = 0.25;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string timeText = value as string;

            if (string.IsNullOrWhiteSpace(timeText))
            {
                return MIN_SCALE;
            }

            if (!TimeSpan.TryParse(timeText, out TimeSpan timeSpan))
            {
                return MIN_SCALE;
            }

            int remainingSeconds = (int)timeSpan.TotalSeconds;
            if (remainingSeconds <= 0)
            {
                return MIN_SCALE;
            }

            double progress = Math.Min(1.0, Math.Max(0.0, remainingSeconds / (double)TOTAL_SECONDS));

            double scale = MIN_SCALE + (1.0 - MIN_SCALE) * progress;
            return scale;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
