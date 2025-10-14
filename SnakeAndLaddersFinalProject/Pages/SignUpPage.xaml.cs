using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class SignUpPage : Page
    {
        public SignUpPage()
        {
            InitializeComponent();
        }

        private string[] ValidateRegistration(string userName, string firstName, string lastName, string email, string password)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(firstName)) errors.Add(T("UiFirstNameRequired"));
            if (string.IsNullOrWhiteSpace(lastName)) errors.Add(T("UiLastNameRequired"));
            if (string.IsNullOrWhiteSpace(userName)) errors.Add(T("UiUserNameRequired"));
            if (string.IsNullOrWhiteSpace(email)) errors.Add(T("UiEmailRequired"));
            else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) errors.Add(T("UiEmailInvalid"));
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) errors.Add(T("UiPasswordTooShort"));
            return errors.ToArray();
        }

        private async void BtnSignUp_Click(object sender, RoutedEventArgs e)
        {
            var errors = ValidateRegistration(
                txtUsername.Text, txtNameOfUser.Text, txtLastname.Text, txtEmail.Text, pwdPassword.Password);

            if (errors.Any())
            {
                ShowWarn(string.Join("\n", errors));
                return;
            }

            var dto = new AuthService.RegistrationDto
            {
                UserName = txtUsername.Text.Trim(),
                FirstName = txtNameOfUser.Text.Trim(),
                LastName = txtLastname.Text.Trim(),
                Email = txtEmail.Text.Trim().ToLowerInvariant(),
                Password = pwdPassword.Password
            };

            var client = new AuthService.AuthServiceClient("BasicHttpBinding_IAuthService");
            try
            {
                // 1) Solicitar envío de código (NO registrar aún)
                var send = await Task.Run(() => client.RequestEmailVerification(dto.Email));

                if (!send.Success)
                {
                    ShowWarn(MapAuth(send.Code, send.Meta));
                    client.Close();
                    return;
                }

                ShowInfo(string.Format(T("UiVerificationSentFmt"), dto.Email));
                NavigationService?.Navigate(new EmailVerificationPage(dto));
                client.Close();
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                ShowError(T("UiEndpointNotFound"));
                client.Abort();
            }
            catch (Exception ex)
            {
                ShowError($"{T("UiGenericError")} {ex.Message}");
                client.Abort();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
            else ShowWarn(T("UiNoBackPage"));
        }
        private static string T(string key) =>
            Globalization.LocalizationManager.Current[key];

        private static void ShowWarn(string msg) =>
            MessageBox.Show(msg, T("UiTitleWarning"), MessageBoxButton.OK, MessageBoxImage.Warning);

        private static void ShowInfo(string msg) =>
            MessageBox.Show(msg, T("UiTitleInfo"), MessageBoxButton.OK, MessageBoxImage.Information);

        private static void ShowError(string msg) =>
            MessageBox.Show(msg, T("UiTitleError"), MessageBoxButton.OK, MessageBoxImage.Error);

        private static string MapAuth(string code, Dictionary<string, string> meta)
        {
            var m = meta ?? new Dictionary<string, string>();
            switch (code)
            {
                case "Auth.Ok": return T("AuthOk");
                case "Auth.EmailRequired": return T("AuthEmailRequired");
                case "Auth.EmailAlreadyExists": return T("AuthEmailAlreadyExists");
                case "Auth.UserNameAlreadyExists": return T("AuthUserNameAlreadyExists");
                case "Auth.InvalidCredentials": return T("AuthInvalidCredentials");
                case "Auth.ThrottleWait":
                    return string.Format(T("AuthThrottleWaitFmt"),
                                                        m.TryGetValue("seconds", out var s) ? s : "45");
                case "Auth.CodeNotRequested": return T("AuthCodeNotRequested");
                case "Auth.CodeExpired": return T("AuthCodeExpired");
                case "Auth.CodeInvalid": return T("AuthCodeInvalid");
                case "Auth.EmailSendFailed": return T("AuthEmailSendFailed");
                default: return T("AuthServerError");
            }
        }
    }
}
