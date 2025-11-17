using SnakeAndLaddersFinalProject.Utilities;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnakeAndLaddersFinalProject.ViewModels.Models
{
    public sealed class LobbyMemberViewModel : INotifyPropertyChanged
    {
        public int UserId { get; }
        public string UserName { get; }
        public DateTime JoinedAt { get; }

        public string AvatarId { get; }

        public string CurrentSkinId { get; }
        public int? CurrentSkinUnlockedId { get; }

        // Clave normalizada de skin (002, 003, etc.)
        public string SkinKey => SkinAssetHelper.NormalizeSkinKey(CurrentSkinId);

        // Claves lógicas (por si las necesitas en otro lado)
        public string TokenKey => SkinAssetHelper.ResolveAssets(CurrentSkinId).TokenKey;
        public string IdleKey => SkinAssetHelper.ResolveAssets(CurrentSkinId).IdleKey;
        public string SadKey => SkinAssetHelper.ResolveAssets(CurrentSkinId).SadKey;

        // 🔹 RUTA COMPLETA PARA LA SKIN GRANDE (lo que usa el XAML)
        public string SkinImagePath => SkinAssetHelper.GetSkinPathFromSkinId(CurrentSkinId);

        // 🔹 Si luego quieres usar el token en el tablero
        public string TokenImagePath => SkinAssetHelper.GetTokenPathFromSkinId(CurrentSkinId);

        private bool _isHost;
        public bool IsHost
        {
            get => _isHost;
            set
            {
                if (_isHost == value)
                {
                    return;
                }

                _isHost = value;
                OnPropertyChanged();
            }
        }

        public LobbyMemberViewModel(int userId, string userName, bool isHost, DateTime joinedAt)
        {
            UserId = userId;
            UserName = userName;
            JoinedAt = joinedAt;
            _isHost = isHost;
        }

        public LobbyMemberViewModel(
            int userId,
            string userName,
            bool isHost,
            DateTime joinedAt,
            string avatarId)
            : this(userId, userName, isHost, joinedAt, avatarId, null, null)
        {
        }

        public LobbyMemberViewModel(
            int userId,
            string userName,
            bool isHost,
            DateTime joinedAt,
            string avatarId,
            string currentSkinId,
            int? currentSkinUnlockedId)
        {
            UserId = userId;
            UserName = userName;
            JoinedAt = joinedAt;
            AvatarId = avatarId;
            _isHost = isHost;
            CurrentSkinId = currentSkinId;
            CurrentSkinUnlockedId = currentSkinUnlockedId;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
