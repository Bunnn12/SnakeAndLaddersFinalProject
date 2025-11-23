using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SnakeAndLaddersFinalProject.Game;
using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.Animation
{
    public sealed class GameBoardAnimationService
    {
        private const int ANIMATION_STEP_MS = 120;
        private const int BOB_STEP_MS = 60;
        private const double BOB_OFFSET = -0.12;
        private const double MIN_DISTANCE_TOLERANCE = 0.001;
        private const double CURVE_FACTOR = 0.25;
        private const double MAX_CURVE_OFFSET = 1.20;

        private readonly PlayerTokenManager tokenManager;
        private readonly IReadOnlyDictionary<int, BoardLinkDto> linksByStartIndex;
        private readonly IReadOnlyDictionary<int, Point> cellCentersByIndex;
        private readonly Func<int, int> mapServerIndexToVisual;

        public GameBoardAnimationService(
            PlayerTokenManager tokenManager,
            IReadOnlyDictionary<int, BoardLinkDto> linksByStartIndex,
            IReadOnlyDictionary<int, Point> cellCentersByIndex,
            Func<int, int> mapServerIndexToVisual)
        {
            this.tokenManager = tokenManager
                ?? throw new ArgumentNullException(nameof(tokenManager));

            this.linksByStartIndex = linksByStartIndex
                ?? throw new ArgumentNullException(nameof(linksByStartIndex));

            this.cellCentersByIndex = cellCentersByIndex
                ?? throw new ArgumentNullException(nameof(cellCentersByIndex));

            this.mapServerIndexToVisual = mapServerIndexToVisual
                ?? throw new ArgumentNullException(nameof(mapServerIndexToVisual));
        }

        public bool IsAnimating { get; private set; }

        public async Task AnimateMoveForLocalPlayerAsync(
            int userId,
            int fromIndexServer,
            int toIndexServer,
            int diceValue)
        {
            int fromVisual = mapServerIndexToVisual(fromIndexServer);
            int toVisual = mapServerIndexToVisual(toIndexServer);

            PlayerTokenViewModel token = tokenManager.GetOrCreateTokenForUser(userId, fromVisual);

            IsAnimating = true;

            try
            {
                int landingIndexServer = fromIndexServer + diceValue;

                if (landingIndexServer > 0 &&
                    linksByStartIndex.TryGetValue(landingIndexServer, out BoardLinkDto usedLink))
                {
                    int landingVisual = mapServerIndexToVisual(landingIndexServer);

                    await AnimateWalkAsync(token, fromVisual, landingVisual).ConfigureAwait(false);

                    await AnimateLinkSlideAsync(token, usedLink).ConfigureAwait(false);
                }
                else
                {
                    await AnimateWalkAsync(token, fromVisual, toVisual).ConfigureAwait(false);
                }
            }
            finally
            {
                IsAnimating = false;
            }
        }

        private async Task AnimateWalkAsync(
            PlayerTokenViewModel token,
            int fromIndexVisual,
            int toIndexVisual)
        {
            if (token == null)
            {
                return;
            }

            if (fromIndexVisual == toIndexVisual)
            {
                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        tokenManager.UpdateTokenPositionFromCell(token, toIndexVisual);
                    });

                return;
            }

            int step = fromIndexVisual < toIndexVisual ? 1 : -1;

            for (int index = fromIndexVisual + step;
                 index != toIndexVisual + step;
                 index += step)
            {
                int cellIndex = index;

                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        tokenManager.UpdateTokenPositionFromCell(token, cellIndex);
                    });

                await BobTokenAsync(token).ConfigureAwait(false);
                await Task.Delay(ANIMATION_STEP_MS).ConfigureAwait(false);
            }
        }

        private async Task AnimateLinkSlideAsync(
            PlayerTokenViewModel token,
            BoardLinkDto link)
        {
            if (token == null || link == null)
            {
                return;
            }

            if (!cellCentersByIndex.TryGetValue(link.StartIndex, out Point start) ||
                !cellCentersByIndex.TryGetValue(link.EndIndex, out Point end))
            {
                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        tokenManager.UpdateTokenPositionFromCell(token, link.EndIndex);
                    });

                return;
            }

            IEnumerable<Point> points = link.IsLadder
                ? GetStraightPathPoints(start, end, 14)
                : GetSnakePathPoints(start, end, 20);

            foreach (Point point in points)
            {
                Point currentPoint = point;

                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        token.X = currentPoint.X;
                        token.Y = currentPoint.Y;
                    });

                await Task.Delay(ANIMATION_STEP_MS).ConfigureAwait(false);
            }

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    tokenManager.UpdateTokenPositionFromCell(token, link.EndIndex);
                });
        }

        private async Task BobTokenAsync(PlayerTokenViewModel token)
        {
            if (token == null)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    token.VerticalOffset = 0;
                });

            await Task.Delay(BOB_STEP_MS).ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    token.VerticalOffset = BOB_OFFSET;
                });

            await Task.Delay(BOB_STEP_MS).ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    token.VerticalOffset = 0;
                });

            await Task.Delay(BOB_STEP_MS).ConfigureAwait(false);
        }

        private static IEnumerable<Point> GetStraightPathPoints(
            Point start,
            Point end,
            int steps)
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

        private static IEnumerable<Point> GetSnakePathPoints(
            Point start,
            Point end,
            int steps)
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

            Point controlPoint1 = new Point(
                oneThirdX + (perpendicularX * offset),
                oneThirdY + (perpendicularY * offset));

            Point controlPoint2 = new Point(
                twoThirdsX - (perpendicularX * offset),
                twoThirdsY - (perpendicularY * offset));

            Point startPoint = start;
            Point midPoint = new Point(midX, midY);
            Point endPoint = end;

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

        private static Point EvaluateQuadraticBezier(
            Point p0,
            Point p1,
            Point p2,
            double t)
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
