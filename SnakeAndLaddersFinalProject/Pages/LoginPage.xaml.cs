using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject;
using SnakeAndLaddersFinalProject.Authentication;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            // Fuerza el estado “Login activo, SignUp inactivo”
            if (btnNavLogin != null) btnNavLogin.IsChecked = true;
            if (btnSignUp != null) btnSignUp.IsChecked = false;
        }
        private void TextBoxTextChanged(object sender, TextChangedEventArgs e) { }

        private string[] ValidateLogin(string identifier, string pwd)
        {
            var errs = new List<string>();
            if (string.IsNullOrWhiteSpace(identifier)) errs.Add(T("UiIdentifierRequired"));
            else if (identifier.Contains("@") && !Regex.IsMatch(identifier, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errs.Add(T("UiEmailInvalid"));
            if (string.IsNullOrWhiteSpace(pwd)) errs.Add(T("UiPasswordRequired"));
            return errs.ToArray();
        }

        private void PwdPasswordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Login(btnLogin, new RoutedEventArgs());
        }

        private void SignUp(object sender, RoutedEventArgs e)
        {
            btnNavLogin.IsChecked = false;
            btnSignUp.IsChecked = false; 
            NavigationService?.Navigate(new SignUpPage());
        }

        private async void Login(object sender, RoutedEventArgs e)
        {
            var identifier = txtUsername.Text.Trim();   // usuario o correo
            var password = pwdPassword.Password;

            var errs = ValidateLogin(identifier, password);
            if (errs.Any())
            {
                ShowWarn(string.Join("\n", errs));
                return;
            }

            var dto = new AuthService.LoginDto { Email = identifier, Password = password };
            var client = new AuthService.AuthServiceClient("BasicHttpBinding_IAuthService");

            try
            {
                var res = await Task.Run(() => client.Login(dto));

                if (res.Success)
                {
                    SessionContext.Current.UserId = res.UserId ?? 0;
                    SessionContext.Current.UserName = string.IsNullOrWhiteSpace(res.DisplayName)
                        ? identifier
                        : res.DisplayName;
                    SessionContext.Current.Email = identifier.Contains("@") ? identifier : string.Empty;

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

        private void ShowWarn(string msg) =>
            ShowDialog(T("UiTitleWarning"), msg, DialogButtons.Ok, GetIcon("warning"));

        private void ShowInfo(string msg) =>
            ShowDialog(T("UiTitleInfo"), msg, DialogButtons.Ok, GetIcon("info"));

        private void ShowError(string msg) =>
            ShowDialog(T("UiTitleError"), msg, DialogButtons.Ok, GetIcon("error"));

        private void ShowDialog(string title, string message, DialogButtons buttons, string iconPackUri = null)
        {
            var owner = Window.GetWindow(this);
            _ = DialogBasicWindow.Show(owner, title, message, buttons, iconPackUri);
        }

        private static string GetIcon(string kind)
        {
            switch (kind)
            {
                case "warning": return "pack://application:,,,/Assets/Icons/warning.png";
                case "info": return "pack://application:,,,/Assets/Icons/info.png";
                case "error": return "pack://application:,,,/Assets/Icons/error.png";
                default: return null;
            }
        }

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

        private void NavLogin(object sender, RoutedEventArgs e)
        {
            btnNavLogin.IsChecked = true;
            btnSignUp.IsChecked = false;
        }



        private void PlayAsGuest(object sender, RoutedEventArgs e)
        {
            try
            {
                // Deja algo consistente en sesión
                SessionContext.Current.UserId = 0;
                SessionContext.Current.UserName = "Guest";
                SessionContext.Current.Email = string.Empty;

                if (NavigationService != null)
                {
                    NavigationService.Navigate(new MainPage());
                    return;
                }

                var currentWindow = Window.GetWindow(this);
                var mainFrame = currentWindow?.FindName("MainFrame") as Frame;
                if (mainFrame != null)
                {
                    mainFrame.Navigate(new MainPage());
                    return;
                }

                var navWindow = new NavigationWindow { ShowsNavigationUI = true };
                navWindow.Navigate(new MainPage());
                navWindow.Show();
            }
            catch (InvalidOperationException)
            {
                ShowInfo("Navigation is not available right now. Please try again.");
            }
            catch (Exception)
            {
                ShowError("Unexpected error while navigating.");
            }
        }
    }
}
