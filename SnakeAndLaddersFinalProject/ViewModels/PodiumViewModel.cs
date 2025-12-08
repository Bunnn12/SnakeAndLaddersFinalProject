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

        private const string KEY_PODIUM_TITLE_DEFAULT = "PodiumTitleDefault";
        private const string KEY_PODIUM_WINNER_FALLBACK_FMT = "PodiumWinnerFallbackFmt";

        private string _title;
        private string _winnerName = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public Action CloseRequested { get; set; }

        public ObservableCollection<PodiumPlayerViewModel> Players { get; }

        public ICommand CloseCommand { get; }

        public string Title
        {
            get { return _title; }
            set
            {
                if (string.Equals(_title, value, StringComparison.Ordinal))
                {
                    return;
                }

                _title = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string WinnerName
        {
            get { return _winnerName; }
            set
            {
                if (string.Equals(_winnerName, value, StringComparison.Ordinal))
                {
                    return;
                }

                _winnerName = value ?? string.Empty;
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

            Title = T(KEY_PODIUM_TITLE_DEFAULT);
        }

        public PodiumViewModel(
            int winnerUserId,
            ReadOnlyCollection<PodiumPlayerViewModel> orderedPlayers)
            : this()
        {
            Initialize(winnerUserId, orderedPlayers);
        }

        public void Initialize(
            int winnerUserId,
            ReadOnlyCollection<PodiumPlayerViewModel> orderedPlayers)
        {
            Initialize(winnerUserId, null, orderedPlayers);
        }

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
                effectiveWinnerName = string.Format(
                    T(KEY_PODIUM_WINNER_FALLBACK_FMT),
                    winnerUserId);
            }

            WinnerName = effectiveWinnerName;

            OnPropertyChanged(nameof(FirstPlace));
            OnPropertyChanged(nameof(SecondPlace));
            OnPropertyChanged(nameof(ThirdPlace));
        }

        private static string T(string key)
        {
            return Globalization.LocalizationManager.Current[key];
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
