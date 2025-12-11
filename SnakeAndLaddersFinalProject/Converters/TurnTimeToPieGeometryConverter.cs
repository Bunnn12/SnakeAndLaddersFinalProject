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

        private const int SECONDS_PER_MINUTE = 60;
        private const int DEFAULT_SECONDS = 0;
        private const int EXPECTED_TIME_PARTS = 2;
        private const int IDX_MINUTES = 0;
        private const int IDX_SECONDS = 1;

        private const double MIN_RATIO = 0.0;
        private const double MAX_RATIO = 1.0;
        private const double LARGE_ARC_THRESHOLD_DEGREES = 180.0;
        private const double DEGREES_PER_RADIAN = 180.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string timeText = value as string;
            int remainingSeconds = ParseSeconds(timeText);

            if (remainingSeconds <= DEFAULT_SECONDS)
            {
                return Geometry.Empty;
            }

            double ratio = remainingSeconds / (double)TOTAL_SECONDS;
            if (ratio <= MIN_RATIO)
            {
                return Geometry.Empty;
            }
            if (ratio > MAX_RATIO)
            {
                ratio = MAX_RATIO;
            }

            double sweepAngle = FULL_SWEEP_DEGREES * ratio;
            double startAngle = START_ANGLE_DEGREES;
            double endAngle = startAngle + sweepAngle;

            double startRad = DegreesToRadians(startAngle);
            double endRad = DegreesToRadians(endAngle);

            Point startPoint = new Point(CENTER_X + RADIUS * Math.Cos(startRad), CENTER_Y +
                RADIUS * Math.Sin(startRad));
            Point endPoint = new Point(CENTER_X + RADIUS * Math.Cos(endRad), CENTER_Y + RADIUS *
                Math.Sin(endRad));

            bool isLargeArc = sweepAngle > LARGE_ARC_THRESHOLD_DEGREES;

            var geometry = new StreamGeometry();
            using (StreamGeometryContext streamGeometryContext = geometry.Open())
            {
                streamGeometryContext.BeginFigure(new Point(0, 0), isFilled: false, isClosed: false);
                streamGeometryContext.LineTo(new Point(GEOMETRY_SIZE, 0), isStroked: false,
                    isSmoothJoin: false);
                streamGeometryContext.LineTo(new Point(GEOMETRY_SIZE, GEOMETRY_SIZE), isStroked: false,
                    isSmoothJoin: false);
                streamGeometryContext.LineTo(new Point(0, GEOMETRY_SIZE), isStroked: false,
                    isSmoothJoin: false);

                streamGeometryContext.BeginFigure(new Point(CENTER_X, CENTER_Y), isFilled: true,
                    isClosed: true);
                streamGeometryContext.LineTo(startPoint, isStroked: true, isSmoothJoin: true);
                streamGeometryContext.ArcTo(endPoint, new Size(RADIUS, RADIUS), rotationAngle: 0.0,
                    isLargeArc: isLargeArc, sweepDirection: SweepDirection.Clockwise,
                    isStroked: true, isSmoothJoin: true);
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
            if (parts.Length == EXPECTED_TIME_PARTS &&
                int.TryParse(parts[IDX_MINUTES], out int minutes) &&
                int.TryParse(parts[IDX_SECONDS], out int seconds))
            {
                return (minutes * SECONDS_PER_MINUTE) + seconds;
            }

            return int.TryParse(timeText, out int onlySeconds) ? onlySeconds : DEFAULT_SECONDS;
        }

        private static double DegreesToRadians(double degrees)
        {
            return (Math.PI / DEGREES_PER_RADIAN) * degrees;
        }
    }
}
