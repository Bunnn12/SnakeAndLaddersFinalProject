using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
            get { return DisplayName; }
            set { DisplayName = value ?? string.Empty; }
        }

        public int Position { get; private set; }

        private int coins;
        private bool isWinner;
        private string skinImagePath = string.Empty;

        public string PositionText
        {
            get
            {
                if (Position <= 0)
                {
                    return string.Empty;
                }

                return string.Format(T(KEY_PODIUM_POSITION_FMT), Position);
            }
        }

        public int Coins
        {
            get { return coins; }
            set
            {
                if (coins == value)
                {
                    return;
                }

                coins = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CoinsText));
            }
        }

        public string CoinsText
        {
            get
            {
                if (Coins <= 0)
                {
                    return string.Empty;
                }

                return string.Format(T(KEY_PODIUM_COINS_FMT), Coins);
            }
        }

        public bool IsWinner
        {
            get { return isWinner; }
            set
            {
                if (isWinner == value)
                {
                    return;
                }

                isWinner = value;
                OnPropertyChanged();
            }
        }

        public string SkinImagePath
        {
            get { return skinImagePath; }
            set
            {
                if (string.Equals(skinImagePath, value, StringComparison.Ordinal))
                {
                    return;
                }

                skinImagePath = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public PodiumPlayerViewModel(int userId, string displayName, int position, int coins)
        {
            UserId = userId;
            DisplayName = displayName ?? string.Empty;
            Position = position;
            Coins = coins;
        }

        public PodiumPlayerViewModel(
            int userId,
            string displayName,
            int position,
            int coins,
            string skinImagePath)
            : this(userId, displayName, position, coins)
        {
            SkinImagePath = skinImagePath;
        }

        public PodiumPlayerViewModel(
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5,
            object arg6,
            object arg7)
        {
            UserId = arg1 is int i ? i : 0;
            DisplayName = arg2 as string ?? string.Empty;
            IsWinner = arg3 is bool b && b;
            Position = arg4 is int p ? p : 0;
            Coins = arg5 is int c ? c : 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
