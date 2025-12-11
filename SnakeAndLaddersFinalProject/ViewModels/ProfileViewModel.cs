using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.UserService;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ProfileViewModel
    {
        private const string USER_SERVICE_ENDPOINT_CONFIGURATION_NAME =
            "NetTcpBinding_IUserService";

        private const int MAX_FIRST_NAME_LENGTH = 100;
        private const int MAX_LAST_NAME_LENGTH = 255;
        private const int MAX_DESCRIPTION_LENGTH = 510; 

        private const int MIN_FIRST_NAME_LENGTH = 1;
        private const int MIN_LAST_NAME_LENGTH = 1;
        private const int MIN_DESCRIPTION_LENGTH = 0;

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
                    _logger.Error("Error while closing UserServiceClient after loading avatar options.",
                        ex);
                }
            }
        }

        public static bool ValidateProfileInputs(string firstName, string lastName, string description)
        {
            string normalizedFirstName = InputValidator.Normalize(firstName);
            string normalizedLastName = InputValidator.Normalize(lastName);
            string normalizedDescription = InputValidator.Normalize(description);

            if (!InputValidator.IsRequired(normalizedFirstName))
            {
                ShowWarn(Lang.ProfileFirstNameRequiredText);
                return false;
            }

            if (!InputValidator.IsRequired(normalizedLastName))
            {
                ShowWarn(Lang.ProfileLastNameRequiredText);
                return false;
            }

            if (!InputValidator.IsLengthInRange(normalizedFirstName, MIN_FIRST_NAME_LENGTH,
                MAX_FIRST_NAME_LENGTH))
            {
                ShowWarn(string.Format(Lang.ProfileFirstNameTooLongFmt, MAX_FIRST_NAME_LENGTH));
                return false;
            }

            if (!IsAllLettersOrUnicode(normalizedFirstName))
            {
                ShowWarn(Lang.ProfileFirstNameInvalidCharsText);
                return false;
            }

            if (!InputValidator.IsLengthInRange(normalizedLastName, MIN_LAST_NAME_LENGTH,
                MAX_LAST_NAME_LENGTH))
            {
                ShowWarn(string.Format(Lang.ProfileLastNameTooLongFmt, MAX_LAST_NAME_LENGTH));
                return false;
            }

            if (!IsAllLettersOrUnicode(normalizedLastName))
            {
                ShowWarn(Lang.ProfileLastNameInvalidCharsText);
                return false;
            }

            if (!string.IsNullOrEmpty(normalizedDescription))
            {
                if (!InputValidator.IsLengthInRange(normalizedDescription,
                    MIN_DESCRIPTION_LENGTH,
                    MAX_DESCRIPTION_LENGTH))
                {
                    ShowWarn(string.Format(Lang.ProfileDescriptionTooLongFmt,
                        MAX_DESCRIPTION_LENGTH));
                    return false;
                }

                if (!InputValidator.IsSafeText(normalizedDescription, MIN_DESCRIPTION_LENGTH,
                    MAX_DESCRIPTION_LENGTH, allowNewLines: true))
                {
                    ShowWarn(Lang.ProfileInvalidCharactersText);
                    return false;
                }
            }

            return true;
        }

        private static bool IsAllLettersOrUnicode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            return text.All(c => char.IsLetter(c) || char.IsWhiteSpace(c));
        }

        public bool TryUpdateProfile(string firstName, string lastName, string description)
        {
            if (LoadedAccount == null)
            {
                return false;
            }

            string normalizedFirstName = InputValidator.Normalize(firstName);
            string normalizedLastName = InputValidator.Normalize(lastName);
            string normalizedDescription = InputValidator.Normalize(description);

            var request = new UpdateProfileRequestDto
            {
                UserId = LoadedAccount.UserId,
                FirstName = normalizedFirstName,
                LastName = normalizedLastName,
                ProfileDescription = normalizedDescription,
                ProfilePhotoId = LoadedAccount.ProfilePhotoId
            };

            var client = new UserServiceClient(USER_SERVICE_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                var updated = client.UpdateProfile(request);

                if (updated == null)
                {
                    ShowError(Lang.ProfileUpdateErrorText);
                    return false;
                }

                LoadedAccount = updated;
                AvatarId = LoadedAccount.ProfilePhotoId;

                if (SessionContext.Current != null)
                {
                    SessionContext.Current.ProfilePhotoId = LoadedAccount.ProfilePhotoId;
                }

                ShowInfo(Lang.ProfileUpdateSuccessText);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error updating profile.", ex);
                ShowError(Lang.ProfileUpdateErrorText);
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
                ShowInfo(Lang.ProfileDeactivateSuccessText);
                Application.Current.Shutdown();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error deactivating account.", ex);
                ShowError(Lang.ProfileDeactivateErrorText);
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
                    ShowError(Lang.ProfileAvatarUpdateErrorText);
                    return false;
                }

                LoadedAccount = updated;
                AvatarId = LoadedAccount.ProfilePhotoId;

                if (SessionContext.Current != null)
                {
                    SessionContext.Current.ProfilePhotoId = LoadedAccount.ProfilePhotoId;
                }

                ShowInfo(Lang.ProfileAvatarUpdateSuccessText);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error updating avatar.", ex);
                ShowError(Lang.ProfileAvatarUpdateErrorText);
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
                    _logger.Error("Error while closing UserServiceClient after avatar update.",
                        ex);
                }
            }
        }

        private static void ShowWarn(string message)
        {
            MessageBox.Show(
                message,
                Lang.UiTitleWarning,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static void ShowInfo(string message)
        {
            MessageBox.Show(
                message,
                Lang.UiTitleInfo,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(
                message,
                Lang.UiTitleError,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
