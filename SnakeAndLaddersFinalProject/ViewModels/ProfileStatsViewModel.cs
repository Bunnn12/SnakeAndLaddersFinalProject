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
            if (TargetUserId <= 0)
            {
                _logger.Warn("ProfileStatsPage: targetUserId inválido.");
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
                    _logger.Warn("ProfileStatsPage: GetPlayerStatsByUserId devolvió null.");
                    ResetStatsToZero();
                    return;
                }

                ApplyStats(stats);
            }
            catch (Exception ex)
            {
                _logger.Error("Error loading player stats.", ex);

                MessageBox.Show(
                    Lang.errorLoadingProfileStatsText,
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
                    _logger.Error("Error while closing StatsServiceClient in ProfileStatsPage.", ex);
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
    }
}
