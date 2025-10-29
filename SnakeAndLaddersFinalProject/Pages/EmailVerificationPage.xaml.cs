using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class EmailVerificationPage : Page
    {
        private readonly AuthService.RegistrationDto _pendingDto;
        private DispatcherTimer _resendTimer;
        private int _remainingSeconds;
        private const int DefaultResendCooldown = 45; 

        public EmailVerificationPage() : this(new AuthService.RegistrationDto
        {
            Email = "",
            FirstName = "",
            LastName = "",
            Password = "",
            UserName = ""
        })
        { }

        public EmailVerificationPage(AuthService.RegistrationDto pendingDto)
        {
            InitializeComponent();
            _pendingDto = pendingDto ?? throw new ArgumentNullException(nameof(pendingDto));

            btnVerificateCode.Click += VerificateCode;
            btnResendCode.Click += ResendCode;
            StartResendCooldown(DefaultResendCooldown);
        }

        private async void VerificateCode(object sender, RoutedEventArgs e)
        {
            var code = (txtCodeSended.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowWarn(T("UiVerificationCodeRequired"));
                return;
            }

            var client = new AuthService.AuthServiceClient("BasicHttpBinding_IAuthService");
            try
            {
                var normalizedEmail = (_pendingDto.Email ?? string.Empty).Trim().ToLowerInvariant();

                var confirm = await Task.Run(() => client.ConfirmEmailVerification(normalizedEmail, code));
                if (!confirm.Success)
                {
                    ShowWarn(MapAuth(confirm.Code, confirm.Meta));
                    client.Close();
                    return;
                }

                _pendingDto.Email = normalizedEmail;
                var register = await Task.Run(() => client.Register(_pendingDto));
                if (!register.Success)
                {
                    ShowWarn(MapAuth(register.Code, register.Meta));
                    client.Close();
                    return;
                }

                ShowInfo(string.Format(T("UiAccountCreatedFmt"), register.DisplayName));
                NavigationService?.Navigate(new LoginPage());
                client.Close();
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                ShowError(T("UiEndpointNotFound"));
                client.Abort();
            }
            catch (Exception ex)
            {
                ShowError(string.Format("{0} {1}", T("UiGenericError"), ex.Message));
                client.Abort();
            }
        }

        private async void ResendCode(object sender, RoutedEventArgs e)
        {
            var email = (_pendingDto.Email ?? string.Empty).Trim().ToLowerInvariant();
            var client = new AuthService.AuthServiceClient("BasicHttpBinding_IAuthService");

            try
            {
                var res = await Task.Run(() => client.RequestEmailVerification(email));
                client.Close();

                if (res.Success)
                {
                    ShowInfo(string.Format(T("UiVerificationSentFmt"), email));
                    StartResendCooldown(DefaultResendCooldown);
                    return;
                }

                if (res.Code == "Auth.ThrottleWait")
                {
                    int seconds = DefaultResendCooldown;
                    if (res.Meta != null)
                    {
                        string s;
                        if (res.Meta.TryGetValue("seconds", out s))
                        {
                            int n;
                            if (int.TryParse(s, out n)) seconds = n;
                        }
                    }

                    ShowWarn(string.Format(T("AuthThrottleWaitFmt"), seconds));
                    StartResendCooldown(seconds);
                    return;
                }

                ShowWarn(MapAuth(res.Code, res.Meta));
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                ShowError(T("UiEndpointNotFound"));
                client.Abort();
            }
            catch (Exception ex)
            {
                ShowError(string.Format("{0} {1}", T("UiGenericError"), ex.Message));
                client.Abort();
            }
        }
        private void StartResendCooldown(int seconds)
        {
            _remainingSeconds = Math.Max(1, seconds);

            btnResendCode.IsEnabled = false;
            UpdateResendButtonContent();

            if (_resendTimer == null)
                _resendTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

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
                btnResendCode.Content = T("btnResendCodeText");
            }
            else
            {
                UpdateResendButtonContent();
            }
        }

        private void UpdateResendButtonContent()
        {
            btnResendCode.Content = string.Format(T("btnResendCodeText"), _remainingSeconds);
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
                                                                       m.ContainsKey("seconds") ? m["seconds"] : "45");
                case "Auth.CodeNotRequested": return T("AuthCodeNotRequested");
                case "Auth.CodeExpired": return T("AuthCodeExpired");
                case "Auth.CodeInvalid": return T("AuthCodeInvalid");
                case "Auth.EmailSendFailed": return T("AuthEmailSendFailed");
                default: return T("AuthServerError");
            }
        }
    }
}
