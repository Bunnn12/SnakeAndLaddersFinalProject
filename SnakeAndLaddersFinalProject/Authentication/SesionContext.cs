using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnakeAndLaddersFinalProject.Authentication
{
    public sealed class SessionContext : INotifyPropertyChanged
    {
        private const int UNAUTHENTICATED_USER_ID = 0;
        private const int DEFAULT_COINS = 0;

        private static readonly SessionContext _currentSessionContext = new SessionContext();

        public static SessionContext Current => _currentSessionContext;

        public event PropertyChangedEventHandler PropertyChanged;

        private int _userId = UNAUTHENTICATED_USER_ID;
        private int _coins;
        private string _userName = string.Empty;
        private string _email = string.Empty;
        private string _authToken = string.Empty;
        private string _profilePhotoId = string.Empty;
        private string _currentSkinId = string.Empty;
        private int? _currentSkinUnlockedId;

        private SessionContext()
        {
        }

        public int UserId
        {
            get => _userId;
            set
            {
                if (SetProperty(ref _userId, value))
                {
                    OnPropertyChanged(nameof(IsAuthenticated));
                }
            }
        }

        public int Coins
        {
            get => _coins;
            set => SetProperty(ref _coins, value);
        }

        public string UserName
        {
            get => _userName;
            set
            {
                if (SetProperty(ref _userName, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(IsAuthenticated));
                }
            }
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value ?? string.Empty);
        }

        public string AuthToken
        {
            get => _authToken;
            set => SetProperty(ref _authToken, value ?? string.Empty);
        }

        public string ProfilePhotoId
        {
            get => _profilePhotoId;
            set => SetProperty(ref _profilePhotoId, value ?? string.Empty);
        }

        public string CurrentSkinId
        {
            get => _currentSkinId;
            set => SetProperty(ref _currentSkinId, value ?? string.Empty);
        }

        public int? CurrentSkinUnlockedId
        {
            get => _currentSkinUnlockedId;
            set => SetProperty(ref _currentSkinUnlockedId, value);
        }

        public bool IsAuthenticated =>
            UserId > UNAUTHENTICATED_USER_ID &&
            !string.IsNullOrWhiteSpace(UserName) &&
            !string.IsNullOrWhiteSpace(AuthToken);

        public void Clear()
        {
            UserId = UNAUTHENTICATED_USER_ID;
            Coins = DEFAULT_COINS;
            UserName = string.Empty;
            Email = string.Empty;
            AuthToken = string.Empty;
            ProfilePhotoId = string.Empty;
            CurrentSkinId = string.Empty;
            CurrentSkinUnlockedId = null;
        }

        private bool SetProperty<T>(
            ref T backingField,
            T value,
            [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
            {
                return false;
            }

            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
