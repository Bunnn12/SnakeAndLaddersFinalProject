namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class PlayerActionGuard
    {
        public static bool CanRollDice(
            bool isMyTurn,
            bool isAnimating,
            bool isRollRequestInProgress,
            bool isUseItemInProgress,
            bool isTargetSelectionActive)
        {
            if (!isMyTurn)
            {
                return false;
            }

            if (isAnimating)
            {
                return false;
            }

            if (isRollRequestInProgress)
            {
                return false;
            }

            if (isUseItemInProgress)
            {
                return false;
            }

            if (isTargetSelectionActive)
            {
                return false;
            }

            return true;
        }

        public static bool CanUseItem(
            bool isMyTurn,
            bool isAnimating,
            bool isRollRequestInProgress,
            bool isUseItemInProgress,
            bool isTargetSelectionActive)
        {
            // De momento comparte las mismas reglas que tirar dado
            return CanRollDice(
                isMyTurn,
                isAnimating,
                isRollRequestInProgress,
                isUseItemInProgress,
                isTargetSelectionActive);
        }

        public static bool CanSelectDiceSlot(
            bool isMyTurn,
            bool isAnimating,
            bool isRollRequestInProgress,
            bool isUseItemInProgress,
            bool isTargetSelectionActive,
            bool hasDiceInSlot)
        {
            if (!CanRollDice(
                    isMyTurn,
                    isAnimating,
                    isRollRequestInProgress,
                    isUseItemInProgress,
                    isTargetSelectionActive))
            {
                return false;
            }

            if (!hasDiceInSlot)
            {
                return false;
            }

            return true;
        }
    }
}
