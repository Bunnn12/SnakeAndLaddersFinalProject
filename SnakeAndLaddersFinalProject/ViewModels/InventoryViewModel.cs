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
using SnakeAndLaddersFinalProject.Infrastructure;
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

        private static readonly ILog Logger = LogManager.GetLogger(typeof(InventoryViewModel));

        private readonly IInventoryManager inventoryManager;

        private InventoryItemViewModel slot1Item;
        private InventoryItemViewModel slot2Item;
        private InventoryItemViewModel slot3Item;

        private InventoryDiceViewModel slot1Dice;
        private InventoryDiceViewModel slot2Dice;

        private InventoryItemViewModel selectedItem;
        private InventoryDiceViewModel selectedDice;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<InventoryItemViewModel> Items { get; }

        public ObservableCollection<InventoryDiceViewModel> Dice { get; }

        public InventoryItemViewModel Slot1Item
        {
            get { return slot1Item; }
            private set
            {
                if (slot1Item == value)
                {
                    return;
                }

                slot1Item = value;
                OnPropertyChanged();
            }
        }

        public InventoryItemViewModel Slot2Item
        {
            get { return slot2Item; }
            private set
            {
                if (slot2Item == value)
                {
                    return;
                }

                slot2Item = value;
                OnPropertyChanged();
            }
        }

        public InventoryItemViewModel Slot3Item
        {
            get { return slot3Item; }
            private set
            {
                if (slot3Item == value)
                {
                    return;
                }

                slot3Item = value;
                OnPropertyChanged();
            }
        }

        public InventoryDiceViewModel Slot1Dice
        {
            get { return slot1Dice; }
            private set
            {
                if (slot1Dice == value)
                {
                    return;
                }

                slot1Dice = value;
                OnPropertyChanged();
            }
        }

        public InventoryDiceViewModel Slot2Dice
        {
            get { return slot2Dice; }
            private set
            {
                if (slot2Dice == value)
                {
                    return;
                }

                slot2Dice = value;
                OnPropertyChanged();
            }
        }

        public InventoryItemViewModel SelectedItem
        {
            get { return selectedItem; }
            set
            {
                if (selectedItem == value)
                {
                    return;
                }

                selectedItem = value;
                OnPropertyChanged();
            }
        }

        public InventoryDiceViewModel SelectedDice
        {
            get { return selectedDice; }
            set
            {
                if (selectedDice == value)
                {
                    return;
                }

                selectedDice = value;
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
            this.inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));

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

        private async void OnSetItemSlot1Executed()
        {
            await SetItemSlotAsync(MIN_ITEM_SLOT);
        }

        private async void OnSetItemSlot2Executed()
        {
            await SetItemSlotAsync(2);
        }

        private async void OnSetItemSlot3Executed()
        {
            await SetItemSlotAsync(MAX_ITEM_SLOT);
        }

        private async void OnClearItemSlotExecuted(byte slotNumber)
        {
            await ClearItemSlotAsync(slotNumber);
        }

        private async void OnSetDiceSlotExecuted(byte slotNumber)
        {
            await SetDiceSlotAsync(slotNumber);
        }

        private async void OnSetDiceSlot1Executed()
        {
            await SetDiceSlotAsync(MIN_DICE_SLOT);
        }

        private async void OnSetDiceSlot2Executed()
        {
            await SetDiceSlotAsync(MAX_DICE_SLOT);
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
                InventorySnapshot snapshot = await inventoryManager.GetInventoryAsync(userId);

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
                    Logger);
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
            if (selectedItem == null)
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
                await inventoryManager.EquipItemToSlotAsync(
                    userId,
                    slotNumber,
                    selectedItem.ObjectId);

                await LoadInventoryAsync();
            }
            catch (Exception ex)
            {
                _ = ExceptionHandler.Handle(
                    ex,
                    "InventoryViewModel.SetItemSlotAsync",
                    Logger);
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
                await inventoryManager.UnequipItemFromSlotAsync(
                    userId,
                    slotNumber);

                await LoadInventoryAsync();
            }
            catch (Exception ex)
            {
                _ = ExceptionHandler.Handle(
                    ex,
                    "InventoryViewModel.ClearItemSlotAsync",
                    Logger);
            }
        }

        private async Task SetDiceSlotAsync(byte slotNumber)
        {
            if (selectedDice == null)
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
                await inventoryManager.EquipDiceToSlotAsync(
                    userId,
                    slotNumber,
                    selectedDice.DiceId);

                await LoadInventoryAsync();
            }
            catch (Exception ex)
            {
                _ = ExceptionHandler.Handle(
                    ex,
                    "InventoryViewModel.SetDiceSlotAsync",
                    Logger);
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
                await inventoryManager.UnequipDiceFromSlotAsync(
                    userId,
                    slotNumber);

                await LoadInventoryAsync();
            }
            catch (Exception ex)
            {
                _ = ExceptionHandler.Handle(
                    ex,
                    "InventoryViewModel.ClearDiceSlotAsync",
                    Logger);
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
