using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.StatsService;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ProfileStatsPage : Page
    {
        private const string STATS_SERVICE_ENDPOINT_CONFIGURATION_NAME = "NetTcpBinding_IStatsService";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProfileStatsPage));

        private readonly int targetUserId;
        private readonly bool isOwnProfile;

        // Props para bindings
        public string AvatarId { get; private set; }

        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarId);

        public string Username { get; private set; }

        public int MatchesPlayed { get; private set; }

        public int MatchesWon { get; private set; }

        public int MatchesLost => MatchesPlayed - MatchesWon;

        public decimal WinPercentage { get; private set; }

        public int Coins { get; private set; }

        /// <summary>
        /// Por ahora usamos las monedas actuales como máximo histórico.
        /// Cuando exista historial de monedas se puede ajustar aquí.
        /// </summary>
        public int MaxCoinsEver { get; private set; }

        public int? RankingPosition { get; private set; }

        public bool IsInTopRanking => RankingPosition.HasValue;

        public string StatsTitle { get; private set; }

        /// <summary>
        /// Constructor para "mi perfil" (desde menú de perfil).
        /// Usa los datos de la sesión actual.
        /// </summary>
        public ProfileStatsPage()
        {
            var session = SessionContext.Current;

            int userIdFromSession = session?.UserId ?? 0;
            string userNameFromSession = session?.UserName;
            string avatarIdFromSession = session?.ProfilePhotoId;

            InitializeComponent();

            targetUserId = userIdFromSession;
            isOwnProfile = true;

            AvatarId = AvatarIdHelper.NormalizeOrDefault(avatarIdFromSession);
            Username = string.IsNullOrWhiteSpace(userNameFromSession)
                ? "Lang.lblProfileUnknownUserText"
                : userNameFromSession;

            StatsTitle = Lang.lblProfileStatsTitle;

            DataContext = this;
        }

        /// <summary>
        /// Constructor para ver el perfil de estadísticas de otro usuario (por ejemplo, desde la lista de amigos).
        /// </summary>
        public ProfileStatsPage(int userId, string username, string avatarId)
        {
            InitializeComponent();

            targetUserId = userId;
            isOwnProfile = false;

            AvatarId = AvatarIdHelper.NormalizeOrDefault(avatarId);
            Username = string.IsNullOrWhiteSpace(username)
                ? "Lang.lblProfileUnknownUserText"
                : username;

            DataContext = this;
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            LoadStats();
        }

        private void LoadStats()
        {
            if (targetUserId <= 0)
            {
                Logger.Warn("ProfileStatsPage: targetUserId inválido.");
                MessageBox.Show(
                    "Lang.errorProfileStatsInvalidUserIdText",
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                ResetStatsToZero();
                return;
            }

            var client = new StatsServiceClient(STATS_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                PlayerStatsDto stats = client.GetPlayerStatsByUserId(targetUserId);

                if (stats == null)
                {
                    Logger.Warn("ProfileStatsPage: GetPlayerStatsByUserId devolvió null.");
                    ResetStatsToZero();
                    return;
                }

                ApplyStats(stats);
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading player stats.", ex);

                MessageBox.Show(
                    "Lang.errorLoadingProfileStatsText",
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                ResetStatsToZero();
            }
            finally
            {
                try
                {
                    client.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while closing StatsServiceClient in ProfileStatsPage.", ex);
                }
            }
        }

        private void ApplyStats(PlayerStatsDto stats)
        {
            if (stats == null)
            {
                ResetStatsToZero();
                return;
            }

            MatchesPlayed = stats.MatchesPlayed;
            MatchesWon = stats.MatchesWon;
            WinPercentage = stats.WinPercentage;
            Coins = stats.Coins;

            // De momento usamos las monedas actuales como máximo histórico.
            MaxCoinsEver = stats.Coins;

            RankingPosition = stats.RankingPosition;

            RefreshBindings();
        }

        private void ResetStatsToZero()
        {
            MatchesPlayed = 0;
            MatchesWon = 0;
            WinPercentage = 0m;
            Coins = 0;
            MaxCoinsEver = 0;
            RankingPosition = null;

            RefreshBindings();
        }

        private void RefreshBindings()
        {
            var currentDataContext = DataContext;
            DataContext = null;
            DataContext = currentDataContext;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
                return;
            }

            var navigationWindow = Application.Current.MainWindow as NavigationWindow;
            if (navigationWindow != null && navigationWindow.CanGoBack)
            {
                navigationWindow.GoBack();
                return;
            }

            // Fallback: si es mi perfil, vuelvo a ProfilePage; si es otro, regreso a MainPage
            Page fallbackPage = isOwnProfile
                ? (Page)new ProfilePage()
                : new MainPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(fallbackPage);
                return;
            }

            if (navigationWindow != null)
            {
                navigationWindow.Navigate(fallbackPage);
            }
            else
            {
                Application.Current.MainWindow.Content = fallbackPage;
            }
        }
    }
}
