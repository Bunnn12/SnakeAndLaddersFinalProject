using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class PodiumViewModel : INotifyPropertyChanged
    {
        private const int MAX_PODIUM_PLAYERS = 3;
        private const int FIRST_PLACE_INDEX = 0;
        private const int SECOND_PLACE_INDEX = 1;
        private const int THIRD_PLACE_INDEX = 2;

        private string title = "Resultados de la partida";
        private string winnerName = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Acción externa para navegar fuera del podio (volver al lobby, menú, etc.).
        /// La configura el shell / ventana principal.
        /// </summary>
        public Action CloseRequested { get; set; }

        public ObservableCollection<PodiumPlayerViewModel> Players { get; }

        public ICommand CloseCommand { get; }

        public string Title
        {
            get { return title; }
            set
            {
                if (string.Equals(title, value, StringComparison.Ordinal))
                {
                    return;
                }

                title = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string WinnerName
        {
            get { return winnerName; }
            set
            {
                if (string.Equals(winnerName, value, StringComparison.Ordinal))
                {
                    return;
                }

                winnerName = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public PodiumPlayerViewModel FirstPlace
        {
            get
            {
                if (Players.Count > FIRST_PLACE_INDEX)
                {
                    return Players[FIRST_PLACE_INDEX];
                }

                return null;
            }
        }

        public PodiumPlayerViewModel SecondPlace
        {
            get
            {
                if (Players.Count > SECOND_PLACE_INDEX)
                {
                    return Players[SECOND_PLACE_INDEX];
                }

                return null;
            }
        }

        public PodiumPlayerViewModel ThirdPlace
        {
            get
            {
                if (Players.Count > THIRD_PLACE_INDEX)
                {
                    return Players[THIRD_PLACE_INDEX];
                }

                return null;
            }
        }

        public PodiumViewModel()
        {
            Players = new ObservableCollection<PodiumPlayerViewModel>();
            CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke());
        }

        /// <summary>
        /// Ctor que usa GameBoardPage: winnerUserId + jugadores ordenados.
        /// </summary>
        public PodiumViewModel(
            int winnerUserId,
            ReadOnlyCollection<PodiumPlayerViewModel> orderedPlayers)
            : this()
        {
            Initialize(winnerUserId, orderedPlayers);
        }

        /// <summary>
        /// Inicializa sin nombre explícito del ganador.
        /// </summary>
        public void Initialize(
            int winnerUserId,
            ReadOnlyCollection<PodiumPlayerViewModel> orderedPlayers)
        {
            Initialize(winnerUserId, null, orderedPlayers);
        }

        /// <summary>
        /// Inicializa con nombre opcional del ganador.
        /// </summary>
        public void Initialize(
            int winnerUserId,
            string winnerDisplayName,
            ReadOnlyCollection<PodiumPlayerViewModel> orderedPlayers)
        {
            Players.Clear();

            if (orderedPlayers != null)
            {
                int count = 0;

                foreach (PodiumPlayerViewModel player in orderedPlayers)
                {
                    if (player == null)
                    {
                        continue;
                    }

                    count++;

                    if (count > MAX_PODIUM_PLAYERS)
                    {
                        break;
                    }

                    Players.Add(player);

                    if (player.UserId == winnerUserId)
                    {
                        player.IsWinner = true;
                    }
                }
            }

            string effectiveWinnerName = string.IsNullOrWhiteSpace(winnerDisplayName)
                ? null
                : winnerDisplayName.Trim();

            if (effectiveWinnerName == null)
            {
                effectiveWinnerName = string.Format("Jugador con Id {0}", winnerUserId);
            }

            WinnerName = effectiveWinnerName;

            OnPropertyChanged(nameof(FirstPlace));
            OnPropertyChanged(nameof(SecondPlace));
            OnPropertyChanged(nameof(ThirdPlace));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler == null)
            {
                return;
            }

            PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);
            handler(this, args);
        }
    }
}
