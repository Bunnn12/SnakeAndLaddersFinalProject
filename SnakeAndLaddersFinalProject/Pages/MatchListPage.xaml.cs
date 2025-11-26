using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Navigation;
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
        private const string MESSAGE_JOIN_CODE_REQUIRED = "Escribe el código de la partida.";
        private const string TITLE_JOIN_MATCH = "Unirse";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchListPage));


        public MatchListPage()
        {
            InitializeComponent();

            DataContext = new LobbyViewModel(); // 👈 aquí vive PublicLobbies
        }

        // ------------ navegación base ------------

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
                    MESSAGE_JOIN_CODE_REQUIRED,
                    TITLE_JOIN_MATCH,
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

        // --------- Unirse haciendo doble clic en la lista ---------

        private void TvMatchListMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (tvMatchList.SelectedItem == null)
            {
                return;
            }

            // Supongamos que tus filas son de tipo LobbySummaryViewModel
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

        // ------------ handlers UI ------------

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            var mainPage = new MainPage();
            NavigateToPage(mainPage);
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            // si tienes una página de settings, navega ahí
            var settingsPage = new SettingsPage();
            NavigateToPage(settingsPage);
        }

        private void BtnJoinByCode_Click(object sender, RoutedEventArgs e)
        {
            string joinCode = txtFindMatch.Text?.Trim();

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                MessageBox.Show(
                    MESSAGE_JOIN_CODE_REQUIRED,
                    TITLE_JOIN_MATCH,
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

        private void BtnFriends_Click(object sender, RoutedEventArgs e)
        {
            var friendsListPage = new FriendsListPage();
            NavigateToPage(friendsListPage);
        }

        private void BtnShop_Click(object sender, RoutedEventArgs e)
        {
            var shopPage = new ShopPage();
            NavigateToPage(shopPage);
        }

        private void BtnInventory_Click(object sender, RoutedEventArgs e)
        {
            var inventoryPage = new InventoryPage();
            NavigateToPage(inventoryPage);
        }

        private void BtnSkins_Click(object sender, RoutedEventArgs e)
        {
            var skinsPage = new SkinsPage();
            NavigateToPage(skinsPage);
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            var session = SessionContext.Current;

            if (session == null || !session.IsAuthenticated)
            {
                MessageBox.Show(
                    "Iniciaste sesión como invitado, no puedes acceder al perfil.\n\n" +
                    "Si deseas usar un perfil, crea una cuenta :).",
                    "Perfil no disponible",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var profilePage = new ProfilePage();
            NavigateToPage(profilePage);
        }
    }
}
