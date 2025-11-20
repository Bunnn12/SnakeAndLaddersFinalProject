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

        private const int MIN_PASSWORD_LENGTH = 8;

        private const string AUTH_CODE_OK = "Auth.Ok";
        private const string AUTH_CODE_EMAIL_REQUIRED = "Auth.EmailRequired";
        private const string AUTH_CODE_EMAIL_ALREADY_EXISTS = "Auth.EmailAlreadyExists";
        private const string AUTH_CODE_USERNAME_ALREADY_EXISTS = "Auth.UserNameAlreadyExists";
        private const string AUTH_CODE_INVALID_CREDENTIALS = "Auth.InvalidCredentials";
        private const string AUTH_CODE_THROTTLE_WAIT = "Auth.ThrottleWait";
        private const string AUTH_CODE_NOT_REQUESTED = "Auth.CodeNotRequested";
        private const string AUTH_CODE_EXPIRED = "Auth.CodeExpired";
        private const string AUTH_CODE_INVALID = "Auth.CodeInvalid";
        private const string AUTH_CODE_EMAIL_SEND_FAILED = "Auth.EmailSendFailed";

        private const string META_KEY_SECONDS = "seconds";

        private const string AUTH_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IAuthService";

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

        private static RegistrationInput NormalizeParam(RegistrationInput input)
        {
            return new RegistrationInput
            {
                Username = (input.Username ?? string.Empty).Trim(),
                GivenName = (input.GivenName ?? string.Empty).Trim(),
                FamilyName = (input.FamilyName ?? string.Empty).Trim(),
                EmailAddress = (input.EmailAddress ?? string.Empty).Trim().ToLowerInvariant(),
                PlainPassword = input.PlainPassword ?? string.Empty
            };
        }

        private static string[] ValidateRegistration(RegistrationInput input)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(input.GivenName))
            {
                errors.Add(T("UiFirstNameRequired"));
            }

            if (string.IsNullOrWhiteSpace(input.FamilyName))
            {
                errors.Add(T("UiLastNameRequired"));
            }

            if (string.IsNullOrWhiteSpace(input.Username))
            {
                errors.Add(T("UiUserNameRequired"));
            }

            if (string.IsNullOrWhiteSpace(input.EmailAddress))
            {
                errors.Add(T("UiEmailRequired"));
            }
            else if (!Regex.IsMatch(input.EmailAddress, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                errors.Add(T("UiEmailInvalid"));
            }

            if (string.IsNullOrWhiteSpace(input.PlainPassword) ||
                input.PlainPassword.Length < MIN_PASSWORD_LENGTH)
            {
                errors.Add(T("UiPasswordTooShort"));
            }

            return errors.ToArray();
        }

        private async void SignUp(object sender, RoutedEventArgs e)
        {
            var input = NormalizeParam(
                new RegistrationInput
                {
                    Username = txtUsername.Text,
                    GivenName = txtNameOfUser.Text,
                    FamilyName = txtLastname.Text,
                    EmailAddress = txtEmail.Text,
                    PlainPassword = pwdPassword.Password
                });

            string[] errors = ValidateRegistration(input);
            if (errors.Any())
            {
                ShowWarn(string.Join("\n", errors));
                return;
            }

            var registrationDto = new AuthService.RegistrationDto
            {
                UserName = input.Username,
                FirstName = input.GivenName,
                LastName = input.FamilyName,
                Email = input.EmailAddress,
                Password = input.PlainPassword
            };

            var authClient = new AuthService.AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                var sendResult = await Task.Run(
                    () => authClient.RequestEmailVerification(registrationDto.Email));

                if (!sendResult.Success)
                {
                    ShowWarn(MapAuth(sendResult.Code, sendResult.Meta));
                    authClient.Close();
                    return;
                }

                ShowInfo(string.Format(T("UiVerificationSentFmt"), registrationDto.Email));
                NavigationService?.Navigate(new EmailVerificationPage(registrationDto));
                authClient.Close();
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                ShowError(T("UiEndpointNotFound"));
                Logger.Warn("No se ha encontrado el endpoint");
                authClient.Abort();
            }
            catch (Exception ex)
            {
                ShowError($"{T("UiGenericError")} {ex.Message}");
                authClient.Abort();
            }
        }

        private void Login(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
            }
            else
            {
                ShowWarn(T("UiNoBackPage"));
            }
        }

        private static string T(string key) =>
            Globalization.LocalizationManager.Current[key];

        private static void ShowWarn(string message) =>
            MessageBox.Show(message, T("UiTitleWarning"), MessageBoxButton.OK, MessageBoxImage.Warning);

        private static void ShowInfo(string message) =>
            MessageBox.Show(message, T("UiTitleInfo"), MessageBoxButton.OK, MessageBoxImage.Information);

        private static void ShowError(string message) =>
            MessageBox.Show(message, T("UiTitleError"), MessageBoxButton.OK, MessageBoxImage.Error);

        private static string MapAuth(string code, Dictionary<string, string> meta)
        {
            var metaDictionary = meta ?? new Dictionary<string, string>();

            switch (code)
            {
                case AUTH_CODE_OK:
                    return T("AuthOk");

                case AUTH_CODE_EMAIL_REQUIRED:
                    return T("AuthEmailRequired");

                case AUTH_CODE_EMAIL_ALREADY_EXISTS:
                    return T("AuthEmailAlreadyExists");

                case AUTH_CODE_USERNAME_ALREADY_EXISTS:
                    return T("AuthUserNameAlreadyExists");

                case AUTH_CODE_INVALID_CREDENTIALS:
                    return T("AuthInvalidCredentials");

                case AUTH_CODE_THROTTLE_WAIT:
                    return string.Format(
                        T("AuthThrottleWaitFmt"),
                        metaDictionary.TryGetValue(META_KEY_SECONDS, out string secondsText)
                            ? secondsText
                            : "45");

                case AUTH_CODE_NOT_REQUESTED:
                    return T("AuthCodeNotRequested");

                case AUTH_CODE_EXPIRED:
                    return T("AuthCodeExpired");

                case AUTH_CODE_INVALID:
                    return T("AuthCodeInvalid");

                case AUTH_CODE_EMAIL_SEND_FAILED:
                    return T("AuthEmailSendFailed");

                default:
                    return T("AuthServerError");
            }
        }
    }
}
