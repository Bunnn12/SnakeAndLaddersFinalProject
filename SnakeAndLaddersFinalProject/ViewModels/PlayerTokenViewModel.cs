using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels.Models
{
    public sealed class PlayerTokenViewModel : INotifyPropertyChanged
    {
        private int _currentCellIndex;
        private double _verticalOffset;
        private double _x;
        private double _y;

        public int UserId { get; }
        public string UserName { get; }

        public string CurrentSkinId { get; }

        public int? CurrentSkinUnlockedId { get; }

        public int CurrentCellIndex
        {
            get { return _currentCellIndex; }
            set
            {
                if (_currentCellIndex == value)
                {
                    return;
                }

                _currentCellIndex = value;
                OnPropertyChanged();
            }
        }

        public double VerticalOffset
        {
            get { return _verticalOffset; }
            set
            {
                if (Math.Abs(_verticalOffset - value) < double.Epsilon)
                {
                    return;
                }

                _verticalOffset = value;
                OnPropertyChanged();
            }
        }

        public double X
        {
            get { return _x; }
            set
            {
                if (Math.Abs(_x - value) < double.Epsilon)
                {
                    return;
                }

                _x = value;
                OnPropertyChanged();
            }
        }

        public double Y
        {
            get { return _y; }
            set
            {
                if (Math.Abs(_y - value) < double.Epsilon)
                {
                    return;
                }

                _y = value;
                OnPropertyChanged();
            }
        }

        public string TokenImagePath { get; }

        public PlayerTokenViewModel(
            int userId,
            string userName,
            string currentSkinId,
            int? currentSkinUnlockedId,
            int initialCellIndex)
        {
            UserId = userId;

            string safeUserName = string.IsNullOrWhiteSpace(userName)
                ? string.Format(
                    Globalization.LocalizationManager.Current["GameDefaultPlayerNameFmt"],
                    userId)
                : userName;

            UserName = safeUserName;

            CurrentSkinId = currentSkinId ?? string.Empty;
            CurrentSkinUnlockedId = currentSkinUnlockedId;

            CurrentCellIndex = initialCellIndex;
            VerticalOffset = 0;

            TokenImagePath = SkinAssetHelper.GetTokenPathFromSkinId(CurrentSkinId);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
