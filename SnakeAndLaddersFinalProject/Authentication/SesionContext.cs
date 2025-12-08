using System.ComponentModel;

namespace SnakeAndLaddersFinalProject.Authentication
{
    public sealed class SessionContext : INotifyPropertyChanged
    {
        private const int USER_ID_NOT_SET = 0;

        private static readonly SessionContext CurrentSessionContext = new SessionContext();

        public static SessionContext Current => CurrentSessionContext;

        public event PropertyChangedEventHandler PropertyChanged;

        private int userId = USER_ID_NOT_SET;
        private int coins;
        private string userName = string.Empty;
        private string email = string.Empty;
        private string authToken = string.Empty;
        private string profilePhotoId = string.Empty;
        private string currentSkinId = string.Empty;
        private int? currentSkinUnlockedId;

        private SessionContext()
        {
        }

        public int UserId
        {
            get => userId;
            set
            {
                if (userId == value)
                {
                    return;
                }

                userId = value;
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
            get => userName;
            set
            {
                if (userName == value)
                {
                    return;
                }

                userName = value ?? string.Empty;
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
            get => authToken;
            set
            {
                if (authToken == value)
                {
                    return;
                }

                authToken = value ?? string.Empty;
                OnPropertyChanged(nameof(AuthToken));
            }
        }

        public string ProfilePhotoId
        {
            get => profilePhotoId;
            set
            {
                if (profilePhotoId == value)
                {
                    return;
                }

                profilePhotoId = value ?? string.Empty;
                OnPropertyChanged(nameof(ProfilePhotoId));
            }
        }

        public string CurrentSkinId
        {
            get => currentSkinId;
            set
            {
                if (currentSkinId == value)
                {
                    return;
                }

                currentSkinId = value ?? string.Empty;
                OnPropertyChanged(nameof(CurrentSkinId));
            }
        }

        public int? CurrentSkinUnlockedId
        {
            get => currentSkinUnlockedId;
            set
            {
                if (currentSkinUnlockedId == value)
                {
                    return;
                }

                currentSkinUnlockedId = value;
                OnPropertyChanged(nameof(CurrentSkinUnlockedId));
            }
        }

        public bool IsAuthenticated =>
            UserId > USER_ID_NOT_SET &&
            !string.IsNullOrWhiteSpace(UserName) &&
            !string.IsNullOrWhiteSpace(AuthToken);

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void Clear()
        {
            UserId = USER_ID_NOT_SET;
            Coins = 0;
            UserName = string.Empty;
            Email = string.Empty;
            AuthToken = string.Empty;
            ProfilePhotoId = string.Empty;
            CurrentSkinId = string.Empty;
            CurrentSkinUnlockedId = null;
        }

    }
}
