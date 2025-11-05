namespace SnakeAndLaddersFinalProject.Game
{
    public sealed class BoardCell
    {
        public int Index { get; }
        public int Row { get; }
        public int Column { get; }

        public BoardCell(int index, int row, int column)
        {
            Index = index;
            Row = row;
            Column = column;
        }
    }
}
