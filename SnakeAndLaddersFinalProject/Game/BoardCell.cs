namespace SnakeAndLaddersFinalProject.Game
{
    public sealed class BoardCell
    {
        public int Index { get; }
        public int Row { get; }
        public int Column { get; }

        public bool IsDark { get; set; }


        public BoardCell(int index, int row, int column, bool isDark)
        {
            Index = index;
            Row = row;
            Column = column;
            IsDark = isDark;
        }
    }
}
