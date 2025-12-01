using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.StatsService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class RankingViewModel
    {
        private const int DEFAULT_MAX_RESULTS = 50;
        private const int INITIAL_POSITION = 1;

        private const string STATS_ENDPOINT_NAME = "BasicHttpBinding_IStatsService";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(RankingViewModel));

        public ObservableCollection<PlayerRankingItemViewModel> Players { get; }

        public RankingViewModel()
        {
            Players = new ObservableCollection<PlayerRankingItemViewModel>();
        }

        public void LoadRanking()
        {
            try
            {
                using (var statsClient = new StatsServiceClient(STATS_ENDPOINT_NAME))
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
                        var viewModel = new PlayerRankingItemViewModel
                        {
                            Position = currentPosition,
                            Username = player.Username,
                            Coins = player.Coins
                        };

                        Players.Add(viewModel);
                        currentPosition++;
                    }
                }
            }
            catch (FaultException faultException)
            {
                Logger.Warn("FaultException al cargar el ranking.", faultException);

                MessageBox.Show(
                    "El servidor reportó un error al obtener el ranking:\n\n" + faultException.Message,
                    "Ranking",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (CommunicationException communicationException)
            {
                Logger.Error("CommunicationException al cargar el ranking.", communicationException);

                MessageBox.Show(
                    "No se pudo comunicar con el servidor para obtener el ranking.\n" +
                    "Verifica que el servidor esté ejecutándose.",
                    "Ranking",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception exception)
            {
                Logger.Error("Error inesperado al cargar el ranking.", exception);

                MessageBox.Show(
                    "Ocurrió un error inesperado al cargar el ranking de jugadores.",
                    "Ranking",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
