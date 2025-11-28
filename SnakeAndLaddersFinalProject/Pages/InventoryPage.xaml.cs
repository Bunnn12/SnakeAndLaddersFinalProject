using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.ViewModels;

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

        private void OnMainMenuClick(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}
