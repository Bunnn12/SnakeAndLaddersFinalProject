using System.ComponentModel;
using System.Runtime.CompilerServices;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.ViewModels.Models
{
    public sealed class PodiumPlayerViewModel : INotifyPropertyChanged
    {
        private const string KEY_PODIUM_POSITION_FMT = "PodiumPositionFmt";
        private const string KEY_PODIUM_COINS_FMT = "PodiumCoinsFmt";

        public int UserId { get; private set; }
        public string DisplayName { get; private set; }

        public string UserName
        {
            get => DisplayName;
            set => DisplayName = value ?? string.Empty;
        }

        public int Position { get; private set; }
        private int _coins;
        private bool _isWinner;
        private string _skinImagePath = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public string PositionText
        {
            get
            {
                if (Position <= 0) return string.Empty;
                return string.Format(Lang.ResourceManager.GetString(KEY_PODIUM_POSITION_FMT), Position);
            }
        }

        public int Coins
        {
            get => _coins;
            set
            {
                if (_coins == value) return;
                _coins = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CoinsText));
            }
        }

        public string CoinsText
        {
            get
            {
                if (Coins <= 0) return string.Empty;
                return string.Format(Lang.ResourceManager.GetString(KEY_PODIUM_COINS_FMT), Coins);
            }
        }

        public bool IsWinner
        {
            get => _isWinner;
            set
            {
                if (_isWinner == value) return;
                _isWinner = value;
                OnPropertyChanged();
            }
        }

        public string SkinImagePath
        {
            get => _skinImagePath;
            set
            {
                if (string.Equals(_skinImagePath, value, System.StringComparison.Ordinal)) return;
                _skinImagePath = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public PodiumPlayerViewModel(int userId, string displayName, int position, int coins, string skinImagePath = "")
        {
            UserId = userId;
            DisplayName = displayName ?? string.Empty;
            Position = position;
            Coins = coins;
            SkinImagePath = skinImagePath;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
