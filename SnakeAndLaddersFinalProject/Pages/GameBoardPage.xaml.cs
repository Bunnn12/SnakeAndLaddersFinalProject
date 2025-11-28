using System;
using System.Windows;
using System.Windows.Controls;
using log4net;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class GameBoardPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardPage));

        public GameBoardPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public GameBoardViewModel ViewModel
        {
            get { return DataContext as GameBoardViewModel; }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            GameBoardViewModel viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            try
            {
                if (viewModel.Inventory != null)
                {
                    await viewModel.Inventory.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error al inicializar el inventario en GameBoardPage.", ex);
            }
        }
    }
}
