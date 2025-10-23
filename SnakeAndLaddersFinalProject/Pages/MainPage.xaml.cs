using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.Navigation;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void CreateMatch(object sender, RoutedEventArgs e)
        {
            NavigateToLobby(new LobbyNavigationArgs { Mode = LobbyEntryMode.Create });
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
                MessageBox.Show("Escribe el código de la partida.", "Unirse", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            NavigateToLobby(new LobbyNavigationArgs { Mode = LobbyEntryMode.Join, JoinCode = code });

        }

        private void btnFriends_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {

        }
    }
}
