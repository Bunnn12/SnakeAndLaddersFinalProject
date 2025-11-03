using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.Navigation;
using SnakeAndLaddersFinalProject.Pages;


namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        // Ir a la pantalla de ajustes antes de crear
        private void CreateMatch(object sender, RoutedEventArgs e)
        {
            var page = new CreateMatchPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(page);
                return;
            }

            var window = Application.Current.MainWindow as NavigationWindow;
            if (window != null)
            {
                window.Navigate(page);
            }
            else
            {
                Application.Current.MainWindow.Content = page;
            }
        }

        private void NavigateToLobby(LobbyNavigationArgs args)
        {
            var page = new LobbyPage(args);

            if (NavigationService != null)
            {
                NavigationService.Navigate(page);
                return;
            }

            var window = Application.Current.MainWindow as NavigationWindow;
            if (window != null)
                window.Navigate(page);
            else
                Application.Current.MainWindow.Content = page;
        }

        private void JoinMatch(object sender, RoutedEventArgs e)
        {
            var code = txtJoinCode.Text?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Escribe el código de la partida.", "Unirse",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            NavigateToLobby(new LobbyNavigationArgs { Mode = LobbyEntryMode.Join, JoinCode = code });
        }

        private void btnFriends_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            return;
        }
    }
}
