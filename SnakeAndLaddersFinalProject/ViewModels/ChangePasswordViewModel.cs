using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using log4net;
using SnakeAndLaddersFinalProject.AuthService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ChangePasswordViewModel : INotifyPropertyChanged
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ChangePasswordViewModel));

        private const string AUTH_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IAuthService";

        private const string AUTH_CODE_INVALID_CREDENTIALS = "Auth.InvalidCredentials";
        private const string AUTH_CODE_PASSWORD_REUSED = "Auth.PasswordReused";
        private const string AUTH_CODE_PASSWORD_WEAK = "Auth.PasswordWeak";
        private const string AUTH_CODE_CODE_NOT_REQUESTED = "Auth.CodeNotRequested";
        private const string AUTH_CODE_CODE_EXPIRED = "Auth.CodeExpired";
        private const string AUTH_CODE_CODE_INVALID = "Auth.CodeInvalid";
        private const string AUTH_CODE_THROTTLE_WAIT = "Auth.ThrottleWait";
        private const string AUTH_CODE_EMAIL_NOT_FOUND = "Auth.EmailNotFound";

        private const string META_KEY_SECONDS = "seconds";

        private const string UI_CHANGE_PASSWORD_GENERIC_ERROR = "UiChangePasswordGenericError";
        private const string UI_CHANGE_PASSWORD_PASSWORDS_DO_NOT_MATCH = "UiChangePasswordPasswordsDoNotMatch";
        private const string UI_CHANGE_PASSWORD_REUSED_PASSWORD = "UiChangePasswordReusedPassword";
        private const string UI_CHANGE_PASSWORD_SUCCESS = "UiChangePasswordSuccess";
        private const string UI_CHANGE_PASSWORD_WEAK_PASSWORD = "UiChangePasswordWeakPassword";
        private const string UI_CHANGE_PASSWORD_CODE_SENT = "UiChangePasswordCodeSent";
        private const string UI_CHANGE_PASSWORD_CODE_NOT_REQUESTED = "UiChangePasswordCodeNotRequested";
        private const string UI_CHANGE_PASSWORD_CODE_EXPIRED = "UiChangePasswordCodeExpired";
        private const string UI_CHANGE_PASSWORD_CODE_INVALID = "UiChangePasswordCodeInvalid";
        private const string UI_CHANGE_PASSWORD_INVALID_EMAIL_FORMAT = "UiChangePasswordInvalidEmailFormat";
        private const string UI_CHANGE_PASSWORD_EMAIL_NOT_FOUND = "UiChangePasswordEmailNotFound";

        private const string UI_TITLE_INFO = "UiTitleInfo";
        private const string UI_TITLE_WARNING = "UiTitleWarning";
        private const string UI_TITLE_ERROR = "UiTitleError";

        private const int PASSWORD_MAX_LENGTH = 510;
        private const int PASSWORD_MIN_LENGTH = 8;
        private const int EMAIL_MAX_LENGTH = 200;
        private const int EMAIL_MIN_LENGTH = 5;
        private const int VERIFICATION_CODE_MAX_LENGTH = 6;
        private const int VERIFICATION_CODE_MIN_LENGTH = 6;

        private string _email = string.Empty;
        private string _verificationCode = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action PasswordChangedSuccessfully;

        public ICommand SendCodeCommand { get; }

        public ICommand ChangePasswordCommand { get; }

        public ChangePasswordViewModel()
        {
            SendCodeCommand = new AsyncCommand(SendCodeAsync);
            ChangePasswordCommand = new AsyncCommand(ChangePasswordAsync);
        }

        public string Email
        {
            get { return _email; }
            set
            {
                if (_email == value)
                {
                    return;
                }

                _email = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string VerificationCode
        {
            get { return _verificationCode; }
            set
            {
                if (_verificationCode == value)
                {
                    return;
                }

                _verificationCode = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string NewPassword
        {
            get { return _newPassword; }
            set
            {
                if (_newPassword == value)
                {
                    return;
                }

                _newPassword = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set
            {
                if (_confirmPassword == value)
                {
                    return;
                }

                _confirmPassword = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public async Task SendCodeAsync()
        {
            string normalizedEmail = InputValidator.Normalize(Email);

            if (!InputValidator.IsRequired(normalizedEmail))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_GENERIC_ERROR));
                return;
            }

            if (!InputValidator.IsLengthInRange(
                    normalizedEmail,
                    EMAIL_MIN_LENGTH,
                    EMAIL_MAX_LENGTH))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_INVALID_EMAIL_FORMAT));
                return;
            }

            if (!InputValidator.IsValidEmail(normalizedEmail))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_INVALID_EMAIL_FORMAT));
                return;
            }

            AuthServiceClient authClient = new AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                _logger.Info("Requesting password change verification code (forgot password).");

                AuthResult result = await Task.Run(
                    () => authClient.RequestPasswordChangeCode(normalizedEmail));

                authClient.Close();

                if (result == null)
                {
                    _logger.Warn("AuthResult is null in RequestPasswordChangeCode.");
                    ShowError(UI_CHANGE_PASSWORD_GENERIC_ERROR);
                    return;
                }

                if (result.Success)
                {
                    ShowInfo(GetLocalizedString(UI_CHANGE_PASSWORD_CODE_SENT));
                    return;
                }

                HandleRequestCodeError(result);
            }
            catch (EndpointNotFoundException ex)
            {
                authClient.Abort();

                string userMessage = ExceptionHandler.Handle(
                    ex,
                    "ChangePasswordPage.SendCode.EndpointNotFound",
                    _logger);

                MessageBox.Show(
                    userMessage,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                authClient.Abort();

                string userMessage = ExceptionHandler.Handle(
                    ex,
                    "ChangePasswordPage.SendCode",
                    _logger);

                MessageBox.Show(
                    userMessage,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public async Task ChangePasswordAsync()
        {
            string normalizedEmail = InputValidator.Normalize(Email);
            string normalizedCode = InputValidator.Normalize(VerificationCode);

            string newPasswordLocal = (NewPassword ?? string.Empty).Trim();
            string confirmPasswordLocal = (ConfirmPassword ?? string.Empty).Trim();

            if (!InputValidator.IsRequired(normalizedEmail)
                || !InputValidator.IsRequired(newPasswordLocal)
                || !InputValidator.IsRequired(confirmPasswordLocal)
                || !InputValidator.IsRequired(normalizedCode))
            {
                ShowError(UI_CHANGE_PASSWORD_GENERIC_ERROR);
                return;
            }

            if (!InputValidator.IsLengthInRange(
                    normalizedEmail,
                    EMAIL_MIN_LENGTH,
                    EMAIL_MAX_LENGTH)
                || !InputValidator.IsValidEmail(normalizedEmail))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_INVALID_EMAIL_FORMAT));
                return;
            }

            if (!string.Equals(newPasswordLocal, confirmPasswordLocal, StringComparison.Ordinal))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_PASSWORDS_DO_NOT_MATCH));
                return;
            }

            if (!InputValidator.IsLengthInRange(
                    newPasswordLocal,
                    PASSWORD_MIN_LENGTH,
                    PASSWORD_MAX_LENGTH))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_WEAK_PASSWORD));
                return;
            }

            if (!InputValidator.IsStrongPassword(
                    newPasswordLocal,
                    PASSWORD_MIN_LENGTH,
                    PASSWORD_MAX_LENGTH))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_WEAK_PASSWORD));
                return;
            }

            if (!InputValidator.IsNumericCode(
                    normalizedCode,
                    VERIFICATION_CODE_MIN_LENGTH,
                    VERIFICATION_CODE_MAX_LENGTH))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_CODE_INVALID));
                return;
            }

            ChangePasswordRequestDto request = new ChangePasswordRequestDto
            {
                Email = normalizedEmail,
                NewPassword = newPasswordLocal,
                VerificationCode = normalizedCode
            };

            AuthServiceClient authClient = new AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                _logger.Info("Sending ChangePassword request (forgot password).");

                AuthResult result = await Task.Run(
                    () => authClient.ChangePassword(request));

                authClient.Close();

                if (result == null)
                {
                    _logger.Warn("AuthResult is null in ChangePassword.");
                    ShowError(UI_CHANGE_PASSWORD_GENERIC_ERROR);
                    return;
                }

                if (result.Success)
                {
                    ShowInfo(GetLocalizedString(UI_CHANGE_PASSWORD_SUCCESS));
                    ClearFields();
                    PasswordChangedSuccessfully?.Invoke();
                    return;
                }

                HandleChangePasswordError(result);
            }
            catch (EndpointNotFoundException ex)
            {
                authClient.Abort();

                string userMessage = ExceptionHandler.Handle(
                    ex,
                    "ChangePasswordPage.ChangePassword.EndpointNotFound",
                    _logger);

                MessageBox.Show(
                    userMessage,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                authClient.Abort();

                string userMessage = ExceptionHandler.Handle(
                    ex,
                    "ChangePasswordPage.ChangePassword",
                    _logger);

                MessageBox.Show(
                    userMessage,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void HandleRequestCodeError(AuthResult result)
        {
            string code = result.Code ?? string.Empty;
            Dictionary<string, string> meta = result.Meta ?? new Dictionary<string, string>();

            if (string.Equals(code, AUTH_CODE_THROTTLE_WAIT, StringComparison.Ordinal))
            {
                int seconds = GetSecondsFromMeta(meta);

                _logger.WarnFormat(
                    "Password change code throttled. Wait {0} seconds.",
                    seconds);

                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_GENERIC_ERROR));
                return;
            }

            if (string.Equals(code, AUTH_CODE_EMAIL_NOT_FOUND, StringComparison.Ordinal))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_EMAIL_NOT_FOUND));
                return;
            }

            ShowError(UI_CHANGE_PASSWORD_GENERIC_ERROR);
        }

        private void HandleChangePasswordError(AuthResult result)
        {
            string code = result.Code ?? string.Empty;

            if (string.Equals(code, AUTH_CODE_INVALID_CREDENTIALS, StringComparison.Ordinal))
            {
                ShowError(UI_CHANGE_PASSWORD_GENERIC_ERROR);
                return;
            }

            if (string.Equals(code, AUTH_CODE_PASSWORD_REUSED, StringComparison.Ordinal))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_REUSED_PASSWORD));
                return;
            }

            if (string.Equals(code, AUTH_CODE_PASSWORD_WEAK, StringComparison.Ordinal))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_WEAK_PASSWORD));
                return;
            }

            if (string.Equals(code, AUTH_CODE_CODE_NOT_REQUESTED, StringComparison.Ordinal))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_CODE_NOT_REQUESTED));
                return;
            }

            if (string.Equals(code, AUTH_CODE_CODE_EXPIRED, StringComparison.Ordinal))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_CODE_EXPIRED));
                return;
            }

            if (string.Equals(code, AUTH_CODE_CODE_INVALID, StringComparison.Ordinal))
            {
                ShowWarn(GetLocalizedString(UI_CHANGE_PASSWORD_CODE_INVALID));
                return;
            }

            ShowError(UI_CHANGE_PASSWORD_GENERIC_ERROR);
        }

        private static int GetSecondsFromMeta(Dictionary<string, string> meta)
        {
            if (meta == null)
            {
                return 0;
            }

            if (!meta.TryGetValue(META_KEY_SECONDS, out string secondsText))
            {
                return 0;
            }

            if (!int.TryParse(secondsText, out int seconds))
            {
                return 0;
            }

            return seconds;
        }

        private static string GetLocalizedString(string key)
        {
            return Globalization.LocalizationManager.Current[key];
        }

        private static void ShowWarn(string message)
        {
            MessageBox.Show(
                message,
                Globalization.LocalizationManager.Current[UI_TITLE_WARNING],
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static void ShowInfo(string message)
        {
            MessageBox.Show(
                message,
                Globalization.LocalizationManager.Current[UI_TITLE_INFO],
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static void ShowError(string messageKey)
        {
            string message = Globalization.LocalizationManager.Current[messageKey];

            MessageBox.Show(
                message,
                Globalization.LocalizationManager.Current[UI_TITLE_ERROR],
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void ClearFields()
        {
            Email = string.Empty;
            VerificationCode = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
