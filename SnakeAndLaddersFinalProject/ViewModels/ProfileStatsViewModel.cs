using System;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.StatsService;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ProfileStatsViewModel
    {
        private const string STATS_SERVICE_ENDPOINT_CONFIGURATION_NAME = "NetTcpBinding_IStatsService";
        private const int MIN_VALID_USER_ID = 1;

        private const string CONTEXT_LOAD_STATS = "ProfileStatsViewModel.LoadStats";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(ProfileStatsViewModel));

        public int TargetUserId { get; }

        public string AvatarId { get; private set; }

        public bool HasAvatar
        {
            get { return !string.IsNullOrWhiteSpace(AvatarId); }
        }

        public string Username { get; private set; }

        public int MatchesPlayed { get; private set; }

        public int MatchesWon { get; private set; }

        public int MatchesLost
        {
            get { return MatchesPlayed - MatchesWon; }
        }

        public decimal WinPercentage { get; private set; }

        public int Coins { get; private set; }

        public int? RankingPosition { get; private set; }

        public bool IsInTopRanking
        {
            get { return RankingPosition.HasValue; }
        }

        public string StatsTitle { get; private set; }

        public static ProfileStatsViewModel CreateForCurrentUser()
        {
            var session = SessionContext.Current;

            int userIdFromSession = session?.UserId ?? 0;
            string userNameFromSession = session?.UserName;
            string avatarIdFromSession = session?.ProfilePhotoId;

            return new ProfileStatsViewModel(
                userIdFromSession,
                userNameFromSession,
                avatarIdFromSession,
                Lang.lblProfileStatsTitle);
        }

        public static ProfileStatsViewModel CreateForOtherUser(
            int userId,
            string username,
            string avatarId)
        {
            return new ProfileStatsViewModel(
                userId,
                username,
                avatarId,
                null);
        }

        private ProfileStatsViewModel(
            int targetUserId,
            string username,
            string avatarId,
            string statsTitle)
        {
            TargetUserId = targetUserId;

            AvatarId = AvatarIdHelper.NormalizeOrDefault(avatarId);
            Username = string.IsNullOrWhiteSpace(username)
                ? Lang.lblProfileUnknownUserText
                : username;

            StatsTitle = statsTitle;
        }

        public void LoadStats()
        {
            if (TargetUserId < MIN_VALID_USER_ID)
            {
                _logger.Warn("ProfileStatsViewModel.LoadStats: invalid TargetUserId.");
                MessageBox.Show(
                    Lang.errorProfileStatsInvalidUserIdText,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                ResetStatsToZero();
                return;
            }

            var client = new StatsServiceClient(STATS_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                PlayerStatsDto stats = client.GetPlayerStatsByUserId(TargetUserId);

                if (stats == null)
                {
                    _logger.Warn("ProfileStatsViewModel.LoadStats: GetPlayerStatsByUserId returned null.");
                    ResetStatsToZero();
                    return;
                }

                ApplyStats(stats);
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    CONTEXT_LOAD_STATS,
                    _logger);

                ResetStatsToZero();

                if (ConnectionLostHandlerException.IsConnectionException(ex))
                {
                    ConnectionLostHandlerException.HandleConnectionLost();
                    return;
                }

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SafeClose(client);
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
            RankingPosition = stats.RankingPosition;
        }

        private void ResetStatsToZero()
        {
            MatchesPlayed = 0;
            MatchesWon = 0;
            WinPercentage = 0m;
            Coins = 0;
            RankingPosition = null;
        }

        private static void SafeClose(StatsServiceClient client)
        {
            if (client == null)
            {
                return;
            }

            try
            {
                if (client.State == System.ServiceModel.CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    client.Close();
                }
            }
            catch
            {
                client.Abort();
            }
        }
    }
}
