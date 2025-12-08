using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Navigation;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.UserService;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class MainPage : Page
    {
        private const string USER_SERVICE_ENDPOINT_CONFIGURATION_NAME = "NetTcpBinding_IUserService";
        private const int DEFAULT_COINS = 0;
        private const int HOST_USER_ID_THRESHOLD = 0;

        private static readonly ILog _logger = LogManager.GetLogger(typeof(MainPage));

        public string AvatarId { get; }

        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarId);

        public int Coins { get; private set; }

        public MainPage()
        {
            InitializeComponent();

            AvatarId = SessionContext.Current?.ProfilePhotoId;
            InitializeCoins();

            DataContext = SessionContext.Current;

            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyHostRestrictionsIfNeeded();
        }

        private void ApplyHostRestrictionsIfNeeded()
        {
            SessionContext session = SessionContext.Current;

            if (session == null)
            {
                return;
            }

            int userId = session.UserId;

            // Negativos = host/guest especial
            bool isHostUser = userId < HOST_USER_ID_THRESHOLD;

            if (!isHostUser)
            {
                return;
            }

            // Deshabilitar todos los botones de menú…
            if (btnBack != null)
            {
                btnBack.IsEnabled = false;
            }

            if (btnSettings != null)
            {
                btnSettings.IsEnabled = false;
            }

            if (btnCreateMatch != null)
            {
                btnCreateMatch.IsEnabled = false;
            }

            if (btnFriends != null)
            {
                btnFriends.IsEnabled = false;
            }

            if (btnShop != null)
            {
                btnShop.IsEnabled = false;
            }

            if (btnInventory != null)
            {
                btnInventory.IsEnabled = false;
            }

            if (btnSkins != null)
            {
                btnSkins.IsEnabled = false;
            }

            if (btnProfile != null)
            {
                btnProfile.IsEnabled = false;
            }

            if (btnRanking != null)
            {
                btnRanking.IsEnabled = false;
            }

            // …menos “Unirse a partida” y el textbox del código
            if (btnJoinMatch != null)
            {
                btnJoinMatch.IsEnabled = true;
            }

            if (txtJoinCode != null)
            {
                txtJoinCode.IsEnabled = true;
            }
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
                _logger.Error("Error while loading coins for MainPage.", ex);

                try
                {
                    client.Abort();
                }
                catch (Exception abortEx)
                {
                    _logger.Warn("Error while aborting UserServiceClient after coin load failure.", abortEx);
                }

                MessageBox.Show(
                    Lang.MainPageErrorLoadingCoinsText,
                    Lang.errorTitle,
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

        private void JoinMatch(object sender, RoutedEventArgs e)
        {
            var matchListPage = new MatchListPage();
            NavigateToPage(matchListPage);
        }

        private void Ranking(object sender, RoutedEventArgs e)
        {
            var rankingPage = new RankingPage();
            NavigateToPage(rankingPage);
        }

        private void Friends(object sender, RoutedEventArgs e)
        {
            var friendsListPage = new FriendsListPage();
            NavigateToPage(friendsListPage);
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
            }
            else
            {
                var profilePage = new ProfilePage();
                NavigateToPage(profilePage);
            }
        }

        private void Shop(object sender, RoutedEventArgs e)
        {
            var shopPage = new ShopPage();
            NavigateToPage(shopPage);
        }

        private void OpenInventory(object sender, RoutedEventArgs e)
        {
            var inventoryPage = new InventoryPage();
            NavigateToPage(inventoryPage);
        }
    }
}
