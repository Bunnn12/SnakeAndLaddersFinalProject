using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Navigation;
using SnakeAndLaddersFinalProject.UserService;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class MainPage : Page
    {
        private const string USER_SERVICE_ENDPOINT_CONFIGURATION_NAME = "NetTcpBinding_IUserService";
        private const int DEFAULT_COINS = 0;

        private const string MESSAGE_JOIN_CODE_REQUIRED = "Escribe el código de la partida.";
        private const string TITLE_JOIN_MATCH = "Unirse";

        private const string MESSAGE_ERROR_LOADING_COINS = "Ocurrió un error al cargar tus monedas.";
        private const string TITLE_ERROR = "Error";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(MainPage));

        public string AvatarId { get; }

        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarId);

        public int Coins { get; private set; }

        public MainPage()
        {
            InitializeComponent();

            AvatarId = SessionContext.Current?.ProfilePhotoId;
            InitializeCoins();

            DataContext = SessionContext.Current;
        }

        private void InitializeCoins()
        {
            SessionContext.Current.Coins = DEFAULT_COINS;

            var session = SessionContext.Current;
            string userName = session?.UserName;

            if (session == null || !session.IsAuthenticated || string.IsNullOrWhiteSpace(userName))
            {
                return;
            }

            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                var account = client.GetProfileByUsername(userName);
                if (account != null)
                {
                    SessionContext.Current.Coins = account.Coins; 
                }

                client.Close();
            }
            catch (Exception ex)
            {
                Logger.Error("Error while loading coins for MainPage.", ex);

                try
                {
                    client.Abort();
                }
                catch (Exception abortEx)
                {
                    Logger.Warn("Error while aborting UserServiceClient after coin load failure.", abortEx);
                }

                MessageBox.Show(
                    MESSAGE_ERROR_LOADING_COINS,
                    TITLE_ERROR,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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

        private void CreateMatch(object sender, RoutedEventArgs e)
        {
            var createMatchPage = new CreateMatchPage();
            NavigateToPage(createMatchPage);
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
            }
            else
            {
                var navigationArgs = new LobbyNavigationArgs
                {
                    Mode = LobbyEntryMode.Join,
                    JoinCode = joinCode
                };

                NavigateToLobby(navigationArgs);
            }
        }

        private void BtnRanking_Click(object sender, RoutedEventArgs e)
        {
            var rankingPage = new RankingPage();
            NavigateToPage(rankingPage);
        }

        private void BtnFriends_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            //por si se necesita en el futuro.
        }

        private void BtnFriends_Click(object sender, RoutedEventArgs e)
        {
            var friendsListPage = new FriendsListPage();
            NavigateToPage(friendsListPage);
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
            }
            else
            {
                var profilePage = new ProfilePage();
                NavigateToPage(profilePage);
            }
        }

        private void BtnShop_Click(object sender, RoutedEventArgs e)
        {
            var shopPage = new ShopPage();
            NavigateToPage(shopPage);
        }
    }
}
