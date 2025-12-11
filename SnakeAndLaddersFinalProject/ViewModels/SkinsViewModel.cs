using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.UserService;
using SnakeAndLaddersFinalProject.Utilities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class SkinsViewModel : INotifyPropertyChanged
    {
        private const string USER_SERVICE_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IUserService";
        private const string SKIN_PREFIX_AVATAR = "A";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(SkinsViewModel));

        private readonly ObservableCollection<AvatarSkinItemViewModel> _avatarOptions
            = new ObservableCollection<AvatarSkinItemViewModel>();

        private AvatarSkinItemViewModel _selectedAvatar;
        private bool _isBusy;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<AvatarSkinItemViewModel> AvatarOptions => _avatarOptions;

        public AvatarSkinItemViewModel SelectedAvatar
        {
            get => _selectedAvatar;
            set
            {
                if (_selectedAvatar == value)
                {
                    return;
                }

                _selectedAvatar = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedDisplayName));
                OnPropertyChanged(nameof(SelectedStatusText));
            }
        }

        public string SelectedDisplayName => SelectedAvatar?.DisplayName ?? string.Empty;

        public string SelectedStatusText
        {
            get
            {
                if (SelectedAvatar == null)
                {
                    return string.Empty;
                }

                if (!SelectedAvatar.IsUnlocked)
                {
                    return Lang.SkinsStatusLockedText;
                }

                return SelectedAvatar.IsCurrent
                    ? Lang.SkinsStatusCurrentText
                    : Lang.SkinsStatusUnlockedText;
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadAsync()
        {
            if (SessionContext.Current == null || !SessionContext.Current.IsAuthenticated)
            {
                _logger.Warn("SkinsViewModel.LoadAsync: user not authenticated.");
                return;
            }

            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                int userId = SessionContext.Current.UserId;
                AvatarProfileOptionsDto options = await client.GetAvatarOptionsAsync(userId);

                if (options == null || options.Avatars == null)
                {
                    _logger.Warn("GetAvatarOptions returned null or empty list.");
                    return;
                }

                _avatarOptions.Clear();

                foreach (AvatarProfileOptionDto option in options.Avatars)
                {
                    string skinCode = option.AvatarCode ?? string.Empty;

                    if (skinCode.StartsWith(SKIN_PREFIX_AVATAR, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string imagePath = SkinAssetHelper.GetSkinPathFromSkinId(skinCode);

                    var item = new AvatarSkinItemViewModel
                    {
                        AvatarCode = skinCode,
                        DisplayName = option.DisplayName ?? skinCode,
                        IsUnlocked = option.IsUnlocked,
                        IsCurrent = option.IsCurrent,
                        ImagePath = imagePath
                    };

                    _avatarOptions.Add(item);
                }

                var current = _avatarOptions.FirstOrDefault(a => a.IsCurrent);

                if (current == null)
                {
                    current = _avatarOptions.FirstOrDefault(a => a.IsUnlocked) ??
                        _avatarOptions.FirstOrDefault();
                }

                SelectedAvatar = current;
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(ex, "SkinsViewModel.LoadAsync",
                    _logger);
                MessageBox.Show(userMessage, Lang.errorTitle, MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SafeClose(client);
                IsBusy = false;
            }
        }

        public void SelectNext()
        {
            if (_avatarOptions.Count == 0 || SelectedAvatar == null)
            {
                return;
            }

            int currentIndex = _avatarOptions.IndexOf(SelectedAvatar);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = (currentIndex + 1) % _avatarOptions.Count;
            SelectedAvatar = _avatarOptions[nextIndex];
        }

        public void SelectPrevious()
        {
            if (_avatarOptions.Count == 0 || SelectedAvatar == null)
            {
                return;
            }

            int currentIndex = _avatarOptions.IndexOf(SelectedAvatar);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int previousIndex = currentIndex - 1;
            if (previousIndex < 0)
            {
                previousIndex = _avatarOptions.Count - 1;
            }

            SelectedAvatar = _avatarOptions[previousIndex];
        }

        public void SelectAvatarFromTile(object dataContext)
        {
            if (dataContext is AvatarSkinItemViewModel item)
            {
                SelectedAvatar = item;
            }
        }

        public async Task ApplySelectionAsync()
        {
            if (SessionContext.Current == null || !SessionContext.Current.IsAuthenticated)
            {
                MessageBox.Show(Lang.UiShopRequiresLogin, Lang.UiTitleInfo, MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (SelectedAvatar == null)
            {
                MessageBox.Show(Lang.SkinsSelectAvatarWarn, Lang.UiTitleInfo, MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!SelectedAvatar.IsUnlocked)
            {
                MessageBox.Show(Lang.SkinsAvatarLockedWarn, Lang.SkinsLockedTitle, MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                var request = new AvatarSelectionRequestDto
                {
                    UserId = SessionContext.Current.UserId,
                    AvatarCode = SelectedAvatar.AvatarCode
                };

                AccountDto result = await client.SelectAvatarForProfileAsync(request).ConfigureAwait(false);

                if (result != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SessionContext.Current.ProfilePhotoId = result.ProfilePhotoId;
                        SessionContext.Current.CurrentSkinId = result.CurrentSkinId;
                        SessionContext.Current.CurrentSkinUnlockedId = result.CurrentSkinUnlockedId;
                        SessionContext.Current.Coins = result.Coins;
                    });

                    await Application.Current.Dispatcher.InvokeAsync(async () => await LoadAsync());

                    MessageBox.Show(Lang.SkinsAvatarUpdatedSuccess, Lang.SkinsUpdatedTitle, MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(ex, "SkinsViewModel.ApplySelectionAsync", _logger);
                MessageBox.Show(userMessage, Lang.errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SafeClose(client);
                IsBusy = false;
            }
        }

        private static void SafeClose(UserServiceClient client)
        {
            if (client == null)
            {
                return;
            }

            try
            {
                if (client.State == CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    client.Close();
                }
            }
            catch
            {
                client.Abort();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
