using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.Game.Inventory;


namespace SnakeAndLaddersFinalProject.Pages
{
    /// <summary>
    /// Lógica de interacción para InventoryPage.xaml
    /// </summary>
    public partial class InventoryPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InventoryPage));

        private readonly InventoryViewModel viewModel;

        public InventoryPage()
        {
            InitializeComponent();

            viewModel = new InventoryViewModel();
            DataContext = viewModel;

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Loaded -= OnLoaded;
                await viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Error al inicializar InventoryPage.", ex);
            }
        }

        private void OnItemSlotClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            var selectedItem = ItemsListView.SelectedItem as InventoryItemViewModel;
            if (selectedItem == null)
            {
                return;
            }

            byte slotNumber;
            if (!byte.TryParse(button.Tag as string, out slotNumber))
            {
                return;
            }

            var selection = new InventoryItemSlotSelection
            {
                SlotNumber = slotNumber,
                Item = selectedItem
            };

            if (viewModel.SetItemSlotCommand != null &&
                viewModel.SetItemSlotCommand.CanExecute(selection))
            {
                viewModel.SetItemSlotCommand.Execute(selection);
            }
        }

        private void OnDiceSlotClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            var selectedDice = DiceListView.SelectedItem as InventoryDiceViewModel;
            if (selectedDice == null)
            {
                return;
            }

            byte slotNumber;
            if (!byte.TryParse(button.Tag as string, out slotNumber))
            {
                return;
            }

            var selection = new InventoryDiceSlotSelection
            {
                SlotNumber = slotNumber,
                Dice = selectedDice
            };

            if (viewModel.SetDiceSlotCommand != null &&
                viewModel.SetDiceSlotCommand.CanExecute(selection))
            {
                viewModel.SetDiceSlotCommand.Execute(selection);
            }
        }


        private void OnMainMenuClick(object sender, RoutedEventArgs e)
        {
            
            NavigationService?.GoBack();
            
        }
    }
}
