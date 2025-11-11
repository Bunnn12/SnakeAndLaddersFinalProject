using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using log4net;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class SignUpPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SignUpPage));
        public SignUpPage()
        {
            InitializeComponent();
        }

        public sealed class RegistrationInput
        {
            public string Username { get; set; }
            public string GivenName { get; set; }
            public string FamilyName { get; set; }
            public string EmailAddress { get; set; }
            public string PlainPassword { get; set; }
        }

        private static RegistrationInput NormalizeParam(RegistrationInput m) => new RegistrationInput
        {
            Username = (m.Username ?? "").Trim(),
            GivenName = (m.GivenName ?? "").Trim(),
            FamilyName = (m.FamilyName ?? "").Trim(),
            EmailAddress = (m.EmailAddress ?? "").Trim().ToLowerInvariant(),
            PlainPassword = m.PlainPassword ?? "",
        };

        private static string[] ValidateRegistration(RegistrationInput m)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(m.GivenName)) errors.Add(T("UiFirstNameRequired"));
            if (string.IsNullOrWhiteSpace(m.FamilyName)) errors.Add(T("UiLastNameRequired"));
            if (string.IsNullOrWhiteSpace(m.Username)) errors.Add(T("UiUserNameRequired"));

            if (string.IsNullOrWhiteSpace(m.EmailAddress)) errors.Add(T("UiEmailRequired"));
            else if (!Regex.IsMatch(m.EmailAddress, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors.Add(T("UiEmailInvalid"));

            if (string.IsNullOrWhiteSpace(m.PlainPassword) || m.PlainPassword.Length < 8)
                errors.Add(T("UiPasswordTooShort"));

            return errors.ToArray();
        }

        private async void SignUp(object sender, RoutedEventArgs e)
        {
            var input = NormalizeParam(new RegistrationInput
            {
                Username = txtUsername.Text,
                GivenName = txtNameOfUser.Text,
                FamilyName = txtLastname.Text,
                EmailAddress = txtEmail.Text,
                PlainPassword = pwdPassword.Password,
            });

            var errors = ValidateRegistration(input);
            if (errors.Any()) { ShowWarn(string.Join("\n", errors)); return; }

            var dto = new AuthService.RegistrationDto
            {
                UserName = input.Username,
                FirstName = input.GivenName,
                LastName = input.FamilyName,
                Email = input.EmailAddress,
                Password = input.PlainPassword,
            };

            var client = new AuthService.AuthServiceClient("BasicHttpBinding_IAuthService");
            try
            {
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
                Logger.Warn("No se ha encontrado el endpoint");
                client.Abort();
            }
            catch (Exception ex)
            {
                ShowError($"{T("UiGenericError")} {ex.Message}");
                client.Abort();
            }
        }


        private void Login(object sender, RoutedEventArgs e)
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
