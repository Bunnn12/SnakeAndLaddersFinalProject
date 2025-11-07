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

        private const string STATS_ENDPOINT_NAME = "BasicHttpBinding_IStatsService";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(RankingViewModel));

        public ObservableCollection<PlayerRankingItemViewModel> Players { get; }

        public RankingViewModel()
        {
            Players = new ObservableCollection<PlayerRankingItemViewModel>();
            LoadRanking();
        }

        private void LoadRanking()
        {
            try
            {
                using (var client = new StatsServiceClient(STATS_ENDPOINT_NAME))
                {
                    var dtos = client.GetTopPlayersByCoins(DEFAULT_MAX_RESULTS);

                    if (dtos == null || dtos.Length == 0)
                    {
                        Players.Clear();
                        return;
                    }

                    Players.Clear();

                    var ordered = dtos
                        .OrderByDescending(x => x.Coins)
                        .ThenBy(x => x.Username)
                        .ToList();

                    var position = 1;

                    foreach (var dto in ordered)
                    {
                        var viewModel = new PlayerRankingItemViewModel
                        {
                            Position = position,
                            Username = dto.Username,
                            Coins = dto.Coins
                        };

                        Players.Add(viewModel);
                        position++;
                    }
                }
            }
            catch (FaultException faultEx)
            {
                Logger.Warn("FaultException al cargar el ranking.", faultEx);

                MessageBox.Show(
                    "El servidor reportó un error al obtener el ranking:\n\n" + faultEx.Message,
                    "Ranking",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (CommunicationException commEx)
            {
                Logger.Error("CommunicationException al cargar el ranking.", commEx);

                MessageBox.Show(
                    "No se pudo comunicar con el servidor para obtener el ranking.\n" +
                    "Verifica que el servidor esté ejecutándose.",
                    "Ranking",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Logger.Error("Error inesperado al cargar el ranking.", ex);

                MessageBox.Show(
                    "Ocurrió un error inesperado al cargar el ranking de jugadores.",
                    "Ranking",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
