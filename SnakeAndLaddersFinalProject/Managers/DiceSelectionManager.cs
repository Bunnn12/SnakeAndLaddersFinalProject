using SnakeAndLaddersFinalProject.Utilities;
using System;

namespace SnakeAndLaddersFinalProject.Managers
{
    public sealed class DiceSelectionManager
    {
        private readonly byte minSlot;
        private readonly byte maxSlot;
        private readonly Func<byte, bool> hasDiceInSlotFunc;
        private readonly Action<byte?> onSelectedSlotChanged;
        private readonly Action<bool> onSlot1SelectedChanged;
        private readonly Action<bool> onSlot2SelectedChanged;
        private readonly Action<string> onNotificationChanged;

        private const string SLOT_SELECTED_MESSAGE_FORMAT =
            "Dado del slot {0} seleccionado para el siguiente tiro.";

        public DiceSelectionManager(
            byte minSlot,
            byte maxSlot,
            Func<byte, bool> hasDiceInSlotFunc,
            Action<byte?> onSelectedSlotChanged,
            Action<bool> onSlot1SelectedChanged,
            Action<bool> onSlot2SelectedChanged,
            Action<string> onNotificationChanged)
        {
            if (minSlot <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minSlot));
            }

            if (maxSlot < minSlot)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSlot));
            }

            this.minSlot = minSlot;
            this.maxSlot = maxSlot;
            this.hasDiceInSlotFunc = hasDiceInSlotFunc ?? throw new ArgumentNullException(nameof(hasDiceInSlotFunc));
            this.onSelectedSlotChanged = onSelectedSlotChanged ?? throw new ArgumentNullException(nameof(onSelectedSlotChanged));
            this.onSlot1SelectedChanged = onSlot1SelectedChanged ?? throw new ArgumentNullException(nameof(onSlot1SelectedChanged));
            this.onSlot2SelectedChanged = onSlot2SelectedChanged ?? throw new ArgumentNullException(nameof(onSlot2SelectedChanged));
            this.onNotificationChanged = onNotificationChanged ?? throw new ArgumentNullException(nameof(onNotificationChanged));
        }

        public byte? SelectedSlot { get; private set; }

        public void ResetSelection()
        {
            SelectedSlot = null;
            onSelectedSlotChanged(null);
            onSlot1SelectedChanged(false);
            onSlot2SelectedChanged(false);
            onNotificationChanged(string.Empty);
        }

        public bool CanSelectSlot(
            byte slotNumber,
            bool isMyTurn,
            bool isAnimating,
            bool isRollRequestInProgress,
            bool isUseItemInProgress,
            bool isTargetSelectionActive)
        {
            bool hasDiceInSlot = hasDiceInSlotFunc(slotNumber);

            return PlayerActionGuard.CanSelectDiceSlot(
                isMyTurn,
                isAnimating,
                isRollRequestInProgress,
                isUseItemInProgress,
                isTargetSelectionActive,
                hasDiceInSlot);
        }

        public void SelectSlot(byte slotNumber)
        {
            if (!hasDiceInSlotFunc(slotNumber))
            {
                return;
            }

            SelectedSlot = slotNumber;
            onSelectedSlotChanged(SelectedSlot);

            bool isSlot1Selected = slotNumber == minSlot;
            bool isSlot2Selected = slotNumber == maxSlot;

            onSlot1SelectedChanged(isSlot1Selected);
            onSlot2SelectedChanged(isSlot2Selected);

            onNotificationChanged(
                string.Format(
                    SLOT_SELECTED_MESSAGE_FORMAT,
                    slotNumber));
        }
    }
}
