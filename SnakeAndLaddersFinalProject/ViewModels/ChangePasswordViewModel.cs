using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Mail;
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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChangePasswordViewModel));

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
        private const int VERIFICATION_CODE_MAX_LENGTH = 6;

        private string email = string.Empty;
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
            get { return email; }
            set
            {
                if (email == value)
                {
                    return;
                }

                email = value ?? string.Empty;
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
            string trimmedEmail = (Email ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(trimmedEmail))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_GENERIC_ERROR));
                return;
            }

            if (trimmedEmail.Length > EMAIL_MAX_LENGTH)
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_INVALID_EMAIL_FORMAT));
                return;
            }

            if (!IsEmailFormatValid(trimmedEmail))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_INVALID_EMAIL_FORMAT));
                return;
            }

            var client = new AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                Logger.Info("Requesting password change verification code (forgot password).");

                AuthResult result = await Task.Run(
                    () => client.RequestPasswordChangeCode(trimmedEmail));

                client.Close();

                if (result == null)
                {
                    Logger.Warn("AuthResult is null in RequestPasswordChangeCode.");
                    ShowError(UI_CHANGE_PASSWORD_GENERIC_ERROR);
                    return;
                }

                if (result.Success)
                {
                    ShowInfo(T(UI_CHANGE_PASSWORD_CODE_SENT));
                    return;
                }

                HandleRequestCodeError(result);
            }
            catch (EndpointNotFoundException ex)
            {
                client.Abort();

                string userMessage = ExceptionHandler.Handle(
                    ex,
                    "ChangePasswordPage.SendCode.EndpointNotFound",
                    Logger);

                MessageBox.Show(
                    userMessage,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                client.Abort();

                string userMessage = ExceptionHandler.Handle(
                    ex,
                    "ChangePasswordPage.SendCode",
                    Logger);

                MessageBox.Show(
                    userMessage,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public async Task ChangePasswordAsync()
        {
            string trimmedEmail = (Email ?? string.Empty).Trim();
            string trimmedCode = (VerificationCode ?? string.Empty).Trim();

            string newPasswordLocal = (NewPassword ?? string.Empty).Trim();
            string confirmPasswordLocal = (ConfirmPassword ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(trimmedEmail)
                || string.IsNullOrWhiteSpace(newPasswordLocal)
                || string.IsNullOrWhiteSpace(confirmPasswordLocal)
                || string.IsNullOrWhiteSpace(trimmedCode))
            {
                ShowError(UI_CHANGE_PASSWORD_GENERIC_ERROR);
                return;
            }

            if (trimmedEmail.Length > EMAIL_MAX_LENGTH)
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_INVALID_EMAIL_FORMAT));
                return;
            }

            if (!IsEmailFormatValid(trimmedEmail))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_INVALID_EMAIL_FORMAT));
                return;
            }

            if (!string.Equals(newPasswordLocal, confirmPasswordLocal, StringComparison.Ordinal))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_PASSWORDS_DO_NOT_MATCH));
                return;
            }

            if (newPasswordLocal.Length > PASSWORD_MAX_LENGTH)
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_WEAK_PASSWORD));
                return;
            }

            if (!IsPasswordStrong(newPasswordLocal))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_WEAK_PASSWORD));
                return;
            }

            if (!IsVerificationCodeValid(trimmedCode))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_CODE_INVALID));
                return;
            }

            var request = new ChangePasswordRequestDto
            {
                Email = trimmedEmail,
                NewPassword = newPasswordLocal,
                VerificationCode = trimmedCode
            };

            var client = new AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                Logger.Info("Sending ChangePassword request (forgot password).");

                AuthResult result = await Task.Run(
                    () => client.ChangePassword(request));

                client.Close();

                if (result == null)
                {
                    Logger.Warn("AuthResult is null in ChangePassword.");
                    ShowError(UI_CHANGE_PASSWORD_GENERIC_ERROR);
                    return;
                }

                if (result.Success)
                {
                    ShowInfo(T(UI_CHANGE_PASSWORD_SUCCESS));
                    ClearFields();
                    PasswordChangedSuccessfully?.Invoke();
                    return;
                }

                HandleChangePasswordError(result);
            }
            catch (EndpointNotFoundException ex)
            {
                client.Abort();

                string userMessage = ExceptionHandler.Handle(
                    ex,
                    "ChangePasswordPage.ChangePassword.EndpointNotFound",
                    Logger);

                MessageBox.Show(
                    userMessage,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                client.Abort();

                string userMessage = ExceptionHandler.Handle(
                    ex,
                    "ChangePasswordPage.ChangePassword",
                    Logger);

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

                Logger.WarnFormat(
                    "Password change code throttled. Wait {0} seconds.",
                    seconds);

                ShowWarn(T(UI_CHANGE_PASSWORD_GENERIC_ERROR));
                return;
            }

            if (string.Equals(code, AUTH_CODE_EMAIL_NOT_FOUND, StringComparison.Ordinal))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_EMAIL_NOT_FOUND));
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
                ShowWarn(T(UI_CHANGE_PASSWORD_REUSED_PASSWORD));
                return;
            }

            if (string.Equals(code, AUTH_CODE_PASSWORD_WEAK, StringComparison.Ordinal))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_WEAK_PASSWORD));
                return;
            }

            if (string.Equals(code, AUTH_CODE_CODE_NOT_REQUESTED, StringComparison.Ordinal))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_CODE_NOT_REQUESTED));
                return;
            }

            if (string.Equals(code, AUTH_CODE_CODE_EXPIRED, StringComparison.Ordinal))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_CODE_EXPIRED));
                return;
            }

            if (string.Equals(code, AUTH_CODE_CODE_INVALID, StringComparison.Ordinal))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_CODE_INVALID));
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

        private static bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            if (password.Length < PASSWORD_MIN_LENGTH || password.Length > PASSWORD_MAX_LENGTH)
            {
                return false;
            }

            bool hasUpper = false;
            bool hasLower = false;
            bool hasSpecial = false;

            foreach (char character in password)
            {
                if (char.IsUpper(character))
                {
                    hasUpper = true;
                    continue;
                }

                if (char.IsLower(character))
                {
                    hasLower = true;
                    continue;
                }

                if (!char.IsLetterOrDigit(character))
                {
                    hasSpecial = true;
                }
            }

            return hasUpper && hasLower && hasSpecial;
        }

        private static bool IsVerificationCodeValid(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            if (code.Length > VERIFICATION_CODE_MAX_LENGTH)
            {
                return false;
            }

            for (int index = 0; index < code.Length; index++)
            {
                if (!char.IsDigit(code[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsEmailFormatValid(string email)
        {
            try
            {
                var address = new MailAddress(email);
                return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string T(string key)
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
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
