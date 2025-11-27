using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.InventoryService;
using SnakeAndLaddersFinalProject.Mappers;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.Game.Inventory;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class InventoryViewModel : INotifyPropertyChanged
    {
        private const string INVENTORY_SERVICE_ENDPOINT_NAME = "NetTcpBinding_IInventoryService";
        private const int MIN_VALID_USER_ID = 1;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(InventoryViewModel));

        private InventoryItemViewModel slot1Item;
        private InventoryItemViewModel slot2Item;
        private InventoryItemViewModel slot3Item;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<InventoryItemViewModel> Items { get; }
        public ObservableCollection<InventoryDiceViewModel> Dice { get; }

        public InventoryItemViewModel Slot1Item
        {
            get => slot1Item;
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
            get => slot2Item;
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
            get => slot3Item;
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

        public ICommand RefreshCommand { get; }
        public ICommand SetItemSlotCommand { get; }
        public ICommand SetDiceSlotCommand { get; }

        private InventoryDiceViewModel slot1Dice;
        private InventoryDiceViewModel slot2Dice;

        public InventoryDiceViewModel Slot1Dice
        {
            get => slot1Dice;
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
            get => slot2Dice;
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

        public InventoryViewModel()
        {
            Items = new ObservableCollection<InventoryItemViewModel>();
            Dice = new ObservableCollection<InventoryDiceViewModel>();

            RefreshCommand = new RelayCommand(OnRefreshExecuted, CanAlwaysExecute);
            SetItemSlotCommand = new RelayCommand(OnSetItemSlotExecuted, CanExecuteSetItemSlot);
            SetDiceSlotCommand = new RelayCommand(OnSetDiceSlotExecuted, CanExecuteSetDiceSlot);
        }

        public Task InitializeAsync()
        {
            return LoadInventoryAsync();
        }

        private bool CanAlwaysExecute(object parameter)
        {
            return true;
        }

        private bool CanExecuteSetItemSlot(object parameter)
        {
            return parameter is InventoryItemSlotSelection;
        }

        private bool CanExecuteSetDiceSlot(object parameter)
        {
            return parameter is InventoryDiceSlotSelection;
        }

        private async void OnRefreshExecuted(object parameter)
        {
            await LoadInventoryAsync();
        }

        private async void OnSetItemSlotExecuted(object parameter)
        {
            await SetItemSlotAsync(parameter);
        }

        private async void OnSetDiceSlotExecuted(object parameter)
        {
            await SetDiceSlotAsync(parameter);
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
                using (var client = CreateClient())
                {
                    var snapshot = await Task.Run(() => client.GetInventory(userId));

                    Items.Clear();
                    Dice.Clear();

                    if (snapshot != null && snapshot.Items != null)
                    {
                        foreach (var dto in snapshot.Items)
                        {
                            Items.Add(new InventoryItemViewModel
                            {
                                ObjectId = dto.ObjectId,
                                ObjectCode = dto.ObjectCode,
                                Name = dto.Name,
                                Quantity = dto.Quantity,
                                SlotNumber = dto.SlotNumber,
                                IconPath = InventoryIconMapper.GetItemIconPath(dto.ObjectCode)
                            });
                        }

                        RefreshSlotItems();
                    }

                    if (snapshot != null && snapshot.Dice != null)
                    {
                        foreach (var dto in snapshot.Dice)
                        {
                            Dice.Add(new InventoryDiceViewModel
                            {
                                DiceId = dto.DiceId,
                                DiceCode = dto.DiceCode,
                                Name = dto.Name,
                                Quantity = dto.Quantity,
                                SlotNumber = dto.SlotNumber,
                                IconPath = InventoryIconMapper.GetDiceIconPath(dto.DiceCode)
                            });
                        }

                        RefreshDiceSlots();
                    }

                }
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


        private async Task SetItemSlotAsync(object parameter)
        {
            var selection = parameter as InventoryItemSlotSelection;

            if (selection == null)
            {
                return;
            }

            int userId = SessionContext.Current.UserId;

            if (!IsValidUserId(userId))
            {
                return;
            }

            if (selection.SlotNumber < 1 || selection.SlotNumber > 3)
            {
                return;
            }

            try
            {
                byte slot = selection.SlotNumber;

                foreach (var item in Items)
                {
                    if (item.SlotNumber == slot && !ReferenceEquals(item, selection.Item))
                    {
                        item.SlotNumber = null;
                    }
                }

                if (selection.Item != null)
                {
                    selection.Item.SlotNumber = slot;
                }

                int? slot1ObjectId = Items.FirstOrDefault(i => i.SlotNumber == 1)?.ObjectId;
                int? slot2ObjectId = Items.FirstOrDefault(i => i.SlotNumber == 2)?.ObjectId;
                int? slot3ObjectId = Items.FirstOrDefault(i => i.SlotNumber == 3)?.ObjectId;

                using (var client = CreateClient())
                {
                    await Task.Run(() => client.UpdateSelectedItems(
                        userId,
                        slot1ObjectId,
                        slot2ObjectId,
                        slot3ObjectId));
                }

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

        private async Task SetDiceSlotAsync(object parameter)
        {
            var selection = parameter as InventoryDiceSlotSelection;

            if (selection == null)
            {
                return;
            }

            int userId = SessionContext.Current.UserId;

            if (!IsValidUserId(userId))
            {
                return;
            }

            if (selection.SlotNumber < 1 || selection.SlotNumber > 2)
            {
                return;
            }

            try
            {
                byte slot = selection.SlotNumber;

                foreach (var dice in Dice)
                {
                    if (dice.SlotNumber == slot && !ReferenceEquals(dice, selection.Dice))
                    {
                        dice.SlotNumber = null;
                    }
                }

                if (selection.Dice != null)
                {
                    selection.Dice.SlotNumber = slot;
                }

                int? slot1DiceId = Dice.FirstOrDefault(d => d.SlotNumber == 1)?.DiceId;
                int? slot2DiceId = Dice.FirstOrDefault(d => d.SlotNumber == 2)?.DiceId;

                using (var client = CreateClient())
                {
                    await Task.Run(() => client.UpdateSelectedDice(
                        userId,
                        slot1DiceId,
                        slot2DiceId));
                }

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

        private static InventoryServiceClient CreateClient()
        {
            return new InventoryServiceClient(INVENTORY_SERVICE_ENDPOINT_NAME);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    

    
}
