using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SnakeAndLaddersFinalProject.AuthService;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class EmailVerificationPage : Page
    {
        private const int DEFAULT_RESEND_COOLDOWN_SECONDS = 45;
        private const int MIN_RESEND_SECONDS = 1;

        private const string AUTH_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IAuthService";
        private const string AUTH_CODE_THROTTLE_WAIT = "Auth.ThrottleWait";

        private const string META_KEY_SECONDS = "seconds";

        private const string KEY_BTN_RESEND_CODE_TEXT = "btnResendCodeText";
        private const string KEY_AUTH_THROTTLE_WAIT_FMT = "AuthThrottleWaitFmt";
        private const string KEY_UI_VERIFICATION_CODE_REQUIRED = "UiVerificationCodeRequired";
        private const string KEY_UI_ENDPOINT_NOT_FOUND = "UiEndpointNotFound";
        private const string KEY_UI_GENERIC_ERROR = "UiGenericError";
        private const string KEY_UI_ACCOUNT_CREATED_FMT = "UiAccountCreatedFmt";

        private readonly AuthService.RegistrationDto _pendingDto;

        private DispatcherTimer _resendTimer;
        private int _remainingSeconds;

        public EmailVerificationPage()
            : this(new AuthService.RegistrationDto
            {
                Email = string.Empty,
                FirstName = string.Empty,
                LastName = string.Empty,
                Password = string.Empty,
                UserName = string.Empty
            })
        {
        }

        public EmailVerificationPage(AuthService.RegistrationDto pendingDto)
        {
            InitializeComponent();

            _pendingDto = pendingDto ?? throw new ArgumentNullException(nameof(pendingDto));

            btnVerificateCode.Click += VerificateCode;
            btnResendCode.Click += ResendCode;

            StartResendCooldown(DEFAULT_RESEND_COOLDOWN_SECONDS);
        }

        private async void VerificateCode(object sender, RoutedEventArgs e)
        {
            string code = (txtCodeSended.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowWarn(T(KEY_UI_VERIFICATION_CODE_REQUIRED));
                return;
            }

            var client = new AuthService.AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                string normalizedEmail = (_pendingDto.Email ?? string.Empty).Trim().ToLowerInvariant();

                AuthResult confirm = await Task.Run(() =>
                    client.ConfirmEmailVerification(normalizedEmail, code));

                if (!confirm.Success)
                {
                    ShowWarn(MapAuth(confirm.Code, confirm.Meta));
                    client.Close();
                    return;
                }

                _pendingDto.Email = normalizedEmail;

                AuthResult register = await Task.Run(() =>
                    client.Register(_pendingDto));

                if (!register.Success)
                {
                    ShowWarn(MapAuth(register.Code, register.Meta));
                    client.Close();
                    return;
                }

                ShowInfo(string.Format(T(KEY_UI_ACCOUNT_CREATED_FMT), register.DisplayName));
                NavigationService?.Navigate(new LoginPage());
                client.Close();
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                ShowError(T(KEY_UI_ENDPOINT_NOT_FOUND));
                client.Abort();
            }
            catch (Exception ex)
            {
                ShowError(string.Format("{0} {1}", T(KEY_UI_GENERIC_ERROR), ex.Message));
                client.Abort();
            }
        }

        private async void ResendCode(object sender, RoutedEventArgs e)
        {
            string email = (_pendingDto.Email ?? string.Empty).Trim().ToLowerInvariant();
            var client = new AuthService.AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                AuthResult result = await Task.Run(() =>
                    client.RequestEmailVerification(email));

                client.Close();

                if (result.Success)
                {
                    ShowInfo(string.Format(T("UiVerificationSentFmt"), email));
                    StartResendCooldown(DEFAULT_RESEND_COOLDOWN_SECONDS);
                    return;
                }

                if (result.Code == AUTH_CODE_THROTTLE_WAIT)
                {
                    int seconds = DEFAULT_RESEND_COOLDOWN_SECONDS;

                    if (result.Meta != null &&
                        result.Meta.TryGetValue(META_KEY_SECONDS, out string secondsText))
                    {
                        if (int.TryParse(secondsText, out int parsedSeconds))
                        {
                            seconds = parsedSeconds;
                        }
                    }

                    ShowWarn(string.Format(T(KEY_AUTH_THROTTLE_WAIT_FMT), seconds));
                    StartResendCooldown(seconds);
                    return;
                }

                ShowWarn(MapAuth(result.Code, result.Meta));
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                ShowError(T(KEY_UI_ENDPOINT_NOT_FOUND));
                client.Abort();
            }
            catch (Exception ex)
            {
                ShowError(string.Format("{0} {1}", T(KEY_UI_GENERIC_ERROR), ex.Message));
                client.Abort();
            }
        }

        private void StartResendCooldown(int seconds)
        {
            _remainingSeconds = Math.Max(MIN_RESEND_SECONDS, seconds);

            btnResendCode.IsEnabled = false;
            UpdateResendButtonContent();

            if (_resendTimer == null)
            {
                _resendTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
            }

            _resendTimer.Tick -= ResendTimerTick;
            _resendTimer.Tick += ResendTimerTick;
            _resendTimer.Start();
        }

        private void ResendTimerTick(object sender, EventArgs e)
        {
            _remainingSeconds--;

            if (_remainingSeconds <= 0)
            {
                _resendTimer.Stop();
                btnResendCode.IsEnabled = true;
                btnResendCode.Content = T(KEY_BTN_RESEND_CODE_TEXT);
            }
            else
            {
                UpdateResendButtonContent();
            }
        }

        private void UpdateResendButtonContent()
        {
            btnResendCode.Content = string.Format(T(KEY_BTN_RESEND_CODE_TEXT), _remainingSeconds);
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
            Dictionary<string, string> metaDictionary = meta ?? new Dictionary<string, string>();

            switch (code)
            {
                case "Auth.Ok":
                    return T("AuthOk");

                case "Auth.EmailRequired":
                    return T("AuthEmailRequired");

                case "Auth.EmailAlreadyExists":
                    return T("AuthEmailAlreadyExists");

                case "Auth.UserNameAlreadyExists":
                    return T("AuthUserNameAlreadyExists");

                case "Auth.InvalidCredentials":
                    return T("AuthInvalidCredentials");

                case "Auth.ThrottleWait":
                    return string.Format(
                        T(KEY_AUTH_THROTTLE_WAIT_FMT),
                        metaDictionary.ContainsKey(META_KEY_SECONDS)
                            ? metaDictionary[META_KEY_SECONDS]
                            : DEFAULT_RESEND_COOLDOWN_SECONDS.ToString());

                case "Auth.CodeNotRequested":
                    return T("AuthCodeNotRequested");

                case "Auth.CodeExpired":
                    return T("AuthCodeExpired");

                case "Auth.CodeInvalid":
                    return T("AuthCodeInvalid");

                case "Auth.EmailSendFailed":
                    return T("AuthEmailSendFailed");

                default:
                    return T("AuthServerError");
            }
        }
    }
}
