using System;
using System.Windows;
using System.Windows.Controls;
using log4net;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class InventoryPage : Page
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(InventoryPage));

        private readonly InventoryViewModel _viewModel;

        public InventoryPage()
        {
            InitializeComponent();

            _viewModel = new InventoryViewModel();
            DataContext = _viewModel;

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Loaded -= OnLoaded;
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                _logger.Error("Error al inicializar InventoryPage.", ex);
            }
        }

        private void OnMainMenuClick(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}
