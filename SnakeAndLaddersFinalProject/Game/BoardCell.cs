namespace SnakeAndLaddersFinalProject.Game
{
    

    public sealed class BoardCell
    {
        public int Index { get; }

        public int Row { get; }

        public int Column { get; }

        public bool IsDark { get; }

        public SpecialCellType SpecialType { get; }

        public bool IsBonus
        {
            get { return (SpecialType & SpecialCellType.Bonus) == SpecialCellType.Bonus; }
        }

        public bool IsTrap
        {
            get { return (SpecialType & SpecialCellType.Trap) == SpecialCellType.Trap; }
        }

        public bool IsTeleport
        {
            get { return (SpecialType & SpecialCellType.Teleport) == SpecialCellType.Teleport; }
        }

        public BoardCell(int index, int row, int column, bool isDark)
            : this(index, row, column, isDark, SpecialCellType.None)
        {
        }

        public BoardCell(
            int index,
            int row,
            int column,
            bool isDark,
            SpecialCellType specialType)
        {
            Index = index;
            Row = row;
            Column = column;
            IsDark = isDark;
            SpecialType = specialType;
        }
    }
}
