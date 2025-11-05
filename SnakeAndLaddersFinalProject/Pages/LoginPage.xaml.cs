using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Utilities;




namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class LoginPage : Page
    {
        private const int GUEST_SUFFIX_MIN_VALUE = 0;
        private const int GUEST_SUFFIX_MAX_EXCLUSIVE = 100;

        private const int GUEST_ID_MIN_VALUE = 1;
        private const int GUEST_ID_MAX_EXCLUSIVE = 1000000;
        public LoginPage()
        {
            InitializeComponent();
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            
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
            var identifier = txtUsername.Text.Trim();
            var password = pwdPassword.Password;

            var errs = ValidateLogin(identifier, password);
            if (errs.Any())
            {
                ShowWarn(string.Join("\n", errs));
                return;
            }

            var dto = new AuthService.LoginDto { Email = identifier, Password = password };
            var client = new AuthService.AuthServiceClient("BasicHttpBinding_IAuthService");

            dynamic res = null;
            var owner = Window.GetWindow(this);

            try
            {
                owner?.Dispatcher.Invoke(() =>
                {
                    var frame = owner.FindName("MainFrame") as Frame;
                    frame?.Navigate(new SnakeAndLaddersFinalProject.Pages.LoadingPage());
                });

                await Task.Delay(5000);
                res = await Task.Run(() => client.Login(dto));

                bool success = false;
                try { success = (bool)res?.Success; } catch { success = false; }

                if (success)
                {
                    int userId = 0;
                    try { userId = (int)(res?.UserId ?? 0); } catch { userId = 0; }

                    string displayName = null;
                    try { displayName = (string)res?.DisplayName; } catch { displayName = null; }

                    string profilePhotoId = null;
                    try { profilePhotoId = (string)res?.ProfilePhotoId; } catch { profilePhotoId = null; }

                    SessionContext.Current.UserId = userId;
                    SessionContext.Current.UserName = string.IsNullOrWhiteSpace(displayName)
                        ? identifier
                        : displayName;
                    SessionContext.Current.Email = identifier.Contains("@") ? identifier : string.Empty;

                    // 🔥 Aquí ligas la sesión con el avatar de BD
                    SessionContext.Current.ProfilePhotoId =
                        AvatarIdHelper.NormalizeOrDefault(profilePhotoId);

                    owner?.Dispatcher.Invoke(() =>
                    {
                        var frame = owner.FindName("MainFrame") as Frame;
                        frame?.Navigate(new MainPage());
                    });
                }
                else
                {
                    owner?.Dispatcher.Invoke(() =>
                    {
                        var frame = owner.FindName("MainFrame") as Frame;
                        frame?.Navigate(new LoginPage());
                    });

                    string code = null;
                    Dictionary<string, string> meta = null;
                    try { code = (string)res?.Code; } catch { code = null; }
                    try { meta = res?.Meta as Dictionary<string, string>; } catch { meta = null; }

                    ShowWarn(MapAuth(code, meta));
                }

                client.Close();
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                owner?.Dispatcher.Invoke(() =>
                {
                    var frame = owner.FindName("MainFrame") as Frame;
                    frame?.Navigate(new LoginPage());
                });
                ShowError(T("UiEndpointNotFound"));
                client.Abort();
            }
            catch (Exception ex)
            {
                owner?.Dispatcher.Invoke(() =>
                {
                    var frame = owner.FindName("MainFrame") as Frame;
                    frame?.Navigate(new LoginPage());
                });
                ShowError($"{T("UiGenericError")} {ex.Message}");
                client.Abort();
            }
        }


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
                var random = new Random();

                int suffix = random.Next(GUEST_SUFFIX_MIN_VALUE, GUEST_SUFFIX_MAX_EXCLUSIVE);
                string guestName = $"Guest{suffix:D2}";

                int guestRandomId = random.Next(GUEST_ID_MIN_VALUE, GUEST_ID_MAX_EXCLUSIVE);
                int guestUserId = guestRandomId * -1;   // id negativo para guest

                SessionContext.Current.UserId = guestUserId;
                SessionContext.Current.UserName = guestName;
                SessionContext.Current.Email = string.Empty;
                SessionContext.Current.ProfilePhotoId = AvatarIdHelper.DefaultId;

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

                var navWindow = new NavigationWindow
                {
                    ShowsNavigationUI = true
                };

                navWindow.Navigate(new MainPage());
                navWindow.Show();
            }
            catch (InvalidOperationException ex)
            {
                ShowInfo("Navigation is not available right now. Please try again.");
            }
            catch (Exception ex)
            {
                
                ShowError("Unexpected error while navigating.");
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var currentWindow = Window.GetWindow(this);
            var mainFrame = currentWindow?.FindName("MainFrame") as Frame;
            mainFrame.Navigate(new SnakeAndLaddersFinalProject.Pages.SettingsPage());


        }
    }
}
