using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class LadderGeometryConverter : IMultiValueConverter
    {
        private const double MIN_DISTANCE_TOLERANCE = 0.001;
        private const double RAIL_OFFSET_FACTOR = 0.10;
        private const double MAX_RAIL_OFFSET = 0.25;

        private const double STEPS_PER_UNIT = 1.2;
        private const int MIN_STEPS = 2;
        private const int MAX_STEPS = 7;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 4)
            {
                return Geometry.Empty;
            }

            if (!TryGetDouble(values[0], out double startX) ||
                !TryGetDouble(values[1], out double startY) ||
                !TryGetDouble(values[2], out double endX) ||
                !TryGetDouble(values[3], out double endY))
            {
                return Geometry.Empty;
            }

            var startPoint = new Point(startX, startY);
            var endPoint = new Point(endX, endY);

            double deltaX = endX - startX;
            double deltaY = endY - startY;
            double distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));

            if (distance < MIN_DISTANCE_TOLERANCE)
            {
                return new LineGeometry(startPoint, endPoint);
            }

            double directionX = deltaX / distance;
            double directionY = deltaY / distance;

            double perpendicularX = -directionY;
            double perpendicularY = directionX;

            double offset = distance * RAIL_OFFSET_FACTOR;
            if (offset > MAX_RAIL_OFFSET)
            {
                offset = MAX_RAIL_OFFSET;
            }

            var rail1Start = new Point(
                startX + (perpendicularX * offset),
                startY + (perpendicularY * offset));

            var rail1End = new Point(
                endX + (perpendicularX * offset),
                endY + (perpendicularY * offset));

            var rail2Start = new Point(
                startX - (perpendicularX * offset),
                startY - (perpendicularY * offset));

            var rail2End = new Point(
                endX - (perpendicularX * offset),
                endY - (perpendicularY * offset));

            var geometry = new PathGeometry();

            var rail1Figure = new PathFigure
            {
                StartPoint = rail1Start,
                IsClosed = false,
                IsFilled = false
            };
            rail1Figure.Segments.Add(new LineSegment(rail1End, true));
            geometry.Figures.Add(rail1Figure);

            var rail2Figure = new PathFigure
            {
                StartPoint = rail2Start,
                IsClosed = false,
                IsFilled = false
            };
            rail2Figure.Segments.Add(new LineSegment(rail2End, true));
            geometry.Figures.Add(rail2Figure);

            int stepCount = (int)(distance * STEPS_PER_UNIT);
            if (stepCount < MIN_STEPS)
            {
                stepCount = MIN_STEPS;
            }
            else if (stepCount > MAX_STEPS)
            {
                stepCount = MAX_STEPS;
            }

            for (int i = 1; i < stepCount; i++)
            {
                double t = i / (double)stepCount;

                double centerX = startX + (directionX * distance * t);
                double centerY = startY + (directionY * distance * t);

                var stepStart = new Point(
                    centerX + (perpendicularX * offset),
                    centerY + (perpendicularY * offset));

                var stepEnd = new Point(
                    centerX - (perpendicularX * offset),
                    centerY - (perpendicularY * offset));

                var stepFigure = new PathFigure
                {
                    StartPoint = stepStart,
                    IsClosed = false,
                    IsFilled = false
                };

                stepFigure.Segments.Add(new LineSegment(stepEnd, true));
                geometry.Figures.Add(stepFigure);
            }

            return geometry;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static bool TryGetDouble(object value, out double result)
        {
            if (value is double doubleValue)
            {
                result = doubleValue;
                return true;
            }

            if (value != null &&
                double.TryParse(
                    value.ToString(),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double parsedValue))
            {
                result = parsedValue;
                return true;
            }

            result = 0.0;
            return false;
        }
    }
}
