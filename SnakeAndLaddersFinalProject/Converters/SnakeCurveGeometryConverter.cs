using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Converters
{
    public sealed class SnakeCurveGeometryConverter : IMultiValueConverter
    {
        private const double MIN_DISTANCE_TOLERANCE = 0.001;
        private const double CURVE_FACTOR = 0.25;
        private const double MAX_CURVE_OFFSET = 1.20;
        private const double ONE_THIRD_FACTOR = 3.0;
        private const double TWO_THIRDS_FACTOR = 2.0;
        private const double HALF_FACTOR = 0.5;

        private const int IDX_START_X = 0;
        private const int IDX_START_Y = 1;
        private const int IDX_END_X = 2;
        private const int IDX_END_Y = 3;
        private const int IDX_IS_LADDER = 4;
        private const int REQUIRED_VALUE_COUNT = 5;

        public object Convert(object[] values, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (values == null || values.Length < REQUIRED_VALUE_COUNT)
            {
                return Geometry.Empty;
            }

            if (!TryGetDouble(values[IDX_START_X], out double startX) ||
                !TryGetDouble(values[IDX_START_Y], out double startY) ||
                !TryGetDouble(values[IDX_END_X], out double endX) ||
                !TryGetDouble(values[IDX_END_Y], out double endY))
            {
                return Geometry.Empty;
            }

            bool isLadder = false;
            if (values[IDX_IS_LADDER] is bool ladderFlag)
            {
                isLadder = ladderFlag;
            }

            var startPoint = new Point(startX, startY);
            var endPoint = new Point(endX, endY);

            double deltaX = endX - startX;
            double deltaY = endY - startY;
            double distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));

            if (distance < MIN_DISTANCE_TOLERANCE || isLadder)
            {
                return new LineGeometry(startPoint, endPoint);
            }

            double perpendicularX = -deltaY / distance;
            double perpendicularY = deltaX / distance;

            double oneThirdX = startX + (deltaX / ONE_THIRD_FACTOR);
            double oneThirdY = startY + (deltaY / ONE_THIRD_FACTOR);

            double twoThirdsX = startX + (TWO_THIRDS_FACTOR * deltaX / ONE_THIRD_FACTOR);
            double twoThirdsY = startY + (TWO_THIRDS_FACTOR * deltaY / ONE_THIRD_FACTOR);

            double midX = (startX + endX) * HALF_FACTOR;
            double midY = (startY + endY) * HALF_FACTOR;
            var midPoint = new Point(midX, midY);

            double offset = distance * CURVE_FACTOR;
            if (offset > MAX_CURVE_OFFSET)
            {
                offset = MAX_CURVE_OFFSET;
            }

            var controlPoint1 = new Point(oneThirdX + (perpendicularX * offset), oneThirdY +
                (perpendicularY * offset));
            var controlPoint2 = new Point(twoThirdsX - (perpendicularX * offset), twoThirdsY -
                (perpendicularY * offset));

            var figure = new PathFigure
            {
                StartPoint = startPoint,
                IsClosed = false,
                IsFilled = false
            };

            figure.Segments.Add(new QuadraticBezierSegment(controlPoint1, midPoint, true));
            figure.Segments.Add(new QuadraticBezierSegment(controlPoint2, endPoint, true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            return geometry;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            CultureInfo culture)
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
                double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture,
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
