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

        // Coordenadas en “unidades de celda” (0..Columns, 0..Rows)
        public double StartX { get; }
        public double StartY { get; }
        public double EndX { get; }
        public double EndY { get; }

        public GameBoardConnectionViewModel(
            SnakeAndLaddersFinalProject.GameBoardService.BoardLinkDto link,
            int rows,
            int columns,
            IList<GameBoardCellViewModel> cells)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));

            StartIndex = link.StartIndex;
            EndIndex = link.EndIndex;
            IsLadder = link.IsLadder;     // ladder = verde, snake = roja

            var startCell = cells.FirstOrDefault(c => c.Index == StartIndex);
            var endCell = cells.FirstOrDefault(c => c.Index == EndIndex);

            if (startCell != null && endCell != null)
            {
                // 🔴 IMPORTANTE: aquí usamos Row tal cual,
                // sin invertir (0 = fila de arriba, como en WPF)
                StartX = startCell.Column + 0.5;
                StartY = startCell.Row + 0.5;

                EndX = endCell.Column + 0.5;
                EndY = endCell.Row + 0.5;
            }
            else
            {
                StartX = StartY = EndX = EndY = 0.0;
            }
        }
    }
}
