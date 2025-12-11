using log4net;
using SnakeAndLaddersFinalProject.AuthService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class EmailVerificationViewModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(
            typeof(EmailVerificationViewModel));

        private const int DEFAULT_RESEND_COOLDOWN_SECONDS = 45;
        private const int VERIFICATION_CODE_LENGTH = 6;

        private const string AUTH_ENDPOINT_CONFIGURATION_NAME =
            "BasicHttpBinding_IAuthService";

        private const string AUTH_CODE_OK = "Auth.Ok";
        private const string AUTH_CODE_EMAIL_REQUIRED = "Auth.EmailRequired";
        private const string AUTH_CODE_EMAIL_ALREADY_EXISTS = "Auth.EmailAlreadyExists";
        private const string AUTH_CODE_USERNAME_ALREADY_EXISTS = "Auth.UserNameAlreadyExists";
        private const string AUTH_CODE_INVALID_CREDENTIALS = "Auth.InvalidCredentials";
        private const string AUTH_CODE_THROTTLE_WAIT = "Auth.ThrottleWait";
        private const string AUTH_CODE_EXPIRED = "Auth.CodeExpired";
        private const string AUTH_CODE_INVALID = "Auth.CodeInvalid";
        private const string AUTH_CODE_EMAIL_SEND_FAILED = "Auth.EmailSendFailed";

        private const string META_KEY_SECONDS = "seconds";

        private const string KEY_AUTH_THROTTLE_WAIT_FMT = "AuthThrottleWaitFmt";
        private const string KEY_UI_VERIFICATION_CODE_REQUIRED = "UiVerificationCodeRequired";
        private const string KEY_UI_ACCOUNT_CREATED_FMT = "UiAccountCreatedFmt";
        private const string KEY_UI_VERIFICATION_SENT_FMT = "UiVerificationSentFmt";

        private readonly RegistrationDto _pendingDto;

        public event Action<int> ResendCooldownRequested;
        public event Action NavigateToLoginRequested;

        public EmailVerificationViewModel(RegistrationDto pendingDto)
        {
            _pendingDto = pendingDto ?? throw new ArgumentNullException(nameof(pendingDto));
        }

        public async Task VerificateCodeAsync(string code)
        {
            string trimmedCode = (code ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(trimmedCode))
            {
                ShowWarn(T(KEY_UI_VERIFICATION_CODE_REQUIRED));
                return;
            }

            if (!IsNumericCode(trimmedCode) || trimmedCode.Length != VERIFICATION_CODE_LENGTH)
            {
                ShowWarn(MapAuth(AUTH_CODE_INVALID, null));
                return;
            }

            var client = new AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                string normalizedEmail = (_pendingDto.Email ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant();

                AuthResult confirm = await Task.Run(
                    () => client.ConfirmEmailVerification(normalizedEmail, trimmedCode));

                if (!confirm.Success)
                {
                    ShowWarn(MapAuth(confirm.Code, confirm.Meta));
                    client.Close();
                    return;
                }

                _pendingDto.Email = normalizedEmail;

                AuthResult register = await Task.Run(
                    () => client.Register(_pendingDto));

                if (!register.Success)
                {
                    ShowWarn(MapAuth(register.Code, register.Meta));
                    client.Close();
                    return;
                }

                ShowInfo(string.Format(T(KEY_UI_ACCOUNT_CREATED_FMT), register.DisplayName));

                client.Close();

                NavigateToLoginRequested?.Invoke();
            }
            catch (Exception ex)
            {
                UiExceptionHelper.ShowModuleError(
                    ex,
                    nameof(VerificateCodeAsync),
                    _logger,
                    Lang.UiEmailVerificationError);

                SafeAbort(client, nameof(VerificateCodeAsync), _logger);
            }
        }

        public async Task ResendCodeAsync()
        {
            string email = (_pendingDto.Email ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            var client = new AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                AuthResult result = await Task.Run(
                    () => client.RequestEmailVerification(email));

                client.Close();

                if (result.Success)
                {
                    ShowInfo(string.Format(T(KEY_UI_VERIFICATION_SENT_FMT), email));
                    ResendCooldownRequested?.Invoke(DEFAULT_RESEND_COOLDOWN_SECONDS);
                    return;
                }

                if (result.Code == AUTH_CODE_THROTTLE_WAIT)
                {
                    int seconds = DEFAULT_RESEND_COOLDOWN_SECONDS;

                    if (result.Meta != null
                        && result.Meta.TryGetValue(META_KEY_SECONDS, out string secondsText)
                        && int.TryParse(secondsText, out int parsedSeconds))
                    {
                        seconds = parsedSeconds;
                    }

                    ShowWarn(string.Format(T(KEY_AUTH_THROTTLE_WAIT_FMT), seconds));
                    ResendCooldownRequested?.Invoke(seconds);
                    return;
                }

                ShowWarn(MapAuth(result.Code, result.Meta));
            }
            catch (Exception ex)
            {
                UiExceptionHelper.ShowModuleError(
                    ex,
                    nameof(ResendCodeAsync),
                    _logger,
                    Lang.UiEmailVerificationResendError);

                SafeAbort(client, nameof(ResendCodeAsync), _logger);
            }
        }

        private static void SafeAbort(AuthServiceClient client, string operationName, ILog logger)
        {
            if (client == null)
            {
                return;
            }

            try
            {
                client.Abort();
            }
            catch (Exception ex)
            {
                logger.Warn(
                    $"Error while aborting AuthServiceClient in '{operationName}'.",
                    ex);
            }
        }

        private static bool IsNumericCode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int index = 0; index < value.Length; index++)
            {
                if (!char.IsDigit(value[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string T(string key)
        {
            return Globalization.LocalizationManager.Current[key];
        }

        private static void ShowWarn(string message)
        {
            MessageBox.Show(
                message,
                T("UiTitleWarning"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static void ShowInfo(string message)
        {
            MessageBox.Show(
                message,
                T("UiTitleInfo"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static string MapAuth(string code, Dictionary<string, string> meta)
        {
            Dictionary<string, string> metaDictionary = meta ?? new Dictionary<string, string>();

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
                        T(KEY_AUTH_THROTTLE_WAIT_FMT),
                        metaDictionary.ContainsKey(META_KEY_SECONDS)
                            ? metaDictionary[META_KEY_SECONDS]
                            : DEFAULT_RESEND_COOLDOWN_SECONDS.ToString());

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
