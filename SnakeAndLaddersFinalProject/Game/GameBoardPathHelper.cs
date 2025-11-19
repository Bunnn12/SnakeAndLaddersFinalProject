using System;
using System.Collections.Generic;
using System.Windows;

namespace SnakeAndLaddersFinalProject.Game
{
    public static class GameBoardPathHelper
    {
        private const double MIN_DISTANCE_TOLERANCE = 0.001;
        private const double CURVE_FACTOR = 0.25;
        private const double MAX_CURVE_OFFSET = 1.20;

        public static IEnumerable<Point> GetStraightPathPoints(Point start, Point end, int steps)
        {
            if (steps <= 1)
            {
                yield return end;
                yield break;
            }

            for (int i = 1; i <= steps; i++)
            {
                double t = i / (double)steps;
                double x = start.X + ((end.X - start.X) * t);
                double y = start.Y + ((end.Y - start.Y) * t);

                yield return new Point(x, y);
            }
        }

        public static IEnumerable<Point> GetSnakePathPoints(Point start, Point end, int steps)
        {
            double startX = start.X;
            double startY = start.Y;
            double endX = end.X;
            double endY = end.Y;

            double deltaX = endX - startX;
            double deltaY = endY - startY;
            double distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));

            if (distance < MIN_DISTANCE_TOLERANCE)
            {
                yield return end;
                yield break;
            }

            double perpendicularX = -deltaY / distance;
            double perpendicularY = deltaX / distance;

            double oneThirdX = startX + (deltaX / 3.0);
            double oneThirdY = startY + (deltaY / 3.0);

            double twoThirdsX = startX + (2.0 * deltaX / 3.0);
            double twoThirdsY = startY + (2.0 * deltaY / 3.0);

            double midX = (startX + endX) * 0.5;
            double midY = (startY + endY) * 0.5;

            double offset = distance * CURVE_FACTOR;
            if (offset > MAX_CURVE_OFFSET)
            {
                offset = MAX_CURVE_OFFSET;
            }

            var controlPoint1 = new Point(
                oneThirdX + (perpendicularX * offset),
                oneThirdY + (perpendicularY * offset));

            var controlPoint2 = new Point(
                twoThirdsX - (perpendicularX * offset),
                twoThirdsY - (perpendicularY * offset));

            var startPoint = start;
            var midPoint = new Point(midX, midY);
            var endPoint = end;

            if (steps < 4)
            {
                steps = 4;
            }

            for (int i = 1; i <= steps; i++)
            {
                double tGlobal = i / (double)steps;

                Point p;
                if (tGlobal <= 0.5)
                {
                    double u = tGlobal * 2.0;
                    p = EvaluateQuadraticBezier(startPoint, controlPoint1, midPoint, u);
                }
                else
                {
                    double u = (tGlobal - 0.5) * 2.0;
                    p = EvaluateQuadraticBezier(midPoint, controlPoint2, endPoint, u);
                }

                yield return p;
            }
        }

        private static Point EvaluateQuadraticBezier(Point p0, Point p1, Point p2, double t)
        {
            double oneMinusT = 1.0 - t;

            double x =
                (oneMinusT * oneMinusT * p0.X) +
                (2 * oneMinusT * t * p1.X) +
                (t * t * p2.X);

            double y =
                (oneMinusT * oneMinusT * p0.Y) +
                (2 * oneMinusT * t * p1.Y) +
                (t * t * p2.Y);

            return new Point(x, y);
        }
    }
}
