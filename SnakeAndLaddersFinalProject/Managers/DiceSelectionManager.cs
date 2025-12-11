using System;
using SnakeAndLaddersFinalProject.Globalization;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.Managers
{
    public sealed class DiceSelectionManager
    {
        private const string SLOT_SELECTED_MESSAGE_KEY = "GameDiceSlotSelectedFmt";
        private const byte MIN_VALID_SLOT_NUMBER = 1;

        private readonly byte _minSlot;
        private readonly byte _maxSlot;
        private readonly Func<byte, bool> _hasDiceInSlotFunc;
        private readonly DiceSelectionCallbacks _callbacks;

        public DiceSelectionManager(
            byte minSlot,
            byte maxSlot,
            Func<byte, bool> hasDiceInSlotFunc,
            DiceSelectionCallbacks callbacks)
        {
            if (minSlot < MIN_VALID_SLOT_NUMBER)
            {
                throw new ArgumentOutOfRangeException(nameof(minSlot));
            }

            if (maxSlot < minSlot)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSlot));
            }

            _hasDiceInSlotFunc = hasDiceInSlotFunc
                ?? throw new ArgumentNullException(nameof(hasDiceInSlotFunc));

            _callbacks = callbacks
                ?? throw new ArgumentNullException(nameof(callbacks));

            _minSlot = minSlot;
            _maxSlot = maxSlot;
        }

        public byte? SelectedSlot { get; private set; }

        public void ResetSelection()
        {
            SelectedSlot = null;

            _callbacks.OnSelectedSlotChanged(null);
            _callbacks.OnSlot1SelectedChanged(false);
            _callbacks.OnSlot2SelectedChanged(false);
            _callbacks.OnNotificationChanged(string.Empty);
        }

        public bool CanSelectSlot(byte slotNumber, DiceSelectionState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            bool hasDiceInSlot = _hasDiceInSlotFunc(slotNumber);

            return PlayerActionGuard.CanSelectDiceSlot(
                state.IsMyTurn,
                state.IsAnimating,
                state.IsRollRequestInProgress,
                state.IsUseItemInProgress,
                state.IsTargetSelectionActive,
                hasDiceInSlot);
        }

        public void SelectSlot(byte slotNumber)
        {
            if (!_hasDiceInSlotFunc(slotNumber))
            {
                return;
            }

            SelectedSlot = slotNumber;

            _callbacks.OnSelectedSlotChanged(SelectedSlot);

            bool isSlot1Selected = slotNumber == _minSlot;
            bool isSlot2Selected = slotNumber == _maxSlot;

            _callbacks.OnSlot1SelectedChanged(isSlot1Selected);
            _callbacks.OnSlot2SelectedChanged(isSlot2Selected);

            string notification = string.Format(
                T(SLOT_SELECTED_MESSAGE_KEY),
                slotNumber);

            _callbacks.OnNotificationChanged(notification);
        }

        private static string T(string key)
        {
            return LocalizationManager.Current[key];
        }
    }

    public sealed class DiceSelectionCallbacks
    {
        public DiceSelectionCallbacks(
            Action<byte?> onSelectedSlotChanged,
            Action<bool> onSlot1SelectedChanged,
            Action<bool> onSlot2SelectedChanged,
            Action<string> onNotificationChanged)
        {
            OnSelectedSlotChanged = onSelectedSlotChanged
                ?? throw new ArgumentNullException(nameof(onSelectedSlotChanged));

            OnSlot1SelectedChanged = onSlot1SelectedChanged
                ?? throw new ArgumentNullException(nameof(onSlot1SelectedChanged));

            OnSlot2SelectedChanged = onSlot2SelectedChanged
                ?? throw new ArgumentNullException(nameof(onSlot2SelectedChanged));

            OnNotificationChanged = onNotificationChanged
                ?? throw new ArgumentNullException(nameof(onNotificationChanged));
        }

        public Action<byte?> OnSelectedSlotChanged { get; }
        public Action<bool> OnSlot1SelectedChanged { get; }
        public Action<bool> OnSlot2SelectedChanged { get; }
        public Action<string> OnNotificationChanged { get; }
    }

    public sealed class DiceSelectionState
    {
        public DiceSelectionState(
            bool isMyTurn,
            bool isAnimating,
            bool isRollRequestInProgress,
            bool isUseItemInProgress,
            bool isTargetSelectionActive)
        {
            IsMyTurn = isMyTurn;
            IsAnimating = isAnimating;
            IsRollRequestInProgress = isRollRequestInProgress;
            IsUseItemInProgress = isUseItemInProgress;
            IsTargetSelectionActive = isTargetSelectionActive;
        }

        public bool IsMyTurn { get; }
        public bool IsAnimating { get; }
        public bool IsRollRequestInProgress { get; }
        public bool IsUseItemInProgress { get; }
        public bool IsTargetSelectionActive { get; }
    }
}
