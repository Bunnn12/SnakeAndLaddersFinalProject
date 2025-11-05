using System.Collections.Generic;

namespace SnakeAndLaddersFinalProject.Game
{
    public static class BoardFactory
    {
        private const int COLOR_PATTERN_MODULO = 2;

        public static IList<BoardCell> CreateBoard(BoardSizeOption boardSize)
        {
            var definition = BoardDefinition.FromBoardSize(boardSize);
            var cells = new List<BoardCell>(definition.CellCount);

            int currentIndex = 1;

            for (int row = definition.Rows - 1; row >= 0; row--)
            {
                int distanceFromBottom = definition.Rows - 1 - row; 
                bool isLeftToRight = (distanceFromBottom % COLOR_PATTERN_MODULO) == 0;

                if (isLeftToRight)
                {
                    for (int column = 0; column < definition.Columns; column++)
                    {
                        
                        int viewRow = distanceFromBottom;
                        int viewColumn = column;

                        bool isDark = ((viewRow + viewColumn) % COLOR_PATTERN_MODULO) == 0;

                        cells.Add(new BoardCell(currentIndex, row, column, isDark));
                        currentIndex++;
                    }
                }
                else
                {
                    for (int column = definition.Columns - 1; column >= 0; column--)
                    {
                        int viewRow = distanceFromBottom;
                        int viewColumn = definition.Columns - 1 - column;

                        bool isDark = ((viewRow + viewColumn) % COLOR_PATTERN_MODULO) == 0;

                        cells.Add(new BoardCell(currentIndex, row, column, isDark));
                        currentIndex++;
                    }
                }
            }

            return cells;
        }


    }
}
