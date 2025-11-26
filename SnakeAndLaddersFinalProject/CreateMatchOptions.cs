using System;

namespace SnakeAndLaddersFinalProject
{
    public enum BoardSizeOption 
    { 
        EightByEight = 8, TenByTen = 10, TwelveByTwelve = 12 
    }
    public enum DifficultyOption 
    { 
        Easy = 0, Medium = 1, Hard = 2 
    }

    [Flags]
    public enum SpecialTileOptions { None = 0, Dice = 1, Message = 2, Trap = 4 }

    public sealed class CreateMatchOptions
    {
        public BoardSizeOption BoardSize { get; set; }
        public DifficultyOption Difficulty { get; set; }
        public SpecialTileOptions SpecialTiles { get; set; }
        public bool IsPrivate { get; set; }
        public int Players { get; set; }
        public string RoomKey { get; set; }
    }
}
