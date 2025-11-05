using System.Collections.Generic;

namespace SnakeAndLaddersFinalProject.Game
{
    public static class BoardFactory
    {
        public static IList<BoardCell> CreateBoard(BoardSizeOption boardSize)
        {
            var definition = BoardDefinition.FromBoardSize(boardSize);
            var cells = new List<BoardCell>(definition.CellCount);

            int currentIndex = 1;

            
            for (int row = definition.Rows - 1; row >= 0; row--)
            {
                int distanceFromBottom = definition.Rows - 1 - row;
                bool isLeftToRight = (distanceFromBottom % 2) == 0;

                if (isLeftToRight)
                {
                    for (int column = 0; column < definition.Columns; column++)
                    {
                        cells.Add(new BoardCell(currentIndex, row, column));
                        currentIndex++;
                    }
                }
                else
                {
                    for (int column = definition.Columns - 1; column >= 0; column--)
                    {
                        cells.Add(new BoardCell(currentIndex, row, column));
                        currentIndex++;
                    }
                }
            }

            return cells;
        }
    }
}
