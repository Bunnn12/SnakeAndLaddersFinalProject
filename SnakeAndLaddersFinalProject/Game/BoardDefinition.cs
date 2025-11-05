namespace SnakeAndLaddersFinalProject.Game
{
    public sealed class BoardDefinition
    {
        private const int MIN_BOARD_SIDE = 1;
        private const BoardSizeOption DEFAULT_BOARD_SIZE = BoardSizeOption.TenByTen;

        public int Rows { get; }
        public int Columns { get; }

        public int CellCount
        {
            get { return Rows * Columns; }
        }

        private BoardDefinition(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
        }

        public static BoardDefinition FromBoardSize(BoardSizeOption boardSize)
        {
            int side = (int)boardSize;

            if (side < MIN_BOARD_SIDE)
            {
                side = (int)DEFAULT_BOARD_SIZE;
            }

            return new BoardDefinition(side, side);
        }
    }
}
