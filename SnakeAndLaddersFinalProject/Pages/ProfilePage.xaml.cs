using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.UserService;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ProfilePage : Page
    {
        private const string USER_SERVICE_ENDPOINT_CONFIGURATION_NAME = "NetTcpBinding_IUserService";

        private const int MAX_FIRST_NAME_LENGTH = 100;
        private const int MAX_LAST_NAME_LENGTH = 255;
        private const int MAX_DESCRIPTION_LENGTH = 500;

        private const string NAME_ALLOWED_PATTERN = @"^[\p{L}\p{M}0-9 .,'\-]*$";

        // Rango de IDs de avatares (A0001, A0002, ..., A0020)
        private const string AVATAR_ID_PREFIX = "A";
        private const int AVATAR_FIRST_INDEX = 1;
        private const int AVATAR_LAST_INDEX = 20;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProfilePage));

        private static readonly Brush NORMAL_CARD_BORDER_BRUSH = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
        private static readonly Brush EDIT_CARD_BORDER_BRUSH = new SolidColorBrush(Color.FromRgb(0x42, 0x85, 0xF4));
        private static readonly Brush NORMAL_CARD_BACKGROUND_BRUSH = Brushes.White;
        private static readonly Brush EDIT_CARD_BACKGROUND_BRUSH = new SolidColorBrush(Color.FromRgb(0xF3, 0xF7, 0xFF));

        private AccountDto loadedAccount;

        public string AvatarId { get; private set; }

        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarId);

        public ProfilePage()
        {
            InitializeComponent();

            AvatarId = SessionContext.Current?.ProfilePhotoId;
            DataContext = this;

            InitializeAvatarOptions();
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            LoadProfile();
            SetEditMode(false);
        }

        private void LoadProfile()
        {
            var session = SessionContext.Current;

            if (session == null || !session.IsAuthenticated)
            {
                MessageBox.Show(
                    "Iniciaste sesión como invitado, no puedes acceder al perfil.\n\n" +
                    "Si deseas usar un perfil, crea una cuenta :).",
                    "Perfil no disponible",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            string userName = session.UserName;

            if (string.IsNullOrWhiteSpace(userName))
            {
                MessageBox.Show("Nombre de usuario no disponible.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                loadedAccount = client.GetProfileByUsername(userName);

                if (loadedAccount == null)
                {
                    MessageBox.Show("Perfil no encontrado.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                txtUsername.Text = loadedAccount.Username;
                txtFirstName.Text = loadedAccount.FirstName;
                txtLastName.Text = loadedAccount.LastName;
                txtDescription.Text = loadedAccount.ProfileDescription;
                txtCoins.Text = loadedAccount.Coins.ToString();

                AvatarId = loadedAccount.ProfilePhotoId;
                RefreshAvatarBinding();
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading profile.", ex);
                MessageBox.Show("Error cargando perfil.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                try
                {
                    client.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while closing UserServiceClient.", ex);
                }
            }
        }

        private void SetEditMode(bool enabled)
        {
            txtFirstName.IsReadOnly = !enabled;
            txtLastName.IsReadOnly = !enabled;
            txtDescription.IsReadOnly = !enabled;

            txtCoins.IsReadOnly = true;

            btnGuardar.IsEnabled = enabled;
            btnCancelar.IsEnabled = enabled;

            borderEditBanner.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            borderProfileCard.BorderBrush = enabled ? EDIT_CARD_BORDER_BRUSH : NORMAL_CARD_BORDER_BRUSH;
            borderProfileCard.Background = enabled ? EDIT_CARD_BACKGROUND_BRUSH : NORMAL_CARD_BACKGROUND_BRUSH;
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode(true);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (loadedAccount != null)
            {
                txtFirstName.Text = loadedAccount.FirstName;
                txtLastName.Text = loadedAccount.LastName;
                txtDescription.Text = loadedAccount.ProfileDescription;
                txtCoins.Text = loadedAccount.Coins.ToString();

                AvatarId = loadedAccount.ProfilePhotoId;
                RefreshAvatarBinding();
            }

            SetEditMode(false);
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (loadedAccount == null)
            {
                return;
            }

            if (!ValidateProfileInputs())
            {
                return;
            }

            string firstName = txtFirstName.Text?.Trim();
            string lastName = txtLastName.Text?.Trim();
            string description = txtDescription.Text?.Trim();

            var request = new UpdateProfileRequestDto
            {
                UserId = loadedAccount.UserId,
                FirstName = firstName,
                LastName = lastName,
                ProfileDescription = description,
                ProfilePhotoId = loadedAccount.ProfilePhotoId
            };

            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                var updated = client.UpdateProfile(request);

                if (updated == null)
                {
                    MessageBox.Show("No se pudo actualizar el perfil.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                loadedAccount = updated;

                txtFirstName.Text = loadedAccount.FirstName;
                txtLastName.Text = loadedAccount.LastName;
                txtDescription.Text = loadedAccount.ProfileDescription;
                txtCoins.Text = loadedAccount.Coins.ToString();

                AvatarId = loadedAccount.ProfilePhotoId;
                RefreshAvatarBinding();

                if (SessionContext.Current != null)
                {
                    SessionContext.Current.ProfilePhotoId = loadedAccount.ProfilePhotoId;
                }

                MessageBox.Show("Perfil actualizado.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                SetEditMode(false);
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating profile.", ex);
                MessageBox.Show("Error guardando perfil.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                try
                {
                    client.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while closing UserServiceClient after update.", ex);
                }
            }
        }

        private bool ValidateProfileInputs()
        {
            string firstName = txtFirstName.Text?.Trim() ?? string.Empty;
            string lastName = txtLastName.Text?.Trim() ?? string.Empty;
            string description = txtDescription.Text?.Trim() ?? string.Empty;

            if (firstName.Length > MAX_FIRST_NAME_LENGTH)
            {
                MessageBox.Show(
                    $"El nombre no puede exceder {MAX_FIRST_NAME_LENGTH} caracteres.",
                    "Datos inválidos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);


                return false;
            }

            if (!string.IsNullOrEmpty(firstName) &&
                !Regex.IsMatch(firstName, NAME_ALLOWED_PATTERN))
            {
                MessageBox.Show(
                    "El nombre contiene caracteres no permitidos.",
                    "Datos inválidos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (lastName.Length > MAX_LAST_NAME_LENGTH)
            {
                MessageBox.Show(
                    $"Los apellidos no pueden exceder {MAX_LAST_NAME_LENGTH} caracteres.",
                    "Datos inválidos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrEmpty(lastName) &&
                !Regex.IsMatch(lastName, NAME_ALLOWED_PATTERN))
            {
                MessageBox.Show(
                    "Los apellidos contienen caracteres no permitidos.",
                    "Datos inválidos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (description.Length > MAX_DESCRIPTION_LENGTH)
            {
                MessageBox.Show(
                    $"La descripción no puede exceder {MAX_DESCRIPTION_LENGTH} caracteres.",
                    "Datos inválidos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (HasControlCharacters(firstName) ||
                HasControlCharacters(lastName) ||
                HasControlCharacters(description))
            {
                MessageBox.Show(
                    "El texto contiene caracteres no válidos.",
                    "Datos inválidos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private static bool HasControlCharacters(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (char ch in value)
            {
                if (char.IsControl(ch) && ch != '\r' && ch != '\n' && ch != '\t')
                {
                    return true;
                }
            }

            return false;
        }

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            if (btnMenu.ContextMenu != null)
            {
                btnMenu.ContextMenu.PlacementTarget = btnMenu;
                btnMenu.ContextMenu.IsOpen = true;
            }
        }

        private void MenuEditAccount_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode(true);
        }

        private void MenuDeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            if (loadedAccount == null)
            {
                MessageBox.Show("No se pudo obtener la información de la cuenta.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                "¿Seguro que deseas desactivar tu cuenta?\n" +
                "No podrás usarla nuevamente hasta que un administrador la reactive.",
                "Desactivar cuenta",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                client.DeactivateAccount(loadedAccount.UserId);

                MessageBox.Show(
                    "Tu cuenta ha sido desactivada correctamente. La aplicación se cerrará.",
                    "Cuenta desactivada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.Error("Error deactivating account.", ex);
                MessageBox.Show("Ocurrió un error al desactivar la cuenta.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                try
                {
                    client.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while closing UserServiceClient after deactivate.", ex);
                }
            }
        }

        private void MenuViewStats_Click(object sender, RoutedEventArgs e)
        {
            var statsPage = new ProfileStatsPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(statsPage);
                return;
            }

            var navigationWindow = Application.Current.MainWindow as NavigationWindow;
            if (navigationWindow != null)
            {
                navigationWindow.Navigate(statsPage);
            }
            else
            {
                Application.Current.MainWindow.Content = statsPage;
            }
        }

        private void BtnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            if (borderAvatarPicker.Visibility == Visibility.Visible)
            {
                borderAvatarPicker.Visibility = Visibility.Collapsed;
            }
            else
            {
                borderAvatarPicker.Visibility = Visibility.Visible;
            }
        }

        private void BtnCloseAvatarPicker_Click(object sender, RoutedEventArgs e)
        {
            borderAvatarPicker.Visibility = Visibility.Collapsed;
        }

        private void AvatarItem_Click(object sender, RoutedEventArgs e)
        {
            if (loadedAccount == null)
            {
                MessageBox.Show("No se ha cargado el perfil.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            string avatarId = button.Tag as string;
            if (string.IsNullOrWhiteSpace(avatarId))
            {
                return;
            }

            var result = MessageBox.Show(
                "¿Quieres usar este avatar como foto de perfil?",
                "Cambiar avatar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                var request = new UpdateProfileRequestDto
                {
                    UserId = loadedAccount.UserId,
                    FirstName = loadedAccount.FirstName,
                    LastName = loadedAccount.LastName,
                    ProfileDescription = loadedAccount.ProfileDescription,
                    ProfilePhotoId = avatarId
                };

                var updated = client.UpdateProfile(request);

                if (updated == null)
                {
                    MessageBox.Show("No se pudo actualizar el avatar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                loadedAccount = updated;

                txtFirstName.Text = loadedAccount.FirstName;
                txtLastName.Text = loadedAccount.LastName;
                txtDescription.Text = loadedAccount.ProfileDescription;
                txtCoins.Text = loadedAccount.Coins.ToString();

                AvatarId = loadedAccount.ProfilePhotoId;
                RefreshAvatarBinding();

                if (SessionContext.Current != null)
                {
                    SessionContext.Current.ProfilePhotoId = loadedAccount.ProfilePhotoId;
                }

                borderAvatarPicker.Visibility = Visibility.Collapsed;

                MessageBox.Show("Avatar actualizado.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating avatar.", ex);
                MessageBox.Show("Error al actualizar el avatar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                try
                {
                    client.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while closing UserServiceClient after avatar update.", ex);
                }
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
                return;
            }

            var navigationWindow = Application.Current.MainWindow as NavigationWindow;
            if (navigationWindow != null && navigationWindow.CanGoBack)
            {
                navigationWindow.GoBack();
                return;
            }

            var mainPage = new MainPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(mainPage);
                return;
            }

            if (navigationWindow != null)
            {
                navigationWindow.Navigate(mainPage);
            }
            else
            {
                Application.Current.MainWindow.Content = mainPage;
            }
        }

        private void InitializeAvatarOptions()
        {
            var avatarIds = new List<string>();

            for (int index = AVATAR_FIRST_INDEX; index <= AVATAR_LAST_INDEX; index++)
            {
                string id = string.Format("{0}{1:D4}", AVATAR_ID_PREFIX, index);
                avatarIds.Add(id);
            }

            avatarItemsControl.ItemsSource = avatarIds;
        }

        private void RefreshAvatarBinding()
        {
            var currentDataContext = DataContext;
            DataContext = null;
            DataContext = currentDataContext;
        }
    }
}
