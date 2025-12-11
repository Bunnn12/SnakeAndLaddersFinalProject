using System.ComponentModel;

namespace SnakeAndLaddersFinalProject.Authentication
{
    public sealed class SessionContext : INotifyPropertyChanged
    {
        private const int USER_ID_NOT_SET = 0;
        private const int DEFAULT_COINS= 0;


        private static readonly SessionContext _currentSessionContext = new SessionContext();

        public static SessionContext Current => _currentSessionContext;

        public event PropertyChangedEventHandler PropertyChanged;

        private int _userId = USER_ID_NOT_SET;
        private int coins;
        private string _userName = string.Empty;
        private string email = string.Empty;
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
                if (_userId == value)
                {
                    return;
                }

                _userId = value;
                OnPropertyChanged(nameof(UserId));
                OnPropertyChanged(nameof(IsAuthenticated));
            }
        }

        public int Coins
        {
            get => coins;
            set
            {
                if (coins == value)
                {
                    return;
                }

                coins = value;
                OnPropertyChanged(nameof(Coins));
            }
        }

        public string UserName
        {
            get => _userName;
            set
            {
                if (_userName == value)
                {
                    return;
                }

                _userName = value ?? string.Empty;
                OnPropertyChanged(nameof(UserName));
                OnPropertyChanged(nameof(IsAuthenticated));
            }
        }

        public string Email
        {
            get => email;
            set
            {
                if (email == value)
                {
                    return;
                }

                email = value ?? string.Empty;
                OnPropertyChanged(nameof(Email));
            }
        }

        public string AuthToken
        {
            get => _authToken;
            set
            {
                if (_authToken == value)
                {
                    return;
                }

                _authToken = value ?? string.Empty;
                OnPropertyChanged(nameof(AuthToken));
            }
        }

        public string ProfilePhotoId
        {
            get => _profilePhotoId;
            set
            {
                if (_profilePhotoId == value)
                {
                    return;
                }

                _profilePhotoId = value ?? string.Empty;
                OnPropertyChanged(nameof(ProfilePhotoId));
            }
        }

        public string CurrentSkinId
        {
            get => _currentSkinId;
            set
            {
                if (_currentSkinId == value)
                {
                    return;
                }

                _currentSkinId = value ?? string.Empty;
                OnPropertyChanged(nameof(CurrentSkinId));
            }
        }

        public int? CurrentSkinUnlockedId
        {
            get => _currentSkinUnlockedId;
            set
            {
                if (_currentSkinUnlockedId == value)
                {
                    return;
                }

                _currentSkinUnlockedId = value;
                OnPropertyChanged(nameof(CurrentSkinUnlockedId));
            }
        }

        public bool IsAuthenticated => UserId > USER_ID_NOT_SET &&
            !string.IsNullOrWhiteSpace(UserName) &&
            !string.IsNullOrWhiteSpace(AuthToken);

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void Clear()
        {
            UserId = USER_ID_NOT_SET;
            Coins = DEFAULT_COINS;
            UserName = string.Empty;
            Email = string.Empty;
            AuthToken = string.Empty;
            ProfilePhotoId = string.Empty;
            CurrentSkinId = string.Empty;
            CurrentSkinUnlockedId = null;
        }

    }
}
