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

        // Un comando por slot de item
        public ICommand SetItemSlot1Command { get; }
        public ICommand SetItemSlot2Command { get; }
        public ICommand SetItemSlot3Command { get; }

        // Un comando por slot de dado
        public ICommand SetDiceSlot1Command { get; }
        public ICommand SetDiceSlot2Command { get; }

        public InventoryViewModel()
            : this(new InventoryManager())
        {
        }

        public InventoryViewModel(IInventoryManager inventoryManager)
        {
            this.inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));

            Items = new ObservableCollection<InventoryItemViewModel>();
            Dice = new ObservableCollection<InventoryDiceViewModel>();

            // Tu RelayCommand (versión actual) espera Action<object>
            RefreshCommand = new RelayCommand(_ => OnRefreshExecuted());

            SetItemSlot1Command = new RelayCommand(_ => OnSetItemSlot1Executed());
            SetItemSlot2Command = new RelayCommand(_ => OnSetItemSlot2Executed());
            SetItemSlot3Command = new RelayCommand(_ => OnSetItemSlot3Executed());

            SetDiceSlot1Command = new RelayCommand(_ => OnSetDiceSlot1Executed());
            SetDiceSlot2Command = new RelayCommand(_ => OnSetDiceSlot2Executed());
        }

        public Task InitializeAsync()
        {
            return LoadInventoryAsync();
        }

        private async void OnRefreshExecuted()
        {
            await LoadInventoryAsync();
        }

        private async void OnSetItemSlot1Executed()
        {
            await SetItemSlotAsync(1);
        }

        private async void OnSetItemSlot2Executed()
        {
            await SetItemSlotAsync(2);
        }

        private async void OnSetItemSlot3Executed()
        {
            await SetItemSlotAsync(3);
        }

        private async void OnSetDiceSlot1Executed()
        {
            await SetDiceSlotAsync(1);
        }

        private async void OnSetDiceSlot2Executed()
        {
            await SetDiceSlotAsync(2);
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
            Slot1Item = Items.FirstOrDefault(i => i.SlotNumber == 1);
            Slot2Item = Items.FirstOrDefault(i => i.SlotNumber == 2);
            Slot3Item = Items.FirstOrDefault(i => i.SlotNumber == 3);
        }

        private void RefreshDiceSlots()
        {
            Slot1Dice = Dice.FirstOrDefault(d => d.SlotNumber == 1);
            Slot2Dice = Dice.FirstOrDefault(d => d.SlotNumber == 2);
        }

        private async Task SetItemSlotAsync(int slotNumber)
        {
            // Si no hay item seleccionado, no hace nada
            if (selectedItem == null)
            {
                return;
            }

            int userId = SessionContext.Current.UserId;

            if (!IsValidUserId(userId))
            {
                return;
            }

            if (slotNumber < 1 || slotNumber > 3)
            {
                return;
            }

            try
            {
                byte slot = (byte)slotNumber;

                foreach (InventoryItemViewModel item in Items)
                {
                    if (item.SlotNumber == slot && !ReferenceEquals(item, selectedItem))
                    {
                        item.SlotNumber = null;
                    }
                }

                selectedItem.SlotNumber = slot;

                int? slot1ObjectId = Items.FirstOrDefault(i => i.SlotNumber == 1)?.ObjectId;
                int? slot2ObjectId = Items.FirstOrDefault(i => i.SlotNumber == 2)?.ObjectId;
                int? slot3ObjectId = Items.FirstOrDefault(i => i.SlotNumber == 3)?.ObjectId;

                await inventoryManager.UpdateSelectedItemsAsync(
                    userId,
                    slot1ObjectId,
                    slot2ObjectId,
                    slot3ObjectId);

                RefreshSlotItems();
            }
            catch (Exception ex)
            {
                _ = ExceptionHandler.Handle(
                    ex,
                    "InventoryViewModel.SetItemSlotAsync",
                    Logger);
            }
        }

        private async Task SetDiceSlotAsync(int slotNumber)
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

            if (slotNumber < 1 || slotNumber > 2)
            {
                return;
            }

            try
            {
                byte slot = (byte)slotNumber;

                foreach (InventoryDiceViewModel diceItem in Dice)
                {
                    if (diceItem.SlotNumber == slot && !ReferenceEquals(diceItem, selectedDice))
                    {
                        diceItem.SlotNumber = null;
                    }
                }

                selectedDice.SlotNumber = slot;

                int? slot1DiceId = Dice.FirstOrDefault(d => d.SlotNumber == 1)?.DiceId;
                int? slot2DiceId = Dice.FirstOrDefault(d => d.SlotNumber == 2)?.DiceId;

                await inventoryManager.UpdateSelectedDiceAsync(
                    userId,
                    slot1DiceId,
                    slot2DiceId);

                RefreshDiceSlots();
            }
            catch (Exception ex)
            {
                _ = ExceptionHandler.Handle(
                    ex,
                    "InventoryViewModel.SetDiceSlotAsync",
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
