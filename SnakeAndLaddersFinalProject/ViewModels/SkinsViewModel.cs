using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.UserService;
using SnakeAndLaddersFinalProject.Utilities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class SkinsViewModel : INotifyPropertyChanged
    {
        private const string USER_SERVICE_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IUserService";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(SkinsViewModel));

        private readonly ObservableCollection<AvatarSkinItemViewModel> _avatarOptions =
            new ObservableCollection<AvatarSkinItemViewModel>();

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

        public string SelectedDisplayName =>
            SelectedAvatar?.DisplayName ?? string.Empty;

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
                    return "Bloqueado. Consíguelo en la tienda.";
                }

                return SelectedAvatar.IsCurrent
                    ? "Actualmente seleccionado."
                    : "Desbloqueado. Haz clic en Aplicar para usarlo.";
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
            if (!SessionContext.Current.IsAuthenticated)
            {
                _logger.Warn("SkinsViewModel.LoadAsync: usuario no autenticado.");
                return;
            }

            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            try
            {
                int userId = SessionContext.Current.UserId;

                using (UserServiceClient client =
                    new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME))
                {
                    AvatarProfileOptionsDto options =
                        await client.GetAvatarOptionsAsync(userId);

                    if (options == null || options.Avatars == null)
                    {
                        _logger.Warn("GetAvatarOptions devolvió null o sin lista.");
                        return;
                    }

                    _avatarOptions.Clear();

                    foreach (AvatarProfileOptionDto option in options.Avatars)
                    {
                        string skinCode = option.AvatarCode ?? string.Empty;

                        // Saltar los avatares default tipo A0001, A0007, etc.
                        if (skinCode.StartsWith("A", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string imagePath = SkinAssetHelper.GetSkinPathFromSkinId(skinCode);

                        AvatarSkinItemViewModel item = new AvatarSkinItemViewModel
                        {
                            AvatarCode = skinCode,
                            DisplayName = option.DisplayName ?? skinCode,
                            IsUnlocked = option.IsUnlocked,
                            IsCurrent = option.IsCurrent,
                            ImagePath = imagePath
                        };

                        _avatarOptions.Add(item);
                    }

                    AvatarSkinItemViewModel current =
                        _avatarOptions.FirstOrDefault(a => a.IsCurrent);

                    if (current == null)
                    {
                        current = _avatarOptions.FirstOrDefault(a => a.IsUnlocked)
                                  ?? _avatarOptions.FirstOrDefault();
                    }

                    SelectedAvatar = current;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error al cargar las opciones de avatar.", ex);

                _ = MessageBox.Show(
                    "Ocurrió un problema al cargar las skins. Intenta de nuevo.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
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
            AvatarSkinItemViewModel item = dataContext as AvatarSkinItemViewModel;

            if (item == null)
            {
                return;
            }

            SelectedAvatar = item;
        }

        public async Task ApplySelectionAsync()
        {
            if (!SessionContext.Current.IsAuthenticated)
            {
                MessageBox.Show(
                    "Debes iniciar sesión para cambiar tu avatar.",
                    "Información",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (SelectedAvatar == null)
            {
                MessageBox.Show(
                    "Selecciona primero un avatar.",
                    "Información",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!SelectedAvatar.IsUnlocked)
            {
                MessageBox.Show(
                    "Este avatar está bloqueado. Desbloquéalo en la tienda para poder usarlo.",
                    "Avatar bloqueado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            try
            {
                AvatarSelectionRequestDto request = new AvatarSelectionRequestDto
                {
                    UserId = SessionContext.Current.UserId,
                    AvatarCode = SelectedAvatar.AvatarCode
                };

                using (UserServiceClient client =
                    new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME))
                {
                    AccountDto result =
                        await client.SelectAvatarForProfileAsync(request).ConfigureAwait(false);

                    if (result != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            SessionContext.Current.ProfilePhotoId = result.ProfilePhotoId;
                            SessionContext.Current.CurrentSkinId = result.CurrentSkinId;
                            SessionContext.Current.CurrentSkinUnlockedId = result.CurrentSkinUnlockedId;
                            SessionContext.Current.Coins = result.Coins;
                        });
                    }
                }

                await LoadAsync().ConfigureAwait(false);

                MessageBox.Show(
                    "Avatar actualizado correctamente.",
                    "Avatar actualizado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error("Error al aplicar la selección de avatar.", ex);
                MessageBox.Show(
                    "No se pudo cambiar el avatar. Intenta de nuevo.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
