using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using SnakeAndLaddersFinalProject.Properties.Langs;

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
            get => _title;
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
            get => _winnerName;
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
            get => GetPlayerAtIndex(FIRST_PLACE_INDEX);
        }

        public Visibility FirstPlaceVisibility => FirstPlace != null ? Visibility.Visible :
            Visibility.Collapsed;

        public PodiumPlayerViewModel SecondPlace
        {
            get => GetPlayerAtIndex(SECOND_PLACE_INDEX);
        }

        public Visibility SecondPlaceVisibility => SecondPlace != null ? Visibility.Visible :
            Visibility.Collapsed;

        public PodiumPlayerViewModel ThirdPlace
        {
            get => GetPlayerAtIndex(THIRD_PLACE_INDEX);
        }

        public Visibility ThirdPlaceVisibility => ThirdPlace != null ? Visibility.Visible :
            Visibility.Collapsed;

        public PodiumViewModel()
        {
            Players = new ObservableCollection<PodiumPlayerViewModel>();
            CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke());
            Title = T(KEY_PODIUM_TITLE_DEFAULT);
        }

        public PodiumViewModel(int winnerUserId, ReadOnlyCollection<PodiumPlayerViewModel>
            orderedPlayers) : this()
        {
            Initialize(winnerUserId, orderedPlayers);
        }

        public void Initialize(int winnerUserId, ReadOnlyCollection<PodiumPlayerViewModel> orderedPlayers)
        {
            Initialize(winnerUserId, null, orderedPlayers);
        }

        public void Initialize(int winnerUserId, string winnerDisplayName,
            ReadOnlyCollection<PodiumPlayerViewModel> orderedPlayers)
        {
            Players.Clear();

            if (orderedPlayers != null)
            {
                int count = 0;
                foreach (var player in orderedPlayers)
                {
                    if (player == null) continue;

                    count++;
                    if (count > MAX_PODIUM_PLAYERS) break;

                    Players.Add(player);

                    if (player.UserId == winnerUserId)
                    {
                        player.IsWinner = true;
                    }
                }
            }

            string effectiveWinnerName = string.IsNullOrWhiteSpace(winnerDisplayName)
                ? string.Format(T(KEY_PODIUM_WINNER_FALLBACK_FMT), winnerUserId)
                : winnerDisplayName.Trim();

            WinnerName = effectiveWinnerName;

            NotifyPropertiesChanged();
        }

        private PodiumPlayerViewModel GetPlayerAtIndex(int index)
        {
            if (Players.Count > index)
            {
                return Players[index];
            }
            return null;
        }

        private void NotifyPropertiesChanged()
        {
            OnPropertyChanged(nameof(FirstPlace));
            OnPropertyChanged(nameof(FirstPlaceVisibility));
            OnPropertyChanged(nameof(SecondPlace));
            OnPropertyChanged(nameof(SecondPlaceVisibility));
            OnPropertyChanged(nameof(ThirdPlace));
            OnPropertyChanged(nameof(ThirdPlaceVisibility));
        }

        private static string T(string key)
        {
            return Lang.ResourceManager.GetString(key);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
