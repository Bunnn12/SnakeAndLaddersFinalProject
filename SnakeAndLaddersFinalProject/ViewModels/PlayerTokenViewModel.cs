using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels.Models
{
    public sealed class PlayerTokenViewModel : INotifyPropertyChanged
    {
        private int currentCellIndex;
        private double verticalOffset;

        public int UserId { get; }
        public string UserName { get; }

        public int? CurrentSkinUnlockedId { get; }

        public int CurrentCellIndex
        {
            get { return currentCellIndex; }
            set
            {
                if (currentCellIndex == value)
                {
                    return;
                }

                currentCellIndex = value;
                OnPropertyChanged();
            }
        }

        public double VerticalOffset
        {
            get { return verticalOffset; }
            set
            {
                if (Math.Abs(verticalOffset - value) < 0.001)
                {
                    return;
                }

                verticalOffset = value;
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
            UserName = string.IsNullOrWhiteSpace(userName)
                ? $"Jugador {userId}"
                : userName;

            CurrentSkinUnlockedId = currentSkinUnlockedId;
            CurrentCellIndex = initialCellIndex;
            VerticalOffset = 0;

            TokenImagePath = SkinAssetHelper.GetTokenPathFromSkinId(currentSkinUnlockedId);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
