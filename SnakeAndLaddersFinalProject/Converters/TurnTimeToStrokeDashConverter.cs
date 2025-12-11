using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class TurnTimeToStrokeDashConverter : IValueConverter
    {
        private const int TURN_TOTAL_SECONDS = 30;
        private const double MIN_PROGRESS = 0.0;
        private const double MAX_PROGRESS = 1.0;
        private const double DEFAULT_DASH_VISIBLE = 1.0;
        private const double DEFAULT_DASH_HIDDEN = 0.0;
        private const int MIN_REMAINING_SECONDS = 0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string timeText = value as string;

            if (string.IsNullOrWhiteSpace(timeText) ||
                !TimeSpan.TryParse(timeText, culture, out TimeSpan timeSpan))
            {
                return new DoubleCollection { DEFAULT_DASH_VISIBLE, DEFAULT_DASH_HIDDEN };
            }

            int remainingSeconds = (int)timeSpan.TotalSeconds;
            if (remainingSeconds < MIN_REMAINING_SECONDS)
            {
                remainingSeconds = MIN_REMAINING_SECONDS;
            }

            double progress = Math.Min(
                MAX_PROGRESS,
                Math.Max(
                    MIN_PROGRESS,
                    remainingSeconds / (double)TURN_TOTAL_SECONDS));

            return new DoubleCollection { progress, MAX_PROGRESS - progress };
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
