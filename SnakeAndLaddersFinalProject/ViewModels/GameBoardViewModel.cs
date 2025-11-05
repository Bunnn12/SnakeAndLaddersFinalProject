using System.Collections.Generic;
using SnakeAndLaddersFinalProject.Game;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel
    {
        public int Rows { get; }
        public int Columns { get; }
        public IList<BoardCell> Cells { get; }

        public GameBoardViewModel(CreateMatchOptions options)
        {
            var definition = BoardDefinition.FromBoardSize(options.BoardSize);

            Rows = definition.Rows;
            Columns = definition.Columns;
            Cells = BoardFactory.CreateBoard(options.BoardSize);
        }
    }
}
