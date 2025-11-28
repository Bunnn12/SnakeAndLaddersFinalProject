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
        private const int PASSWORD_MAX_LENGTH = 510;
        private const int USERNAME_MAX_LENGTH = 90;
        private const int NAME_MAX_LENGTH = 90;
        private const int EMAIL_MAX_LENGTH = 200;

        private const string AUTH_CODE_OK = "Auth.Ok";
        private const string AUTH_CODE_EMAIL_REQUIRED = "Auth.EmailRequired";
        private const string AUTH_CODE_EMAIL_ALREADY_EXISTS = "Auth.EmailAlreadyExists";
        private const string AUTH_CODE_USERNAME_ALREADY_EXISTS = "Auth.UserNameAlreadyExists";
        private const string AUTH_CODE_INVALID_CREDENTIALS = "Auth.InvalidCredentials";
        private const string AUTH_CODE_THROTTLE_WAIT = "Auth.ThrottleWait";
        private const string AUTH_CODE_NOT_REQUESTED = "Auth.NotRequested";
        private const string AUTH_CODE_EXPIRED = "Auth.Expired";
        private const string AUTH_CODE_INVALID = "Auth.Invalid";
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

            // Nombre
            if (string.IsNullOrWhiteSpace(input.GivenName))
            {
                errors.Add(T("UiFirstNameRequired"));
            }
            else
            {
                if (input.GivenName.Length > NAME_MAX_LENGTH)
                {
                    errors.Add("El nombre no puede tener más de 90 caracteres.");
                }

                if (!IsLettersOnly(input.GivenName))
                {
                    errors.Add("El nombre sólo puede contener letras.");
                }
            }

            // Apellido
            if (string.IsNullOrWhiteSpace(input.FamilyName))
            {
                errors.Add(T("UiLastNameRequired"));
            }
            else
            {
                if (input.FamilyName.Length > NAME_MAX_LENGTH)
                {
                    errors.Add("El apellido no puede tener más de 90 caracteres.");
                }

                if (!IsLettersOnly(input.FamilyName))
                {
                    errors.Add("El apellido sólo puede contener letras.");
                }
            }

            // Username
            if (string.IsNullOrWhiteSpace(input.Username))
            {
                errors.Add(T("UiUserNameRequired"));
            }
            else if (input.Username.Length > USERNAME_MAX_LENGTH)
            {
                errors.Add("El nombre de usuario no puede tener más de 90 caracteres.");
            }

            // Email
            if (string.IsNullOrWhiteSpace(input.EmailAddress))
            {
                errors.Add(T("UiEmailRequired"));
            }
            else
            {
                if (input.EmailAddress.Length > EMAIL_MAX_LENGTH)
                {
                    errors.Add("El correo electrónico no puede tener más de 200 caracteres.");
                }
                else if (!Regex.IsMatch(input.EmailAddress, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    errors.Add(T("UiEmailInvalid"));
                }
            }

            // Password
            if (string.IsNullOrWhiteSpace(input.PlainPassword))
            {
                errors.Add("La contraseña no puede estar vacía.");
            }
            else
            {
                if (input.PlainPassword.Length > PASSWORD_MAX_LENGTH)
                {
                    errors.Add("La contraseña no puede tener más de 510 caracteres.");
                }

                if (!IsPasswordStrong(input.PlainPassword))
                {
                    errors.Add("LA CONTRASEÑA DEBE TENER MINIMO 8 CARACTERES, 1 LETRA MAYUSCULA Y UNA MINUSCULA Y UN CARACTER ESPECIAL");
                }
            }

            return errors.ToArray();
        }

        private static bool IsLettersOnly(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];

                if (char.IsLetter(character))
                {
                    continue;
                }

                if (char.IsWhiteSpace(character))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            if (password.Length < MIN_PASSWORD_LENGTH || password.Length > PASSWORD_MAX_LENGTH)
            {
                return false;
            }

            bool hasUpper = false;
            bool hasLower = false;
            bool hasSpecial = false;

            for (int index = 0; index < password.Length; index++)
            {
                char character = password[index];

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
                    // Aquí se manejan duplicados de correo / throttling / etc.
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
