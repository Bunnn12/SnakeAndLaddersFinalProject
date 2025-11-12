using System;
using System.Collections.Generic;
using System.Diagnostics;
using SnakeAndLaddersFinalProject.GameBoardService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardConnectionViewModel
    {
        private const double CELL_CENTER_OFFSET = 0.5;
        private const int MIN_INDEX = 1;
        private const string CONNECTION_LOG_PREFIX = "[GameBoardConnection]";

        public int StartIndex { get; }
        public int EndIndex { get; }
        public bool IsLadder { get; }

        public double StartX { get; }
        public double StartY { get; }
        public double EndX { get; }
        public double EndY { get; }

        public GameBoardConnectionViewModel(
            BoardLinkDto link,
            int rows,
            int columns,
            IList<GameBoardCellViewModel> cells)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (rows <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rows));
            }

            if (columns <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columns));
            }

            // Solo para mantener la firma compatible con GameBoardViewModel
            _ = cells;

            StartIndex = link.StartIndex;
            EndIndex = link.EndIndex;
            IsLadder = link.IsLadder;

            (int startRow, int startColumn) = CalculateCellCoordinates(StartIndex, rows, columns);
            (int endRow, int endColumn) = CalculateCellCoordinates(EndIndex, rows, columns);

            StartX = startColumn + CELL_CENTER_OFFSET;
            StartY = startRow + CELL_CENTER_OFFSET;

            EndX = endColumn + CELL_CENTER_OFFSET;
            EndY = endRow + CELL_CENTER_OFFSET;

            LogConnectionCoordinates();
        }

        private static (int rowFromTop, int columnFromLeft) CalculateCellCoordinates(
            int index,
            int rows,
            int columns)
        {
            int maxIndex = rows * columns;

            if (index < MIN_INDEX || index > maxIndex)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    "Index is outside the board limits.");
            }

            int zeroBasedIndex = index - MIN_INDEX;

            // Numeración lógica: desde ABAJO hacia ARRIBA
            int rowFromBottom = zeroBasedIndex / columns;
            int columnFromLeft = zeroBasedIndex % columns;

            // El Canvas / UniformGrid trabajan con filas desde ARRIBA,
            // así que invertimos la fila
            int rowFromTop = (rows - 1) - rowFromBottom;

            return (rowFromTop, columnFromLeft);
        }

        private void LogConnectionCoordinates()
        {
            string connectionType = IsLadder ? "LADDER" : "SNAKE";

            Debug.WriteLine(
                $"{CONNECTION_LOG_PREFIX} {connectionType} " +
                $"StartIndex={StartIndex} " +
                $"Start=({StartX:0.00}, {StartY:0.00}) " +
                $"EndIndex={EndIndex} " +
                $"End=({EndX:0.00}, {EndY:0.00})");
        }
    }
}
