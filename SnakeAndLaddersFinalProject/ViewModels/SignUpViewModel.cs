using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.AuthService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class SignUpViewModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SignUpViewModel));

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
        private const string AUTH_CODE_NOT_REQUESTED = "Auth.CodeNotRequested";
        private const string AUTH_CODE_EXPIRED = "Auth.CodeExpired";
        private const string AUTH_CODE_INVALID = "Auth.CodeInvalid";
        private const string AUTH_CODE_EMAIL_SEND_FAILED = "Auth.EmailSendFailed";

        private const string META_KEY_SECONDS = "seconds";
        private const string DEFAULT_THROTTLE_SECONDS_TEXT = "45";

        private const string AUTH_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IAuthService";

        public sealed class RegistrationInput
        {
            public string Username { get; set; } = string.Empty;

            public string GivenName { get; set; } = string.Empty;

            public string FamilyName { get; set; } = string.Empty;

            public string EmailAddress { get; set; } = string.Empty;

            public string PlainPassword { get; set; } = string.Empty;
        }

        public sealed class RegistrationResult
        {
            public bool IsSuccess { get; set; }

            public bool IsEndpointNotFound { get; set; }

            public bool IsGenericError { get; set; }

            public string Code { get; set; } = string.Empty;

            public Dictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();

            public RegistrationDto Registration { get; set; }
        }

        public async Task<RegistrationResult> SignUpAsync(RegistrationInput rawInput)
        {
            var result = new RegistrationResult();

            if (rawInput == null)
            {
                result.IsGenericError = true;
                result.Code = AUTH_CODE_INVALID_CREDENTIALS;
                return result;
            }

            RegistrationInput input = NormalizeParam(rawInput);

            string[] errors = ValidateRegistration(input);

            if (errors.Any())
            {
                ShowWarn(string.Join("\n", errors));
                result.IsSuccess = false;
                return result;
            }

            var registrationDto = new RegistrationDto
            {
                UserName = input.Username,
                FirstName = input.GivenName,
                LastName = input.FamilyName,
                Email = input.EmailAddress,
                Password = input.PlainPassword
            };

            var authClient = new AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                AuthResult sendResult = await Task
                    .Run(() => authClient.RequestEmailVerification(registrationDto.Email))
                    .ConfigureAwait(true);

                if (!sendResult.Success)
                {
                    ShowWarn(MapAuth(sendResult.Code, sendResult.Meta));
                    authClient.Close();

                    result.IsSuccess = false;
                    result.Code = sendResult.Code ?? string.Empty;
                    result.Meta = sendResult.Meta ?? new Dictionary<string, string>();
                    return result;
                }

                ShowInfo(string.Format(Globalization("UiVerificationSentFmt"), registrationDto.Email));
                authClient.Close();

                result.IsSuccess = true;
                result.Registration = registrationDto;
                result.Code = AUTH_CODE_OK;
                return result;
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                ShowError(Globalization("UiEndpointNotFound"));
                _logger.Warn("No se ha encontrado el endpoint de AuthService.");
                authClient.Abort();

                result.IsEndpointNotFound = true;
                return result;
            }
            catch (Exception ex)
            {
                ShowError($"{Globalization("UiGenericError")} {ex.Message}");
                _logger.Error("Error inesperado al registrar usuario.", ex);
                authClient.Abort();

                result.IsGenericError = true;
                return result;
            }
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
                errors.Add(Globalization("UiFirstNameRequired"));
            }
            else
            {
                if (input.GivenName.Length > NAME_MAX_LENGTH)
                {
                    errors.Add(Globalization("UiFirstNameTooLong"));
                }

                if (!IsLettersOnly(input.GivenName))
                {
                    errors.Add(Globalization("UiFirstNameLettersOnly"));
                }
            }

            if (string.IsNullOrWhiteSpace(input.FamilyName))
            {
                errors.Add(Globalization("UiLastNameRequired"));
            }
            else
            {
                if (input.FamilyName.Length > NAME_MAX_LENGTH)
                {
                    errors.Add(Globalization("UiLastNameTooLong"));
                }

                if (!IsLettersOnly(input.FamilyName))
                {
                    errors.Add(Globalization("UiLastNameLettersOnly"));
                }
            }

            if (string.IsNullOrWhiteSpace(input.Username))
            {
                errors.Add(Globalization("UiUserNameRequired"));
            }
            else if (input.Username.Length > USERNAME_MAX_LENGTH)
            {
                errors.Add(Globalization("UiUserNameTooLong"));
            }

            if (string.IsNullOrWhiteSpace(input.EmailAddress))
            {
                errors.Add(Globalization("UiEmailRequired"));
            }
            else
            {
                if (input.EmailAddress.Length > EMAIL_MAX_LENGTH)
                {
                    errors.Add(Globalization("UiEmailTooLong"));
                }
                else if (!Regex.IsMatch(input.EmailAddress, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    errors.Add(Globalization("UiEmailInvalid"));
                }
            }

            if (string.IsNullOrWhiteSpace(input.PlainPassword))
            {
                errors.Add(Globalization("UiPasswordRequired"));
            }
            else
            {
                if (input.PlainPassword.Length > PASSWORD_MAX_LENGTH)
                {
                    errors.Add(Globalization("UiPasswordTooLong"));
                }

                if (!IsPasswordStrong(input.PlainPassword))
                {
                    errors.Add(string.Format(Globalization("UiPasswordWeak"), MIN_PASSWORD_LENGTH));
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

        private static string Globalization(string key)
        {
            return SnakeAndLaddersFinalProject.Globalization.LocalizationManager.Current[key];
        }

        private static void ShowWarn(string message)
        {
            MessageBox.Show(
                message,
                Globalization("UiTitleWarning"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static void ShowInfo(string message)
        {
            MessageBox.Show(
                message,
                Globalization("UiTitleInfo"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(
                message,
                Globalization("UiTitleError"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static string MapAuth(string code, Dictionary<string, string> meta)
        {
            var metaDictionary = meta ?? new Dictionary<string, string>();

            switch (code)
            {
                case AUTH_CODE_OK:
                    return Globalization("AuthOk");

                case AUTH_CODE_EMAIL_REQUIRED:
                    return Globalization("AuthEmailRequired");

                case AUTH_CODE_EMAIL_ALREADY_EXISTS:
                    return Globalization("AuthEmailAlreadyExists");

                case AUTH_CODE_USERNAME_ALREADY_EXISTS:
                    return Globalization("AuthUserNameAlreadyExists");

                case AUTH_CODE_INVALID_CREDENTIALS:
                    return Globalization("AuthInvalidCredentials");

                case AUTH_CODE_THROTTLE_WAIT:
                    return string.Format(
                        Globalization("AuthThrottleWaitFmt"),
                        metaDictionary.TryGetValue(META_KEY_SECONDS, out string secondsText)
                            ? secondsText
                            : DEFAULT_THROTTLE_SECONDS_TEXT);

                case AUTH_CODE_NOT_REQUESTED:
                    return Globalization("AuthCodeNotRequested");

                case AUTH_CODE_EXPIRED:
                    return Globalization("AuthCodeExpired");

                case AUTH_CODE_INVALID:
                    return Globalization("AuthCodeInvalid");

                case AUTH_CODE_EMAIL_SEND_FAILED:
                    return Globalization("AuthEmailSendFailed");

                default:
                    return Globalization("AuthServerError");
            }
        }
    }
}
