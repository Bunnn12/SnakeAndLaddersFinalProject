using System;
using SnakeAndLaddersFinalProject.Globalization;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.Managers
{
    public sealed class DiceSelectionManager
    {
        private const string SLOT_SELECTED_MESSAGE_KEY = "GameDiceSlotSelectedFmt";

        private readonly byte _minSlot;
        private readonly byte _maxSlot;
        private readonly Func<byte, bool> _hasDiceInSlotFunc;
        private readonly Action<byte?> _onSelectedSlotChanged;
        private readonly Action<bool> _onSlot1SelectedChanged;
        private readonly Action<bool> _onSlot2SelectedChanged;
        private readonly Action<string> _onNotificationChanged;

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

            _minSlot = minSlot;
            _maxSlot = maxSlot;
            _hasDiceInSlotFunc = hasDiceInSlotFunc ?? throw new ArgumentNullException(nameof(hasDiceInSlotFunc));
            _onSelectedSlotChanged = onSelectedSlotChanged ?? throw new ArgumentNullException(nameof(onSelectedSlotChanged));
            _onSlot1SelectedChanged = onSlot1SelectedChanged ?? throw new ArgumentNullException(nameof(onSlot1SelectedChanged));
            _onSlot2SelectedChanged = onSlot2SelectedChanged ?? throw new ArgumentNullException(nameof(onSlot2SelectedChanged));
            _onNotificationChanged = onNotificationChanged ?? throw new ArgumentNullException(nameof(onNotificationChanged));
        }

        public byte? SelectedSlot { get; private set; }

        public void ResetSelection()
        {
            SelectedSlot = null;
            _onSelectedSlotChanged(null);
            _onSlot1SelectedChanged(false);
            _onSlot2SelectedChanged(false);
            _onNotificationChanged(string.Empty);
        }

        public bool CanSelectSlot(
            byte slotNumber,
            bool isMyTurn,
            bool isAnimating,
            bool isRollRequestInProgress,
            bool isUseItemInProgress,
            bool isTargetSelectionActive)
        {
            bool hasDiceInSlot = _hasDiceInSlotFunc(slotNumber);

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
            if (!_hasDiceInSlotFunc(slotNumber))
            {
                return;
            }

            SelectedSlot = slotNumber;
            _onSelectedSlotChanged(SelectedSlot);

            bool isSlot1Selected = slotNumber == _minSlot;
            bool isSlot2Selected = slotNumber == _maxSlot;

            _onSlot1SelectedChanged(isSlot1Selected);
            _onSlot2SelectedChanged(isSlot2Selected);

            _onNotificationChanged(
                string.Format(
                    T(SLOT_SELECTED_MESSAGE_KEY),
                    slotNumber));
        }

        private static string T(string key)
        {
            return LocalizationManager.Current[key];
        }
    }
}
