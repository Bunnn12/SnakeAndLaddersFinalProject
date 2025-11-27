using System;

namespace SnakeAndLaddersFinalProject.Mappers
{
    internal static class InventoryIconMapper
    {
        // ITEMS
        private const string BASE_PATH = "pack://application:,,,/Assets/Images/Icons/";

        private const string ICON_ANCHOR = "AnchorIcon.png";
        private const string ICON_ROCKET = "RocketIcon.png";
        private const string ICON_SWAP = "SwapIcon.png";
        private const string ICON_FREEZE = "FreezeIcon.png";
        private const string ICON_SHIELD = "ShieldIcon.png";
        private const string ICON_ITEM_FALLBACK = "DefaultItemIcon.png";


        private const string ICON_DICE_BASE = "DiceBaseIcon.png";
        private const string ICON_DICE_NEG = "DiceNegIcon.png";
        private const string ICON_DICE_123 = "Dice123Icon.png";
        private const string ICON_DICE_456 = "Dice456Icon.png";
        private const string ICON_DICE_FALLBACK = "DefaultDiceIcon.png";

        public static string GetItemIconPath(string objectCode)
        {
            if (string.IsNullOrWhiteSpace(objectCode))
            {
                return BuildItemPath(ICON_ITEM_FALLBACK);
            }

            switch (objectCode)
            {
                case "IT_ANCHOR":
                    return BuildItemPath(ICON_ANCHOR);
                case "IT_ROCKET":
                    return BuildItemPath(ICON_ROCKET);
                case "IT_SWAP":
                    return BuildItemPath(ICON_SWAP);
                case "IT_FREEZE":
                    return BuildItemPath(ICON_FREEZE);
                case "IT_SHIELD":
                    return BuildItemPath(ICON_SHIELD);
                default:
                    return BuildItemPath(ICON_ITEM_FALLBACK);
            }
        }

        public static string GetDiceIconPath(string diceCode)
        {
            if (string.IsNullOrWhiteSpace(diceCode))
            {
                return BuildDicePath(ICON_DICE_FALLBACK);
            }

            switch (diceCode)
            {
                case "DICE_BASE":
                    return BuildDicePath(ICON_DICE_BASE);
                case "DICE_NEG":
                    return BuildDicePath(ICON_DICE_NEG);
                case "DICE_123":
                    return BuildDicePath(ICON_DICE_123);
                case "DICE_456":
                    return BuildDicePath(ICON_DICE_456);
                default:
                    return BuildDicePath(ICON_DICE_FALLBACK);
            }
        }

        private static string BuildItemPath(string fileName)
        {
            return BASE_PATH + fileName;
        }

        private static string BuildDicePath(string fileName)
        {
            return BASE_PATH + fileName;
        }
    }
}
