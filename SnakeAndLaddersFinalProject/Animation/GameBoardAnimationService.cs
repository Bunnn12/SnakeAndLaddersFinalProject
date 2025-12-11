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
        private const int TOKEN_MOVE_DELAY_MS = 120;
        private const int BOB_ANIMATION_DELAY_MS = 60;
        private const double BOB_VERTICAL_OFFSET = -0.12;
        private const double MIN_DISTANCE_TOLERANCE = 0.001;
        private const double SNAKE_CURVE_FACTOR = 0.25;
        private const double MAX_SNAKE_CURVE_OFFSET = 1.20;
        private const int LADDER_PATH_STEPS = 14;
        private const int SNAKE_PATH_STEPS = 20;
        private const int MIN_SNAKE_PATH_STEPS = 4;
        private const double DEFAULT_VERTICAL_OFFSET = 0.0;

        private readonly PlayerTokenManager _tokenManager;
        private readonly IReadOnlyDictionary<int, BoardLinkDto> _linksByStartCellIndex;
        private readonly IReadOnlyDictionary<int, Point> _cellCentersByIndex;
        private readonly Func<int, int> _mapServerIndexToVisual;

        public GameBoardAnimationService(PlayerTokenManager tokenManager,
            IReadOnlyDictionary<int, BoardLinkDto> linksByStartIndex,
            IReadOnlyDictionary<int, Point> cellCentersByIndex,
            Func<int, int> mapServerIndexToVisual)
        {
            this._tokenManager = tokenManager
                ?? throw new ArgumentNullException(nameof(tokenManager));

            this._linksByStartCellIndex = linksByStartIndex
                ?? throw new ArgumentNullException(nameof(linksByStartIndex));

            this._cellCentersByIndex = cellCentersByIndex
                ?? throw new ArgumentNullException(nameof(cellCentersByIndex));

            this._mapServerIndexToVisual = mapServerIndexToVisual
                ?? throw new ArgumentNullException(nameof(mapServerIndexToVisual));
        }

        public bool IsAnimating { get; private set; }

        public async Task AnimateMoveForLocalPlayerAsync(int userId, int fromServerCellIndex,
            int toIndexServer, int diceValue)
        {
            int fromVisual = _mapServerIndexToVisual(fromServerCellIndex);
            int toVisual = _mapServerIndexToVisual(toIndexServer);

            PlayerTokenViewModel token = _tokenManager.GetOrCreateTokenForUser(userId, fromVisual);

            IsAnimating = true;

            try
            {
                int landingIndexServer = fromServerCellIndex + diceValue;

                if (landingIndexServer > 0 &&
                    _linksByStartCellIndex.TryGetValue(landingIndexServer, out BoardLinkDto link) &&
                    toIndexServer == link.EndIndex)
                {
                    int landingVisual = _mapServerIndexToVisual(landingIndexServer);

                    await AnimateTokenWalkAsync(token, fromVisual, landingVisual)
                        .ConfigureAwait(false);

                    await AnimateLinkSlideAsync(token, link).ConfigureAwait(false);
                }
                else
                {
                    await AnimateTokenWalkAsync(token, fromVisual, toVisual).ConfigureAwait(false);
                }
            }
            finally
            {
                IsAnimating = false;
            }
        }

        private async Task AnimateTokenWalkAsync(PlayerTokenViewModel token, int fromIndexVisual,
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
                        _tokenManager.UpdateTokenPositionFromCell(token, toIndexVisual);
                    });

                return;
            }

            int step = fromIndexVisual < toIndexVisual ? 1 : -1;

            for (int index = fromIndexVisual + step; index != toIndexVisual + step;
                 index += step)
            {
                int cellIndex = index;

                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        _tokenManager.UpdateTokenPositionFromCell(token, cellIndex);
                    });

                await AnimateTokenBobAsync(token).ConfigureAwait(false);
                await Task.Delay(TOKEN_MOVE_DELAY_MS).ConfigureAwait(false);
            }
        }

        private async Task AnimateLinkSlideAsync(PlayerTokenViewModel token, BoardLinkDto link)
        {
            if (token == null || link == null)
            {
                return;
            }

            if (!_cellCentersByIndex.TryGetValue(link.StartIndex, out Point start) ||
                !_cellCentersByIndex.TryGetValue(link.EndIndex, out Point end))
            {
                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        _tokenManager.UpdateTokenPositionFromCell(token, link.EndIndex);
                    });

                return;
            }

            IEnumerable<Point> points = link.IsLadder
                ? GetStraightPathPoints(start, end, LADDER_PATH_STEPS)
                : GetSnakePathPoints(start, end, SNAKE_PATH_STEPS);

            foreach (Point point in points)
            {
                Point currentPoint = point;

                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        token.X = currentPoint.X;
                        token.Y = currentPoint.Y;
                    });

                await Task.Delay(TOKEN_MOVE_DELAY_MS).ConfigureAwait(false);
            }

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    _tokenManager.UpdateTokenPositionFromCell(token, link.EndIndex);
                });
        }

        private static async Task AnimateTokenBobAsync(PlayerTokenViewModel token)
        {
            if (token == null)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    token.VerticalOffset = DEFAULT_VERTICAL_OFFSET;
                });

            await Task.Delay(BOB_ANIMATION_DELAY_MS).ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    token.VerticalOffset = BOB_VERTICAL_OFFSET;
                });

            await Task.Delay(BOB_ANIMATION_DELAY_MS).ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    token.VerticalOffset = DEFAULT_VERTICAL_OFFSET;
                });

            await Task.Delay(BOB_ANIMATION_DELAY_MS).ConfigureAwait(false);
        }

        private static IEnumerable<Point> GetStraightPathPoints(Point start, Point end,
            int stepCount)
        {
            if (stepCount <= 1)
            {
                yield return end;
                yield break;
            }

            for (int stepIndex = 1; stepIndex <= stepCount; stepIndex++)
            {
                double progress = stepIndex / (double)stepCount;
                double x = start.X + ((end.X - start.X) * progress);
                double y = start.Y + ((end.Y - start.Y) * progress);

                yield return new Point(x, y);
            }
        }

        private static IEnumerable<Point> GetSnakePathPoints(Point start, Point end, int steps)
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

            double offset = distance * SNAKE_CURVE_FACTOR;
            if (offset > MAX_SNAKE_CURVE_OFFSET)
            {
                offset = MAX_SNAKE_CURVE_OFFSET;
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

            if (steps < MIN_SNAKE_PATH_STEPS)
            {
                steps = MIN_SNAKE_PATH_STEPS;
            }

            for (int i = 1; i <= steps; i++)
            {
                double globalProgress = i / (double)steps;

                Point pointOnPath;
                if (globalProgress <= 0.5)
                {
                    double segmentProgress = globalProgress * 2.0;
                    pointOnPath = EvaluateQuadraticBezier(startPoint, controlPoint1, midPoint,
                        segmentProgress);
                }
                else
                {
                    double u = (globalProgress - 0.5) * 2.0;
                    pointOnPath = EvaluateQuadraticBezier(midPoint, controlPoint2, endPoint, u);
                }

                yield return pointOnPath;
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
