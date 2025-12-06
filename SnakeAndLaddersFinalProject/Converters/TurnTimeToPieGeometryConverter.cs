using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class TurnTimeToPieGeometryConverter : IValueConverter
    {
        private const int TOTAL_SECONDS = 30;

        private const double GEOMETRY_SIZE = 100.0;
        private const double CENTER_X = GEOMETRY_SIZE / 2.0;
        private const double CENTER_Y = GEOMETRY_SIZE / 2.0;
        private const double RADIUS = GEOMETRY_SIZE / 2.0;

        private const double START_ANGLE_DEGREES = -90.0;
        private const double FULL_SWEEP_DEGREES = 359.999;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string timeText = value as string;
            int remainingSeconds = ParseSeconds(timeText);

            if (remainingSeconds <= 0)
            {
                return Geometry.Empty;
            }

            double ratio = remainingSeconds / (double)TOTAL_SECONDS;

            if (ratio <= 0.0)
            {
                return Geometry.Empty;
            }

            if (ratio > 1.0)
            {
                ratio = 1.0;
            }

            double sweepAngle = FULL_SWEEP_DEGREES * ratio;

            double startAngle = START_ANGLE_DEGREES;
            double endAngle = startAngle + sweepAngle;

            double startRad = DegreesToRadians(startAngle);
            double endRad = DegreesToRadians(endAngle);

            Point startPoint = new Point(
                CENTER_X + RADIUS * Math.Cos(startRad),
                CENTER_Y + RADIUS * Math.Sin(startRad));

            Point endPoint = new Point(
                CENTER_X + RADIUS * Math.Cos(endRad),
                CENTER_Y + RADIUS * Math.Sin(endRad));

            bool isLargeArc = sweepAngle > 180.0;

            var geometry = new StreamGeometry();

            using (StreamGeometryContext ctx = geometry.Open())
            {
                // 1) Figura "dummy" para fijar siempre el bounding box 0,0 - 100,100
                ctx.BeginFigure(new Point(0, 0), isFilled: false, isClosed: false);
                ctx.LineTo(new Point(GEOMETRY_SIZE, 0), isStroked: false, isSmoothJoin: false);
                ctx.LineTo(new Point(GEOMETRY_SIZE, GEOMETRY_SIZE), isStroked: false, isSmoothJoin: false);
                ctx.LineTo(new Point(0, GEOMETRY_SIZE), isStroked: false, isSmoothJoin: false);

                // 2) Figura real del pastel
                ctx.BeginFigure(new Point(CENTER_X, CENTER_Y), isFilled: true, isClosed: true);
                ctx.LineTo(startPoint, isStroked: true, isSmoothJoin: true);
                ctx.ArcTo(
                    endPoint,
                    new Size(RADIUS, RADIUS),
                    rotationAngle: 0.0,
                    isLargeArc: isLargeArc,
                    sweepDirection: SweepDirection.Clockwise,
                    isStroked: true,
                    isSmoothJoin: true);
            }

            geometry.Freeze();
            return geometry;
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

            string[] parts = timeText.Split(':');

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

        private static double DegreesToRadians(double degrees)
        {
            return (Math.PI / 180.0) * degrees;
        }
    }
}
