using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.UserService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ProfileViewModel
    {
        private const string USER_SERVICE_ENDPOINT_CONFIGURATION_NAME = "NetTcpBinding_IUserService";

        private const int MAX_FIRST_NAME_LENGTH = 100;
        private const int MAX_LAST_NAME_LENGTH = 255;
        private const int MAX_DESCRIPTION_LENGTH = 500;

        private const string NAME_ALLOWED_PATTERN = @"^[\p{L}\p{M} .,'\-]*$";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(ProfileViewModel));

        public AccountDto LoadedAccount { get; private set; }

        public string AvatarId { get; private set; }

        public bool HasAvatar
        {
            get { return !string.IsNullOrWhiteSpace(AvatarId); }
        }

        public IList<AvatarProfileOptionViewModel> AvatarOptions { get; private set; }

        public ProfileViewModel()
        {
            AvatarOptions = new List<AvatarProfileOptionViewModel>();
            AvatarId = SessionContext.Current?.ProfilePhotoId;
        }

        public bool LoadProfile()
        {
            var session = SessionContext.Current;

            if (session == null || !session.IsAuthenticated)
            {
                MessageBox.Show(
                    Lang.ProfileGuestNotAllowedText,
                    Lang.ProfileGuestNotAllowedTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return false;
            }

            string userName = session.UserName;

            if (string.IsNullOrWhiteSpace(userName))
            {
                MessageBox.Show(
                    Lang.ProfileUserNameUnavailableText,
                    Lang.UiTitleError,
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
                        Lang.ProfileNotFoundText,
                        Lang.UiTitleInfo,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return false;
                }

                AvatarId = LoadedAccount.ProfilePhotoId;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error loading profile.", ex);
                MessageBox.Show(
                    Lang.ProfileAccountInfoLoadError,
                    Lang.UiTitleError,
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
                    _logger.Error("Error while closing UserServiceClient.", ex);
                }
            }
        }

        public bool LoadAvatarOptions()
        {
            if (LoadedAccount == null)
            {
                return false;
            }

            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                AvatarProfileOptionsDto optionsDto = client.GetAvatarOptions(LoadedAccount.UserId);

                var options = new List<AvatarProfileOptionViewModel>();

                if (optionsDto != null && optionsDto.Avatars != null)
                {
                    foreach (var avatarDto in optionsDto.Avatars)
                    {
                        if (string.IsNullOrWhiteSpace(avatarDto.AvatarCode))
                        {
                            continue;
                        }

                        var option = new AvatarProfileOptionViewModel(
                            avatarDto.AvatarCode,
                            avatarDto.IsUnlocked,
                            avatarDto.IsCurrent);

                        options.Add(option);
                    }
                }

                AvatarOptions = options;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error loading avatar options.", ex);
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
                    _logger.Error("Error while closing UserServiceClient after loading avatar options.", ex);
                }
            }
        }

        public bool ValidateProfileInputs(string firstName, string lastName, string description)
        {
            firstName = firstName ?? string.Empty;
            lastName = lastName ?? string.Empty;
            description = description ?? string.Empty;

            if (string.IsNullOrWhiteSpace(firstName))
            {
                MessageBox.Show(
                    Lang.ProfileFirstNameRequiredText,
                    Lang.UiTitleWarning,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            // ===== Apellido requerido =====
            if (string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show(
                    Lang.ProfileLastNameRequiredText,
                    Lang.UiTitleWarning,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (firstName.Length > MAX_FIRST_NAME_LENGTH)
            {
                MessageBox.Show(
                    string.Format(Lang.ProfileFirstNameTooLongFmt, MAX_FIRST_NAME_LENGTH),
                    Lang.UiTitleWarning,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (!Regex.IsMatch(firstName, NAME_ALLOWED_PATTERN))
            {
                MessageBox.Show(
                    Lang.ProfileFirstNameInvalidCharsText,
                    Lang.UiTitleWarning,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (lastName.Length > MAX_LAST_NAME_LENGTH)
            {
                MessageBox.Show(
                    string.Format(Lang.ProfileLastNameTooLongFmt, MAX_LAST_NAME_LENGTH),
                    Lang.UiTitleWarning,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(lastName, NAME_ALLOWED_PATTERN))
            {
                MessageBox.Show(
                    Lang.ProfileLastNameInvalidCharsText,
                    Lang.UiTitleWarning,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (description.Length > MAX_DESCRIPTION_LENGTH)
            {
                MessageBox.Show(
                    string.Format(Lang.ProfileDescriptionTooLongFmt, MAX_DESCRIPTION_LENGTH),
                    Lang.UiTitleWarning,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (HasControlCharacters(firstName) ||
                HasControlCharacters(lastName) ||
                HasControlCharacters(description))
            {
                MessageBox.Show(
                    Lang.ProfileInvalidCharactersText,
                    Lang.UiTitleWarning,
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
                        Lang.ProfileUpdateErrorText,
                        Lang.UiTitleError,
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
                    Lang.ProfileUpdateSuccessText,
                    Lang.UiTitleInfo,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error updating profile.", ex);
                MessageBox.Show(
                    Lang.ProfileUpdateErrorText,
                    Lang.UiTitleError,
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
                    _logger.Error("Error while closing UserServiceClient after update.", ex);
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
                    Lang.ProfileDeactivateSuccessText,
                    Lang.ProfileDeactivateSuccessTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Application.Current.Shutdown();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error deactivating account.", ex);
                MessageBox.Show(
                    Lang.ProfileDeactivateErrorText,
                    Lang.UiTitleError,
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
                    _logger.Error("Error while closing UserServiceClient after deactivate.", ex);
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
                        Lang.ProfileAvatarUpdateErrorText,
                        Lang.UiTitleError,
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
                    Lang.ProfileAvatarUpdateSuccessText,
                    Lang.UiTitleInfo,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error updating avatar.", ex);
                MessageBox.Show(
                    Lang.ProfileAvatarUpdateErrorText,
                    Lang.UiTitleError,
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
                    _logger.Error("Error while closing UserServiceClient after avatar update.", ex);
                }
            }
        }
    }
}
