using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.StatsService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class RankingViewModel
    {
        private const int DEFAULT_MAX_RESULTS = 50;
        private const int INITIAL_POSITION = 1;

        private const string STATS_ENDPOINT_NAME = "BasicHttpBinding_IStatsService";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(RankingViewModel));

        public ObservableCollection<PlayerRankingItemViewModel> Players { get; }

        public RankingViewModel()
        {
            Players = new ObservableCollection<PlayerRankingItemViewModel>();
        }

        public void LoadRanking()
        {
            try
            {
                using (StatsServiceClient statsClient = new StatsServiceClient(STATS_ENDPOINT_NAME))
                {
                    var rankingItems = statsClient.GetTopPlayersByCoins(DEFAULT_MAX_RESULTS);

                    if (rankingItems == null || rankingItems.Length == 0)
                    {
                        Players.Clear();
                        return;
                    }

                    Players.Clear();

                    var orderedPlayers = rankingItems
                        .OrderByDescending(player => player.Coins)
                        .ThenBy(player => player.Username)
                        .ToList();

                    int currentPosition = INITIAL_POSITION;

                    foreach (var player in orderedPlayers)
                    {
                        PlayerRankingItemViewModel playerRankingViewModel = new PlayerRankingItemViewModel
                        {
                            Position = currentPosition,
                            Username = player.Username,
                            Coins = player.Coins
                        };

                        Players.Add(playerRankingViewModel);
                        currentPosition++;
                    }
                }
            }
            catch (FaultException faultException)
            {
                _logger.Warn("FaultException al cargar el ranking.", faultException);

                string message = string.Format(
                    Lang.errorRankingServerFaultTextFmt,
                    faultException.Message);

                MessageBox.Show(
                    message,
                    Lang.lblRankingTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (CommunicationException communicationException)
            {
                _logger.Error("CommunicationException al cargar el ranking.", communicationException);

                MessageBox.Show(
                    Lang.errorRankingCommunicationText,
                    Lang.lblRankingTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception exception)
            {
                _logger.Error("Error inesperado al cargar el ranking.", exception);

                MessageBox.Show(
                    Lang.errorRankingUnexpectedText,
                    Lang.lblRankingTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
