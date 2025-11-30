using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.UserService;
using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ProfileViewModel
    {
        private const string USER_SERVICE_ENDPOINT_CONFIGURATION_NAME = "NetTcpBinding_IUserService";

        private const int MAX_FIRST_NAME_LENGTH = 100;
        private const int MAX_LAST_NAME_LENGTH = 255;
        private const int MAX_DESCRIPTION_LENGTH = 500;

        private const string NAME_ALLOWED_PATTERN = @"^[\p{L}\p{M}0-9 .,'\-]*$";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProfileViewModel));

        public AccountDto LoadedAccount { get; private set; }

        public string AvatarId { get; private set; }

        public bool HasAvatar
        {
            get { return !string.IsNullOrWhiteSpace(AvatarId); }
        }

        public ProfileViewModel()
        {
            AvatarId = SessionContext.Current?.ProfilePhotoId;
        }

        public bool LoadProfile()
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

                return false;
            }

            string userName = session.UserName;

            if (string.IsNullOrWhiteSpace(userName))
            {
                MessageBox.Show(
                    "Nombre de usuario no disponible.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                LoadedAccount = client.GetProfileByUsername(userName);

                if (LoadedAccount == null)
                {
                    MessageBox.Show(
                        "Perfil no encontrado.",
                        "Información",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return false;
                }

                AvatarId = LoadedAccount.ProfilePhotoId;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading profile.", ex);
                MessageBox.Show(
                    "Error cargando perfil.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
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

        public bool ValidateProfileInputs(string firstName, string lastName, string description)
        {
            firstName = firstName ?? string.Empty;
            lastName = lastName ?? string.Empty;
            description = description ?? string.Empty;

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

        public bool TryUpdateProfile(string firstName, string lastName, string description)
        {
            if (LoadedAccount == null)
            {
                return false;
            }

            var request = new UpdateProfileRequestDto
            {
                UserId = LoadedAccount.UserId,
                FirstName = firstName,
                LastName = lastName,
                ProfileDescription = description,
                ProfilePhotoId = LoadedAccount.ProfilePhotoId
            };

            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                var updated = client.UpdateProfile(request);

                if (updated == null)
                {
                    MessageBox.Show(
                        "No se pudo actualizar el perfil.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }

                LoadedAccount = updated;
                AvatarId = LoadedAccount.ProfilePhotoId;

                if (SessionContext.Current != null)
                {
                    SessionContext.Current.ProfilePhotoId = LoadedAccount.ProfilePhotoId;
                }

                MessageBox.Show(
                    "Perfil actualizado.",
                    "Información",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating profile.", ex);
                MessageBox.Show(
                    "Error guardando perfil.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
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

        public bool TryDeactivateAccount()
        {
            if (LoadedAccount == null)
            {
                return false;
            }

            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                client.DeactivateAccount(LoadedAccount.UserId);

                MessageBox.Show(
                    "Tu cuenta ha sido desactivada correctamente. La aplicación se cerrará.",
                    "Cuenta desactivada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Application.Current.Shutdown();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error deactivating account.", ex);
                MessageBox.Show(
                    "Ocurrió un error al desactivar la cuenta.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
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

        public bool TryUpdateAvatar(string avatarId)
        {
            if (LoadedAccount == null)
            {
                return false;
            }

            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                var request = new UpdateProfileRequestDto
                {
                    UserId = LoadedAccount.UserId,
                    FirstName = LoadedAccount.FirstName,
                    LastName = LoadedAccount.LastName,
                    ProfileDescription = LoadedAccount.ProfileDescription,
                    ProfilePhotoId = avatarId
                };

                var updated = client.UpdateProfile(request);

                if (updated == null)
                {
                    MessageBox.Show(
                        "No se pudo actualizar el avatar.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }

                LoadedAccount = updated;
                AvatarId = LoadedAccount.ProfilePhotoId;

                if (SessionContext.Current != null)
                {
                    SessionContext.Current.ProfilePhotoId = LoadedAccount.ProfilePhotoId;
                }

                MessageBox.Show(
                    "Avatar actualizado.",
                    "Información",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating avatar.", ex);
                MessageBox.Show(
                    "Error al actualizar el avatar.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
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
    }
}
