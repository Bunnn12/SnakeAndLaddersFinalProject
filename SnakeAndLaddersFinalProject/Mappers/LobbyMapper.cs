using System;

namespace SnakeAndLaddersFinalProject.Mappers
{
    internal static class LobbyMapper
    {
        private const string DIFFICULTY_EASY = "Easy";
        private const string DIFFICULTY_NORMAL = "Normal";
        private const string DIFFICULTY_HARD = "Hard";

        private const int BOARD_SIZE_SMALL = 8;
        private const int BOARD_SIZE_MEDIUM = 10;
        private const int BOARD_SIZE_LARGE = 12;

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
                case BOARD_SIZE_SMALL:
                    return BoardSizeOption.EightByEight;
                case BOARD_SIZE_LARGE:
                    return BoardSizeOption.TwelveByTwelve;
                case BOARD_SIZE_MEDIUM:
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
