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
        private double y;

        public int UserId { get; }
        public string UserName { get; }
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
                if (_verticalOffset.Equals(value))
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
                if (_x.Equals(value))
                {
                    return;
                }

                _x = value;
                OnPropertyChanged();
            }
        }

        public double Y
        {
            get { return y; }
            set
            {
                if (y.Equals(value))
                {
                    return;
                }

                y = value;
                OnPropertyChanged();
            }
        }

        public string TokenImagePath { get; }

        public PlayerTokenViewModel(
            int userId,
            string userName,
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

            CurrentSkinUnlockedId = currentSkinUnlockedId;
            CurrentCellIndex = initialCellIndex;
            VerticalOffset = 0;

            TokenImagePath = SkinAssetHelper.GetTokenPathFromSkinId(currentSkinUnlockedId);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
