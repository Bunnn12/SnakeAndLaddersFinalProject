using System;

namespace SnakeAndLaddersFinalProject.Mappers
{
    internal static class LobbyMapper
    {
        private const string DIFFICULTY_EASY = "Easy";
        private const string DIFFICULTY_NORMAL = "Normal";
        private const string DIFFICULTY_HARD = "Hard";

        public static string MapDifficultyToServerString(DifficultyOption value)
        {
            switch (value)
            {
                case DifficultyOption.Easy:
                    return DIFFICULTY_EASY;
                case DifficultyOption.Hard:
                    return DIFFICULTY_HARD;
                default:
                    return DIFFICULTY_NORMAL;
            }
        }

        public static BoardSizeOption MapBoardSize(int boardSide)
        {
            switch (boardSide)
            {
                case 8:
                    return BoardSizeOption.EightByEight;
                case 12:
                    return BoardSizeOption.TwelveByTwelve;
                case 10:
                default:
                    return BoardSizeOption.TenByTen;
            }
        }

        public static DifficultyOption MapDifficultyFromServer(string difficulty)
        {
            if (string.Equals(difficulty, DIFFICULTY_EASY, StringComparison.OrdinalIgnoreCase))
            {
                return DifficultyOption.Easy;
            }

            if (string.Equals(difficulty, DIFFICULTY_HARD, StringComparison.OrdinalIgnoreCase))
            {
                return DifficultyOption.Hard;
            }

            return DifficultyOption.Medium;
        }

        public static SpecialTileOptions MapSpecialTiles(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return SpecialTileOptions.None;
            }

            if (Enum.TryParse(value, true, out SpecialTileOptions parsed))
            {
                return parsed;
            }

            return SpecialTileOptions.None;
        }

        public static void MapSpecialTileBooleans(
            SpecialTileOptions options,
            out bool enableDice,
            out bool enableItem,
            out bool enableMessage)
        {
            enableDice = options.HasFlag(SpecialTileOptions.Dice);
            enableItem = options.HasFlag(SpecialTileOptions.Item);
            enableMessage = options.HasFlag(SpecialTileOptions.Message);
        }
    }
}
