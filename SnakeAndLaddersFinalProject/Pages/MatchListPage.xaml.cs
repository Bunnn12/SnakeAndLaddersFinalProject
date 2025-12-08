using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Navigation;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class MatchListPage : Page
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(MatchListPage));

        public MatchListPage()
        {
            InitializeComponent();

            DataContext = new LobbyViewModel();
        }

        private Frame FindMainFrame()
        {
            Window owner = Window.GetWindow(this) ?? Application.Current.MainWindow;
            return owner?.FindName("MainFrame") as Frame;
        }

        private void NavigateToPage(Page targetPage)
        {
            if (targetPage == null)
            {
                throw new ArgumentNullException(nameof(targetPage));
            }

            var mainFrame = FindMainFrame();

            if (mainFrame != null)
            {
                mainFrame.Navigate(targetPage);
            }
            else if (NavigationService != null)
            {
                NavigationService.Navigate(targetPage);
            }
            else if (Application.Current.MainWindow is NavigationWindow navigationWindow)
            {
                navigationWindow.Navigate(targetPage);
            }
            else if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Content = targetPage;
            }
        }

        private void NavigateToLobby(LobbyNavigationArgs navigationArgs)
        {
            if (navigationArgs == null)
            {
                throw new ArgumentNullException(nameof(navigationArgs));
            }

            var lobbyPage = new LobbyPage(navigationArgs);
            NavigateToPage(lobbyPage);
        }

        private void JoinMatchClick(object sender, RoutedEventArgs e)
        {
            string joinCode = txtFindMatch.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                MessageBox.Show(
                    Lang.UiJoinMatchCodeRequired,
                    Lang.UiJoinMatchTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var navigationArgs = new LobbyNavigationArgs
            {
                Mode = LobbyEntryMode.Join,
                JoinCode = joinCode
            };

            NavigateToLobby(navigationArgs);
        }

        private void TvMatchListMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (tvMatchList.SelectedItem == null)
            {
                return;
            }

            dynamic selected = tvMatchList.SelectedItem;
            string joinCode = selected?.CodigoPartida as string;

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                return;
            }

            var navigationArgs = new LobbyNavigationArgs
            {
                Mode = LobbyEntryMode.Join,
                JoinCode = joinCode
            };

            NavigateToLobby(navigationArgs);
        }

        private void Back(object sender, RoutedEventArgs e)
        {
            var mainPage = new MainPage();
            NavigateToPage(mainPage);
        }

        private void Settings(object sender, RoutedEventArgs e)
        {
            var settingsPage = new SettingsPage();
            NavigateToPage(settingsPage);
        }

        private void JoinByCode(object sender, RoutedEventArgs e)
        {
            string joinCode = txtFindMatch.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                MessageBox.Show(
                    Lang.UiJoinMatchCodeRequired,
                    Lang.UiJoinMatchTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var navigationArgs = new LobbyNavigationArgs
            {
                Mode = LobbyEntryMode.Join,
                JoinCode = joinCode
            };

            NavigateToLobby(navigationArgs);
        }

        private void Friends(object sender, RoutedEventArgs e)
        {
            var friendsListPage = new FriendsListPage();
            NavigateToPage(friendsListPage);
        }

        private void Shop(object sender, RoutedEventArgs e)
        {
            var shopPage = new ShopPage();
            NavigateToPage(shopPage);
        }

        private void Inventory(object sender, RoutedEventArgs e)
        {
            var inventoryPage = new InventoryPage();
            NavigateToPage(inventoryPage);
        }

        private void Skins(object sender, RoutedEventArgs e)
        {
            var skinsPage = new SkinsPage();
            NavigateToPage(skinsPage);
        }

        private void Profile(object sender, RoutedEventArgs e)
        {
            var session = SessionContext.Current;

            if (session == null || !session.IsAuthenticated)
            {
                MessageBox.Show(
                    Lang.ProfileGuestNotAllowedText,
                    Lang.ProfileGuestNotAllowedTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var profilePage = new ProfilePage();
            NavigateToPage(profilePage);
        }
    }
}
