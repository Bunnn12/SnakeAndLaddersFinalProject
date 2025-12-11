using System;
using System.Globalization;
using System.Windows.Data;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class TurnTimeToScaleConverter : IValueConverter
    {
        private const int TOTAL_SECONDS = 30;
        private const double MIN_SCALE_FACTOR = 0.25;
        private const double MIN_PROGRESS = 0.0;
        private const double MAX_PROGRESS = 1.0;
        private const double MAX_SCALE_FACTOR = 1.0;

        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            string timeText = value as string;

            if (string.IsNullOrWhiteSpace(timeText))
            {
                return MIN_SCALE_FACTOR;
            }

            if (!TimeSpan.TryParse(timeText, out TimeSpan timeSpan))
            {
                return MIN_SCALE_FACTOR;
            }

            int remainingSeconds = (int)timeSpan.TotalSeconds;
            if (remainingSeconds <= 0)
            {
                return MIN_SCALE_FACTOR;
            }

            double progress = Math.Min(MAX_PROGRESS, Math.Max(MIN_PROGRESS, remainingSeconds
                / (double)TOTAL_SECONDS));
            double scale = MIN_SCALE_FACTOR + (MAX_SCALE_FACTOR - MIN_SCALE_FACTOR) * progress;
            return scale;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
