using System.ComponentModel;
using System.Runtime.CompilerServices;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels.Models
{
    public sealed class PlayerTokenViewModel : INotifyPropertyChanged
    {
        private const string APP_ASSEMBLY_NAME = "SnakeAndLaddersFinalProject";

        public int UserId { get; }
        public string UserName { get; }
        public string SkinId { get; }

        /// <summary>
        /// Pack URI de la imagen de la ficha (token) según la skin seleccionada.
        /// Ejemplo:
        /// pack://application:,,,/SnakeAndLaddersFinalProject;component/Assets/Images/Skins/Tokens/T003.png
        /// </summary>
        public string TokenImagePath { get; }

        private int currentCellIndex;
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

        public PlayerTokenViewModel(int userId, string userName, string skinId, int startCellIndex)
        {
            UserId = userId;
            UserName = userName;
            SkinId = skinId;
            currentCellIndex = startCellIndex;

            string relativeTokenPath = SkinAssetHelper.GetTokenPathFromSkinId(SkinId);

            TokenImagePath = string.Concat(
                "pack://application:,,,/",
                APP_ASSEMBLY_NAME,
                ";component",
                relativeTokenPath);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
