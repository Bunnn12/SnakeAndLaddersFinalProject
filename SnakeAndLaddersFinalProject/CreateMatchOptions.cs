using System;

namespace SnakeAndLaddersFinalProject
{
    public enum BoardSizeOption 
    { 
        EightByEight = 7, TenByTen = 10, TwelveByTwelve = 12 
    }
    public enum DifficultyOption 
    { 
        Easy = 0, Medium = 1, Hard = 2 
    }

    [Flags]
    public enum SpecialTileOptions { None = 0, Dice = 1, Message = 2, Trap = 4 }

    public sealed class CreateMatchOptions
    {
        public BoardSizeOption BoardSize { get; set; } = BoardSizeOption.TenByTen;
        public DifficultyOption Difficulty { get; set; } = DifficultyOption.Medium;
        public SpecialTileOptions SpecialTiles { get; set; } = SpecialTileOptions.None;
        public bool IsPrivate { get; set; }
        public string RoomKey { get; set; } = string.Empty;
        public int Players { get; set; } = AppConstants.MIN_PLAYERS_TO_START;
    }
}
