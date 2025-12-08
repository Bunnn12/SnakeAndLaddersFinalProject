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
        public string SkinKey => SkinAssetHelper.NormalizeSkinKey(CurrentSkinId);
        public string TokenKey => SkinAssetHelper.ResolveSkinAssetsFromKey(CurrentSkinId).TokenKey;
        public string IdleKey => SkinAssetHelper.ResolveSkinAssetsFromKey(CurrentSkinId).IdleKey;
        public string SadKey => SkinAssetHelper.ResolveSkinAssetsFromKey(CurrentSkinId).SadKey;
        public string SkinImagePath => SkinAssetHelper.GetSkinPathFromSkinId(CurrentSkinId);
        public string TokenImagePath => SkinAssetHelper.GetTokenPathFromSkinId(CurrentSkinId);

        private bool _isHost;
        private bool _isLocalPlayer;
        private bool _isCurrentTurn;

        private bool _hasShield;
        private int _remainingShieldTurns;
        private bool _isFrozen;
        private int _remainingFrozenTurns;

        private string _effectsText;

        public string EffectsText
        {
            get { return _effectsText; }
            set
            {
                if (string.Equals(_effectsText, value, StringComparison.Ordinal))
                {
                    return;
                }

                _effectsText = value;
                OnPropertyChanged();
            }
        }

        public bool IsCurrentTurn
        {
            get { return _isCurrentTurn; }
            set
            {
                if (_isCurrentTurn == value)
                {
                    return;
                }

                _isCurrentTurn = value;
                OnPropertyChanged();
            }
        }

        public bool IsLocalPlayer
        {
            get { return _isLocalPlayer; }
            set
            {
                if (_isLocalPlayer == value)
                {
                    return;
                }

                _isLocalPlayer = value;
                OnPropertyChanged();
            }
        }

        public bool IsHost
        {
            get { return _isHost; }
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

        public bool HasShield
        {
            get { return _hasShield; }
            set
            {
                if (_hasShield == value)
                {
                    return;
                }

                _hasShield = value;
                OnPropertyChanged();
            }
        }

        public int RemainingShieldTurns
        {
            get { return _remainingShieldTurns; }
            set
            {
                if (_remainingShieldTurns == value)
                {
                    return;
                }

                _remainingShieldTurns = value;
                OnPropertyChanged();
            }
        }

        public bool IsFrozen
        {
            get { return _isFrozen; }
            set
            {
                if (_isFrozen == value)
                {
                    return;
                }

                _isFrozen = value;
                OnPropertyChanged();
            }
        }

        public int RemainingFrozenTurns
        {
            get { return _remainingFrozenTurns; }
            set
            {
                if (_remainingFrozenTurns == value)
                {
                    return;
                }

                _remainingFrozenTurns = value;
                OnPropertyChanged();
            }
        }

        public LobbyMemberViewModel(int userId, string userName, bool isHost, DateTime joinedAt)
        {
            UserId = userId;
            UserName = userName;
            JoinedAt = joinedAt;
            this._isHost = isHost;
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
