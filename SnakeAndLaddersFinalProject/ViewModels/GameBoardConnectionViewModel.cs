using System;
using System.Collections.Generic;
using System.Linq;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardConnectionViewModel
    {
        public int StartIndex { get; }

        public int EndIndex { get; }

        public bool IsLadder { get; }

        // Coordenadas en unidades de celda (0..Columns, 0..Rows)
        public double StartX { get; }

        public double StartY { get; }

        public double EndX { get; }

        public double EndY { get; }

        public GameBoardConnectionViewModel(
            int startIndex,
            int endIndex,
            bool isLadder,
            int rows,
            int columns,
            IList<GameBoardCellViewModel> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (rows <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rows));
            }

            if (columns <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columns));
            }

            StartIndex = startIndex;
            EndIndex = endIndex;
            IsLadder = isLadder;

            var startCell = cells.FirstOrDefault(c => c.Index == startIndex);
            var endCell = cells.FirstOrDefault(c => c.Index == endIndex);

            if (startCell != null && endCell != null)
            {
                // X: 0 = columna izquierda, Columns = derecha
                // Y: 0 = fila inferior, Rows = fila superior
                StartX = startCell.Column + 0.5;
                StartY = (rows - 1 - startCell.Row) + 0.5;

                EndX = endCell.Column + 0.5;
                EndY = (rows - 1 - endCell.Row) + 0.5;
            }
            else
            {
                StartX = 0.0;
                StartY = 0.0;
                EndX = 0.0;
                EndY = 0.0;
            }
        }
    }
}
