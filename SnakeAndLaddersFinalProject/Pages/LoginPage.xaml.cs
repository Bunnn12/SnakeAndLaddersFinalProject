using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) { }

        private string[] ValidateLogin(string identifier, string pwd)
        {
            var errs = new List<string>();
            if (string.IsNullOrWhiteSpace(identifier)) errs.Add(T("UiIdentifierRequired"));
            else if (identifier.Contains("@") && !Regex.IsMatch(identifier, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errs.Add(T("UiEmailInvalid"));
            if (string.IsNullOrWhiteSpace(pwd)) errs.Add(T("UiPasswordRequired"));
            return errs.ToArray();
        }

        private void PwdPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnLogin_Click(btnLogin, new RoutedEventArgs());
        }

        private void BtnSignUp_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SignUpPage());
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var id = txtUsername.Text.Trim();   // usuario O correo
            var pwd = pwdPassword.Password;

            var errs = ValidateLogin(id, pwd);
            if (errs.Any())
            {
                ShowWarn(string.Join("\n", errs));
                return;
            }

            var dto = new AuthService.LoginDto { Email = id, Password = pwd }; // 'Email' = identificador
            var client = new AuthService.AuthServiceClient("BasicHttpBinding_IAuthService");
            try
            {
                var res = await Task.Run(() =>client.Login(dto));
                if (res.Success)
                {
                    ShowInfo(T("UiLoginOk"));
                    NavigationService?.Navigate(new MainPage());
                }
                else
                {
                    ShowWarn(MapAuth(res.Code, res.Meta));
                }
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

        // ===== Helpers =====
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
