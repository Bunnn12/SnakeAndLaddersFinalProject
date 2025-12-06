using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class TurnTimeToBrushConverter : IValueConverter
    {
        private const int TOTAL_SECONDS = 30;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string timeText = value as string;

            int remainingSeconds = ParseSeconds(timeText);
            if (remainingSeconds <= 0)
            {
                // rojo al final
                return new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
            }

            // >= 20s → verde
            // 20–10s → amarillo
            // < 10s → rojo
            Color color;

            if (remainingSeconds >= 20)
            {
                color = Color.FromRgb(0x4C, 0xAF, 0x50); // verde
            }
            else if (remainingSeconds >= 10)
            {
                color = Color.FromRgb(0xFF, 0xC1, 0x07); // amarillo
            }
            else
            {
                color = Color.FromRgb(0xE5, 0x39, 0x35); // rojo
            }

            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int minutes) &&
                int.TryParse(parts[1], out int seconds))
            {
                return (minutes * 60) + seconds;
            }

            return int.TryParse(timeText, out int onlySeconds)
                ? onlySeconds
                : 0;
        }
    }
}
