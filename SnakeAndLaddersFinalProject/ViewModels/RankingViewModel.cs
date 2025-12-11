using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.StatsService;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class RankingViewModel
    {
        private const int DEFAULT_MAX_RESULTS = 50;
        private const int INITIAL_POSITION = 1;

        private const string STATS_ENDPOINT_NAME = "BasicHttpBinding_IStatsService";

        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(RankingViewModel));

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
                        var playerRankingViewModel = new PlayerRankingItemViewModel
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
            catch (Exception ex)
            {
                ExceptionHandler.Handle(
                    ex,
                    "RankingViewModel.LoadRanking",
                    _logger);

                MessageBox.Show(
                    Lang.UiRankingLoadError,
                    Lang.lblRankingTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

    }
}
