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

        // Clave normalizada de skin (002, 003, etc.).
        public string SkinKey => SkinAssetHelper.NormalizeSkinKey(CurrentSkinId);

        // Claves lógicas.
        public string TokenKey => SkinAssetHelper.ResolveAssets(CurrentSkinId).TokenKey;
        public string IdleKey => SkinAssetHelper.ResolveAssets(CurrentSkinId).IdleKey;
        public string SadKey => SkinAssetHelper.ResolveAssets(CurrentSkinId).SadKey;

        // Imagen grande de la skin.
        public string SkinImagePath => SkinAssetHelper.GetSkinPathFromSkinId(CurrentSkinId);

        // Imagen del token en tablero.
        public string TokenImagePath => SkinAssetHelper.GetTokenPathFromSkinId(CurrentSkinId);

        private bool isHost;
        private bool isLocalPlayer;
        private bool isCurrentTurn;

        // Efectos de estado
        private bool hasShield;
        private int remainingShieldTurns;
        private bool isFrozen;
        private int remainingFrozenTurns;

        private string effectsText;

        public string EffectsText
        {
            get { return effectsText; }
            set
            {
                if (string.Equals(effectsText, value, StringComparison.Ordinal))
                {
                    return;
                }

                effectsText = value;
                OnPropertyChanged();
            }
        }

        public bool IsCurrentTurn
        {
            get { return isCurrentTurn; }
            set
            {
                if (isCurrentTurn == value)
                {
                    return;
                }

                isCurrentTurn = value;
                OnPropertyChanged();
            }
        }

        public bool IsLocalPlayer
        {
            get { return isLocalPlayer; }
            set
            {
                if (isLocalPlayer == value)
                {
                    return;
                }

                isLocalPlayer = value;
                OnPropertyChanged();
            }
        }

        public bool IsHost
        {
            get { return isHost; }
            set
            {
                if (isHost == value)
                {
                    return;
                }

                isHost = value;
                OnPropertyChanged();
            }
        }

        // ---- Nuevas propiedades para efectos ----

        public bool HasShield
        {
            get { return hasShield; }
            set
            {
                if (hasShield == value)
                {
                    return;
                }

                hasShield = value;
                OnPropertyChanged();
            }
        }

        public int RemainingShieldTurns
        {
            get { return remainingShieldTurns; }
            set
            {
                if (remainingShieldTurns == value)
                {
                    return;
                }

                remainingShieldTurns = value;
                OnPropertyChanged();
            }
        }

        public bool IsFrozen
        {
            get { return isFrozen; }
            set
            {
                if (isFrozen == value)
                {
                    return;
                }

                isFrozen = value;
                OnPropertyChanged();
            }
        }

        public int RemainingFrozenTurns
        {
            get { return remainingFrozenTurns; }
            set
            {
                if (remainingFrozenTurns == value)
                {
                    return;
                }

                remainingFrozenTurns = value;
                OnPropertyChanged();
            }
        }

        // ---- Constructores ----

        public LobbyMemberViewModel(int userId, string userName, bool isHost, DateTime joinedAt)
        {
            UserId = userId;
            UserName = userName;
            JoinedAt = joinedAt;
            this.isHost = isHost;
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
            this.isHost = isHost;
            CurrentSkinId = currentSkinId;
            CurrentSkinUnlockedId = currentSkinUnlockedId;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
