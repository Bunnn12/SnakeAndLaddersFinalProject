using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Game.Inventory;
using SnakeAndLaddersFinalProject.Mappers;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class InventoryViewModel : INotifyPropertyChanged
    {
        private const int MIN_VALID_USER_ID = 1;

        private const byte MIN_ITEM_SLOT = 1;
        private const byte MAX_ITEM_SLOT = 3;

        private const byte MIN_DICE_SLOT = 1;
        private const byte MAX_DICE_SLOT = 2;

        private static readonly ILog _logger = LogManager.GetLogger(typeof(InventoryViewModel));

        private readonly IInventoryManager _inventoryManager;

        private InventoryItemViewModel _slot1Item;
        private InventoryItemViewModel _slot2Item;
        private InventoryItemViewModel _slot3Item;

        private InventoryDiceViewModel _slot1Dice;
        private InventoryDiceViewModel _slot2Dice;

        private InventoryItemViewModel _selectedItem;
        private InventoryDiceViewModel _selectedDice;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<InventoryItemViewModel> Items { get; }

        public ObservableCollection<InventoryDiceViewModel> Dice { get; }

        public InventoryItemViewModel Slot1Item
        {
            get { return _slot1Item; }
            private set
            {
                if (_slot1Item == value)
                {
                    return;
                }

                _slot1Item = value;
                OnPropertyChanged();
            }
        }

        public InventoryItemViewModel Slot2Item
        {
            get { return _slot2Item; }
            private set
            {
                if (_slot2Item == value)
                {
                    return;
                }

                _slot2Item = value;
                OnPropertyChanged();
            }
        }

        public InventoryItemViewModel Slot3Item
        {
            get { return _slot3Item; }
            private set
            {
                if (_slot3Item == value)
                {
                    return;
                }

                _slot3Item = value;
                OnPropertyChanged();
            }
        }

        public InventoryDiceViewModel Slot1Dice
        {
            get { return _slot1Dice; }
            private set
            {
                if (_slot1Dice == value)
                {
                    return;
                }

                _slot1Dice = value;
                OnPropertyChanged();
            }
        }

        public InventoryDiceViewModel Slot2Dice
        {
            get { return _slot2Dice; }
            private set
            {
                if (_slot2Dice == value)
                {
                    return;
                }

                _slot2Dice = value;
                OnPropertyChanged();
            }
        }

        public InventoryItemViewModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem == value)
                {
                    return;
                }

                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        public InventoryDiceViewModel SelectedDice
        {
            get { return _selectedDice; }
            set
            {
                if (_selectedDice == value)
                {
                    return;
                }

                _selectedDice = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; }

        public ICommand SetItemSlot1Command { get; }
        public ICommand SetItemSlot2Command { get; }
        public ICommand SetItemSlot3Command { get; }

        public ICommand ClearItemSlot1Command { get; }
        public ICommand ClearItemSlot2Command { get; }
        public ICommand ClearItemSlot3Command { get; }

        public ICommand SetDiceSlot1Command { get; }
        public ICommand SetDiceSlot2Command { get; }

        public ICommand ClearDiceSlot1Command { get; }
        public ICommand ClearDiceSlot2Command { get; }

        public InventoryViewModel()
            : this(new InventoryManager())
        {
        }

        public InventoryViewModel(IInventoryManager inventoryManager)
        {
            this._inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));

            Items = new ObservableCollection<InventoryItemViewModel>();
            Dice = new ObservableCollection<InventoryDiceViewModel>();

            RefreshCommand = new RelayCommand(_ => OnRefreshExecuted());

            SetItemSlot1Command = new RelayCommand(_ => OnSetItemSlotExecuted(MIN_ITEM_SLOT));
            SetItemSlot2Command = new RelayCommand(_ => OnSetItemSlotExecuted(2));
            SetItemSlot3Command = new RelayCommand(_ => OnSetItemSlotExecuted(MAX_ITEM_SLOT));

            ClearItemSlot1Command = new RelayCommand(_ => OnClearItemSlotExecuted(MIN_ITEM_SLOT));
            ClearItemSlot2Command = new RelayCommand(_ => OnClearItemSlotExecuted(2));
            ClearItemSlot3Command = new RelayCommand(_ => OnClearItemSlotExecuted(MAX_ITEM_SLOT));

            SetDiceSlot1Command = new RelayCommand(_ => OnSetDiceSlotExecuted(MIN_DICE_SLOT));
            SetDiceSlot2Command = new RelayCommand(_ => OnSetDiceSlotExecuted(MAX_DICE_SLOT));

            ClearDiceSlot1Command = new RelayCommand(_ => OnClearDiceSlotExecuted(MIN_DICE_SLOT));
            ClearDiceSlot2Command = new RelayCommand(_ => OnClearDiceSlotExecuted(MAX_DICE_SLOT));
        }

        public Task InitializeAsync()
        {
            return LoadInventoryAsync();
        }

        private async void OnRefreshExecuted()
        {
            await LoadInventoryAsync();
        }

        private async void OnSetItemSlotExecuted(byte slotNumber)
        {
            await SetItemSlotAsync(slotNumber);
        }

        private async void OnClearItemSlotExecuted(byte slotNumber)
        {
            await ClearItemSlotAsync(slotNumber);
        }

        private async void OnSetDiceSlotExecuted(byte slotNumber)
        {
            await SetDiceSlotAsync(slotNumber);
        }

        private async void OnClearDiceSlotExecuted(byte slotNumber)
        {
            await ClearDiceSlotAsync(slotNumber);
        }

        private async Task LoadInventoryAsync()
        {
            int userId = SessionContext.Current.UserId;

            if (!IsValidUserId(userId))
            {
                return;
            }

            try
            {
                InventorySnapshot snapshot = await _inventoryManager.GetInventoryAsync(userId);

                Items.Clear();
                Dice.Clear();

                foreach (InventoryItemData item in snapshot.Items)
                {
                    Items.Add(
                        new InventoryItemViewModel
                        {
                            ObjectId = item.ObjectId,
                            ObjectCode = item.ObjectCode,
                            Name = item.Name,
                            Quantity = item.Quantity,
                            SlotNumber = item.SlotNumber,
                            IconPath = InventoryIconMapper.GetItemIconPath(item.ObjectCode)
                        });
                }

                RefreshSlotItems();

                foreach (InventoryDiceData diceData in snapshot.Dice)
                {
                    Dice.Add(
                        new InventoryDiceViewModel
                        {
                            DiceId = diceData.DiceId,
                            DiceCode = diceData.DiceCode,
                            Name = diceData.Name,
                            Quantity = diceData.Quantity,
                            SlotNumber = diceData.SlotNumber,
                            IconPath = InventoryIconMapper.GetDiceIconPath(diceData.DiceCode)
                        });
                }

                RefreshDiceSlots();
            }
            catch (Exception ex)
            {
                _ = ExceptionHandler.Handle(
                    ex,
                    "InventoryViewModel.LoadInventoryAsync",
                    _logger);
            }
        }

        private void RefreshSlotItems()
        {
            Slot1Item = Items.FirstOrDefault(i => i.SlotNumber == MIN_ITEM_SLOT);
            Slot2Item = Items.FirstOrDefault(i => i.SlotNumber == 2);
            Slot3Item = Items.FirstOrDefault(i => i.SlotNumber == MAX_ITEM_SLOT);
        }

        private void RefreshDiceSlots()
        {
            Slot1Dice = Dice.FirstOrDefault(d => d.SlotNumber == MIN_DICE_SLOT);
            Slot2Dice = Dice.FirstOrDefault(d => d.SlotNumber == MAX_DICE_SLOT);
        }

        private async Task SetItemSlotAsync(byte slotNumber)
        {
            if (_selectedItem == null)
            {
                return;
            }

            int userId = SessionContext.Current.UserId;

            if (!IsValidUserId(userId))
            {
                return;
            }

            if (slotNumber < MIN_ITEM_SLOT || slotNumber > MAX_ITEM_SLOT)
            {
                return;
            }

            try
            {
                await _inventoryManager.EquipItemToSlotAsync(
                    userId,
                    slotNumber,
                    _selectedItem.ObjectId);

                await LoadInventoryAsync();
            }
            catch (Exception ex)
            {
                _ = ExceptionHandler.Handle(
                    ex,
                    "InventoryViewModel.SetItemSlotAsync",
                    _logger);
            }
        }

        private async Task ClearItemSlotAsync(byte slotNumber)
        {
            int userId = SessionContext.Current.UserId;

            if (!IsValidUserId(userId))
            {
                return;
            }

            if (slotNumber < MIN_ITEM_SLOT || slotNumber > MAX_ITEM_SLOT)
            {
                return;
            }

            try
            {
                await _inventoryManager.UnequipItemFromSlotAsync(
                    userId,
                    slotNumber);

                await LoadInventoryAsync();
            }
            catch (Exception ex)
            {
                _ = ExceptionHandler.Handle(
                    ex,
                    "InventoryViewModel.ClearItemSlotAsync",
                    _logger);
            }
        }

        private async Task SetDiceSlotAsync(byte slotNumber)
        {
            if (_selectedDice == null)
            {
                return;
            }

            int userId = SessionContext.Current.UserId;

            if (!IsValidUserId(userId))
            {
                return;
            }

            if (slotNumber < MIN_DICE_SLOT || slotNumber > MAX_DICE_SLOT)
            {
                return;
            }

            try
            {
                await _inventoryManager.EquipDiceToSlotAsync(
                    userId,
                    slotNumber,
                    _selectedDice.DiceId);

                await LoadInventoryAsync();
            }
            catch (Exception ex)
            {
                _ = ExceptionHandler.Handle(
                    ex,
                    "InventoryViewModel.SetDiceSlotAsync",
                    _logger);
            }
        }

        private async Task ClearDiceSlotAsync(byte slotNumber)
        {
            int userId = SessionContext.Current.UserId;

            if (!IsValidUserId(userId))
            {
                return;
            }

            if (slotNumber < MIN_DICE_SLOT || slotNumber > MAX_DICE_SLOT)
            {
                return;
            }

            try
            {
                await _inventoryManager.UnequipDiceFromSlotAsync(
                    userId,
                    slotNumber);

                await LoadInventoryAsync();
            }
            catch (Exception ex)
            {
                _ = ExceptionHandler.Handle(
                    ex,
                    "InventoryViewModel.ClearDiceSlotAsync",
                    _logger);
            }
        }

        private static bool IsValidUserId(int userId)
        {
            return userId >= MIN_VALID_USER_ID;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler == null)
            {
                return;
            }

            PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);
            handler(this, args);
        }
    }
}
