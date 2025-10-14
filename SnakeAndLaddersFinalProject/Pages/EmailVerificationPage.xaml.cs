using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class EmailVerificationPage : Page
    {
        private readonly AuthService.RegistrationDto _pendingDto;

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

            lblVerificationMessage.Content = string.Format(T("UiVerificationSentFmt"), _pendingDto.Email);
            btnVerificateCode.Click += BtnVerificateCode_Click;
        }

        private async void BtnVerificateCode_Click(object sender, RoutedEventArgs e)
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

                // 1) Confirmar código
                var confirm = await Task.Run(() => client.ConfirmEmailVerification(normalizedEmail, code));
                if (!confirm.Success)
                {
                    ShowWarn(MapAuth(confirm.Code, confirm.Meta));
                    client.Close();
                    return;
                }

                // 2) Registrar
                _pendingDto.Email = normalizedEmail;
                var register = await Task.Run(() => client.Register(_pendingDto));
                if (!register.Success)
                {
                    ShowWarn(MapAuth(register.Code, register.Meta));
                    client.Close();
                    return;
                }

                ShowInfo(string.Format(T("UiAccountCreatedFmt"), register.DisplayName));
                NavigationService?.Navigate(new MainPage());
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
                    return string.Format(T("Auth_ThrottleWaitFmt"),
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
