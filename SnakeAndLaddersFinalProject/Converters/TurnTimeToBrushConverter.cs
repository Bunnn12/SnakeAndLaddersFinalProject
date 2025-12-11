using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class TurnTimeToBrushConverter : IValueConverter
    {
        private const int HIGH_TIME_THRESHOLD_SECONDS = 20;
        private const int MEDIUM_TIME_THRESHOLD_SECONDS = 10;
        private const int SECONDS_PER_MINUTE = 60;
        private const int DEFAULT_SECONDS = 0;
        private const int IDX_MINUTES = 0;
        private const int IDX_SECONDS = 1;
        private const int TIME_PARTS_COUNT = 2;

        private static readonly Color _highTimeColor = Color.FromRgb(0x4C, 0xAF, 0x50);
        private static readonly Color _mediumTimeColor = Color.FromRgb(0xFF, 0xC1, 0x07);
        private static readonly Color _lowTimeColor = Color.FromRgb(0xE5, 0x39, 0x35);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string timeText = value as string;
            int remainingSeconds = ParseSeconds(timeText);

            if (remainingSeconds <= DEFAULT_SECONDS)
            {
                return new SolidColorBrush(_lowTimeColor);
            }

            Color color;
            if (remainingSeconds >= HIGH_TIME_THRESHOLD_SECONDS)
            {
                color = _highTimeColor;
            }
            else if (remainingSeconds >= MEDIUM_TIME_THRESHOLD_SECONDS)
            {
                color = _mediumTimeColor;
            }
            else
            {
                color = _lowTimeColor;
            }
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static int ParseSeconds(string timeText)
        {
            if (string.IsNullOrWhiteSpace(timeText))
            {
                return 0;
            }

            var parts = timeText.Split(':');
            if (parts.Length == TIME_PARTS_COUNT &&
                int.TryParse(parts[IDX_MINUTES], out int minutes) &&
                int.TryParse(parts[IDX_SECONDS], out int seconds))
            {
                return (minutes * SECONDS_PER_MINUTE) + seconds;
            }

            return int.TryParse(timeText, out int onlySeconds) ? onlySeconds : 0;
        }
    }
}
