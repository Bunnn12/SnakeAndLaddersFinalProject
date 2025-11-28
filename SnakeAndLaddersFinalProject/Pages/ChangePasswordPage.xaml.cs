using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.AuthService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Utilities;

//aaaa

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ChangePasswordPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChangePasswordPage));

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
        private const string UI_NO_BACK_PAGE = "UiNoBackPage";

        private const int PASSWORD_MAX_LENGTH = 510;
        private const int PASSWORD_MIN_LENGTH = 8;
        private const int EMAIL_MAX_LENGTH = 200;
        private const int VERIFICATION_CODE_MAX_LENGTH = 6;

        public ChangePasswordPage()
        {
            InitializeComponent();
        }

        private async void BtnSendCode_Click(object sender, RoutedEventArgs e)
        {
            string email = (txtEmail.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_GENERIC_ERROR));
                return;
            }

            if (email.Length > EMAIL_MAX_LENGTH)
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_INVALID_EMAIL_FORMAT));
                return;
            }

            if (!IsEmailFormatValid(email))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_INVALID_EMAIL_FORMAT));
                return;
            }

            var client = new AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                Logger.Info("Requesting password change verification code (forgot password).");

                AuthResult result = await System.Threading.Tasks.Task.Run(
                    () => client.RequestPasswordChangeCode(email));

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
                    $"{nameof(ChangePasswordPage)}.{nameof(BtnSendCode_Click)}.EndpointNotFound",
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
                    $"{nameof(ChangePasswordPage)}.{nameof(BtnSendCode_Click)}",
                    Logger);

                MessageBox.Show(
                    userMessage,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string email = (txtEmail.Text ?? string.Empty).Trim();
            string newPassword = (pwdNewPassword.Password ?? string.Empty).Trim();
            string confirmPassword = (pwdConfirmPassword.Password ?? string.Empty).Trim();
            string verificationCode = (txtVerificationCode.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(newPassword)
                || string.IsNullOrWhiteSpace(confirmPassword)
                || string.IsNullOrWhiteSpace(verificationCode))
            {
                ShowError(UI_CHANGE_PASSWORD_GENERIC_ERROR);
                return;
            }

            if (email.Length > EMAIL_MAX_LENGTH)
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_INVALID_EMAIL_FORMAT));
                return;
            }

            if (!IsEmailFormatValid(email))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_INVALID_EMAIL_FORMAT));
                return;
            }

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_PASSWORDS_DO_NOT_MATCH));
                return;
            }

            if (newPassword.Length > PASSWORD_MAX_LENGTH)
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_WEAK_PASSWORD));
                return;
            }

            if (!IsPasswordStrong(newPassword))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_WEAK_PASSWORD));
                return;
            }

            if (!IsVerificationCodeValid(verificationCode))
            {
                ShowWarn(T(UI_CHANGE_PASSWORD_CODE_INVALID));
                return;
            }

            var request = new ChangePasswordRequestDto
            {
                Email = email,
                NewPassword = newPassword,
                VerificationCode = verificationCode
            };

            var client = new AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                Logger.Info("Sending ChangePassword request (forgot password).");

                AuthResult result = await System.Threading.Tasks.Task.Run(
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
                    NavigateToLogin();
                    return;
                }

                HandleChangePasswordError(result);
            }
            catch (EndpointNotFoundException ex)
            {
                client.Abort();

                string userMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(ChangePasswordPage)}.{nameof(BtnChangePassword_Click)}.EndpointNotFound",
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
                    $"{nameof(ChangePasswordPage)}.{nameof(BtnChangePassword_Click)}",
                    Logger);

                MessageBox.Show(
                    userMessage,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
                return;
            }

            ShowWarn(T(UI_NO_BACK_PAGE));
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

        private void TxtVerificationCode_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsAllDigits(e.Text))
            {
                e.Handled = true;
                return;
            }
        }

        private void TxtVerificationCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            string text = textBox.Text ?? string.Empty;

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string filtered = new string(text.Where(char.IsDigit).ToArray());

            if (filtered.Length > VERIFICATION_CODE_MAX_LENGTH)
            {
                filtered = filtered.Substring(0, VERIFICATION_CODE_MAX_LENGTH);
            }

            if (!string.Equals(text, filtered, StringComparison.Ordinal))
            {
                int caretIndex = textBox.CaretIndex;
                textBox.Text = filtered;
                textBox.CaretIndex = Math.Min(caretIndex, filtered.Length);
            }
        }

        private void TxtVerificationCode_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text, true))
            {
                e.CancelCommand();
                return;
            }

            string pastedText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;

            if (!IsVerificationCodeValid(pastedText))
            {
                e.CancelCommand();
            }
        }

        private static bool IsAllDigits(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (int index = 0; index < text.Length; index++)
            {
                if (!char.IsDigit(text[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private void ClearFields()
        {
            txtEmail.Text = string.Empty;
            pwdNewPassword.Password = string.Empty;
            pwdConfirmPassword.Password = string.Empty;
            txtVerificationCode.Text = string.Empty;
        }

        private void NavigateToLogin()
        {
            Window owner = Window.GetWindow(this) ?? Application.Current?.MainWindow;
            Frame mainFrame = owner?.FindName("MainFrame") as Frame;

            if (mainFrame != null)
            {
                mainFrame.Navigate(new LoginPage());
                return;
            }

            NavigationService?.Navigate(new LoginPage());
        }

        private static string T(string key) =>
            Globalization.LocalizationManager.Current[key];

        private static void ShowWarn(string message) =>
            MessageBox.Show(
                message,
                Globalization.LocalizationManager.Current[UI_TITLE_WARNING],
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

        private static void ShowInfo(string message) =>
            MessageBox.Show(
                message,
                Globalization.LocalizationManager.Current[UI_TITLE_INFO],
                MessageBoxButton.OK,
                MessageBoxImage.Information);

        private static void ShowError(string messageKey)
        {
            string message = Globalization.LocalizationManager.Current[messageKey];

            MessageBox.Show(
                message,
                Globalization.LocalizationManager.Current[UI_TITLE_ERROR],
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
