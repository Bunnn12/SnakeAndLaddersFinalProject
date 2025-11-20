using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Navigation;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class MainPage : Page
    {
        private const string MESSAGE_JOIN_CODE_REQUIRED = "Escribe el código de la partida.";
        private const string TITLE_JOIN_MATCH = "Unirse";

        public string AvatarId { get; }

        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarId);

        public MainPage()
        {
            InitializeComponent();

            AvatarId = SessionContext.Current?.ProfilePhotoId;

            DataContext = this;
        }

        private void CreateMatch(object sender, RoutedEventArgs e)
        {
            var createMatchPage = new CreateMatchPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(createMatchPage);
                return;
            }

            var navigationWindow = Application.Current.MainWindow as NavigationWindow;
            if (navigationWindow != null)
            {
                navigationWindow.Navigate(createMatchPage);
            }
            else
            {
                Application.Current.MainWindow.Content = createMatchPage;
            }
        }

        private void NavigateToLobby(LobbyNavigationArgs navigationArgs)
        {
            var lobbyPage = new LobbyPage(navigationArgs);

            if (NavigationService != null)
            {
                NavigationService.Navigate(lobbyPage);
                return;
            }

            var navigationWindow = Application.Current.MainWindow as NavigationWindow;
            if (navigationWindow != null)
            {
                navigationWindow.Navigate(lobbyPage);
            }
            else
            {
                Application.Current.MainWindow.Content = lobbyPage;
            }
        }

        private void JoinMatch(object sender, RoutedEventArgs e)
        {
            string joinCode = txtJoinCode.Text?.Trim();
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                MessageBox.Show(
                    MESSAGE_JOIN_CODE_REQUIRED,
                    TITLE_JOIN_MATCH,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            NavigateToLobby(
                new LobbyNavigationArgs
                {
                    Mode = LobbyEntryMode.Join,
                    JoinCode = joinCode
                });
        }

        private void BtnRanking_Click(object sender, RoutedEventArgs e)
        {
            var rankingPage = new RankingPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(rankingPage);
                return;
            }

            var navigationWindow = Application.Current.MainWindow as NavigationWindow;
            if (navigationWindow != null)
            {
                navigationWindow.Navigate(rankingPage);
            }
            else
            {
                Application.Current.MainWindow.Content = rankingPage;
            }
        }

        private void BtnFriends_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            return;
        }

        private void BtnFriends_Click(object sender, RoutedEventArgs e)
        {
            var friendsListPage = new FriendsListPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(friendsListPage);
                return;
            }

            var navigationWindow = Application.Current.MainWindow as NavigationWindow;
            if (navigationWindow != null)
            {
                navigationWindow.Navigate(friendsListPage);
            }
            else
            {
                Application.Current.MainWindow.Content = friendsListPage;
            }
        }
    }
}
