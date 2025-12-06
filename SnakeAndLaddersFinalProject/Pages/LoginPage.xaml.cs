using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class LoginPage : Page
    {
        private const int GUEST_SUFFIX_MIN_VALUE = 0;
        private const int GUEST_SUFFIX_MAX_EXCLUSIVE = 100;

        private const int GUEST_ID_MIN_VALUE = 1;
        private const int GUEST_ID_MAX_EXCLUSIVE = 1_000_000;

        private const string GUEST_NAME_PREFIX = "Guest";
        private const string GUEST_TOKEN_PREFIX = "GUEST-";

        private const string DEFAULT_GUEST_SKIN_ID = "001";

        private const string AUTH_CODE_THROTTLE_WAIT = "Auth.ThrottleWait";

        private const string META_KEY_SECONDS = "seconds";
        private const string META_KEY_SANCTION_TYPE = "sanctionType";
        private const string META_KEY_BAN_ENDS_AT_UTC = "banEndsAtUtc";

        private const string BAN_DATE_DISPLAY_FORMAT = "dd/MM/yyyy HH:mm";
        private const string DEFAULT_THROTTLE_SECONDS_TEXT = "45";

        private const string ICON_KIND_WARNING = "warning";
        private const string ICON_KIND_INFO = "info";
        private const string ICON_KIND_ERROR = "error";

        private const string ICON_URI_WARNING = "pack://application:,,,/Assets/Icons/warning.png";
        private const string ICON_URI_INFO = "pack://application:,,,/Assets/Icons/info.png";
        private const string ICON_URI_ERROR = "pack://application:,,,/Assets/Icons/error.png";

        private const int LOGIN_LOADING_DELAY_MILLISECONDS = 200;

        private static readonly Random GuestRandom = new Random();

        private LoginViewModel ViewModel => DataContext as LoginViewModel;

        public LoginPage()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            if (btnNavLogin != null)
            {
                btnNavLogin.IsChecked = true;
            }

            if (btnSignUp != null)
            {
                btnSignUp.IsChecked = false;
            }
        }

        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void PwdPasswordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Login(btnLogin, new RoutedEventArgs());
            }
        }

        private void SignUp(object sender, RoutedEventArgs e)
        {
            if (btnNavLogin != null)
            {
                btnNavLogin.IsChecked = false;
            }

            if (btnSignUp != null)
            {
                btnSignUp.IsChecked = false;
            }

            NavigationService?.Navigate(new SignUpPage());
        }

        private async void Login(object sender, RoutedEventArgs e)
        {
            string identifier = txtUsername.Text?.Trim() ?? string.Empty;
            string password = pwdPassword.Password ?? string.Empty;

            LoginViewModel viewModel = ViewModel;
            if (viewModel == null)
            {
                ShowError(T("UiGenericError"));
                return;
            }

            string[] errors = viewModel.ValidateLogin(identifier, password);
            if (errors.Any())
            {
                ShowWarn(string.Join("\n", errors));
                return;
            }

            NavigateToLoadingPage();

            await Task.Delay(LOGIN_LOADING_DELAY_MILLISECONDS).ConfigureAwait(true);

            LoginViewModel.LoginServiceResult result =
                await viewModel.LoginAsync(identifier, password).ConfigureAwait(true);

            if (result.IsEndpointNotFound)
            {
                NavigateToLoginPage();
                ShowError(T("UiEndpointNotFound"));
                return;
            }

            if (result.IsGenericError)
            {
                NavigateToLoginPage();
                ShowError(T("UiGenericError"));
                return;
            }

            if (result.IsSuccess)
            {
                NavigateToMainPage();

                if (!result.HasAuthToken)
                {
                    ShowWarn(
                        "Sesión iniciada, pero el servicio no devolvió token. Algunas funciones pueden no estar disponibles.");
                }

                return;
            }

            NavigateToLoginPage();

            ShowWarn(MapAuth(result.Code, result.Meta));
        }

        private void NavigateToLoadingPage()
        {
            if (TryGetMainFrame(out Frame mainFrame))
            {
                mainFrame.Navigate(new LoadingPage());
                return;
            }

            NavigationService?.Navigate(new LoadingPage());
        }

        private void NavigateToLoginPage()
        {
            if (TryGetMainFrame(out Frame mainFrame))
            {
                mainFrame.Navigate(new LoginPage());
                return;
            }

            NavigationService?.Navigate(new LoginPage());
        }

        private void NavigateToMainPage()
        {
            if (TryGetMainFrame(out Frame mainFrame))
            {
                mainFrame.Navigate(new MainPage());
                return;
            }

            NavigationService?.Navigate(new MainPage());
        }

        private bool TryGetMainFrame(out Frame mainFrame)
        {
            Window owner = Window.GetWindow(this) ?? Application.Current?.MainWindow;
            mainFrame = owner?.FindName("MainFrame") as Frame;
            return mainFrame != null;
        }

        private static string T(string key) =>
            Globalization.LocalizationManager.Current[key];

        private void ShowWarn(string message) =>
            ShowDialog(T("UiTitleWarning"), message, GetIcon(ICON_KIND_WARNING));

        private void ShowInfo(string message) =>
            ShowDialog(T("UiTitleInfo"), message, GetIcon(ICON_KIND_INFO));

        private void ShowError(string message) =>
            ShowDialog(T("UiTitleError"), message, GetIcon(ICON_KIND_ERROR));

        private void ShowDialog(string title, string message, string iconPackUri)
        {
            Window owner = Window.GetWindow(this) ?? Application.Current?.MainWindow;
            _ = DialogBasicWindow.Show(owner, title, message, DialogButtons.Ok, iconPackUri);
        }

        private static string GetIcon(string kind)
        {
            switch (kind)
            {
                case ICON_KIND_WARNING:
                    return ICON_URI_WARNING;

                case ICON_KIND_INFO:
                    return ICON_URI_INFO;

                case ICON_KIND_ERROR:
                    return ICON_URI_ERROR;

                default:
                    return ICON_URI_INFO;
            }
        }

        private static string MapAuth(string code, Dictionary<string, string> meta)
        {
            var metaDictionary = meta ?? new Dictionary<string, string>();

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

                case AUTH_CODE_THROTTLE_WAIT:
                    return string.Format(
                        T("Auth_ThrottleWaitFmt"),
                        metaDictionary.TryGetValue(META_KEY_SECONDS, out string secondsText)
                            ? secondsText
                            : DEFAULT_THROTTLE_SECONDS_TEXT);

                case "Auth.CodeNotRequested":
                    return T("AuthCodeNotRequested");

                case "Auth.CodeExpired":
                    return T("AuthCodeExpired");

                case "Auth.CodeInvalid":
                    return T("AuthCodeInvalid");

                case "Auth.EmailSendFailed":
                    return T("AuthEmailSendFailed");

                case "Auth.AccountDeleted":
                    return T("AuthAccountDeleted");

                case "Auth.Banned":
                    if (metaDictionary.TryGetValue(META_KEY_SANCTION_TYPE, out string sanctionType) &&
                        string.Equals(sanctionType, "S4", StringComparison.OrdinalIgnoreCase))
                    {
                        return T("AuthBannedPermanent");
                    }

                    if (metaDictionary.TryGetValue(META_KEY_BAN_ENDS_AT_UTC, out string rawDate) &&
                        DateTime.TryParse(rawDate, out DateTime banEndsUtc))
                    {
                        DateTime local = banEndsUtc.ToLocalTime();
                        return string.Format(
                            T("AuthBannedUntilFmt"),
                            local.ToString(BAN_DATE_DISPLAY_FORMAT));
                    }

                    return T("AuthBannedGeneric");

                default:
                    return T("AuthServerError");
            }
        }

        private void NavLogin(object sender, RoutedEventArgs e)
        {
            if (btnNavLogin != null)
            {
                btnNavLogin.IsChecked = true;
            }

            if (btnSignUp != null)
            {
                btnSignUp.IsChecked = false;
            }
        }

        private void PlayAsGuest(object sender, RoutedEventArgs e)
        {
            try
            {
                SessionContext session = SessionContext.Current;
                if (session == null)
                {
                    ShowError(T("UiGenericError"));
                    return;
                }

                int suffix = GuestRandom.Next(GUEST_SUFFIX_MIN_VALUE, GUEST_SUFFIX_MAX_EXCLUSIVE);
                string guestName = $"{GUEST_NAME_PREFIX}{suffix:D2}";

                int guestRandomId = GuestRandom.Next(GUEST_ID_MIN_VALUE, GUEST_ID_MAX_EXCLUSIVE);
                int guestUserId = guestRandomId * -1;

                session.UserId = guestUserId;
                session.UserName = guestName;
                session.Email = string.Empty;
                session.ProfilePhotoId = AvatarIdHelper.DEFAULT_AVATAR_ID;
                session.AuthToken = $"{GUEST_TOKEN_PREFIX}{Guid.NewGuid():N}";
                session.CurrentSkinId = DEFAULT_GUEST_SKIN_ID;
                session.CurrentSkinUnlockedId = null;

                NavigateToMainPage();
            }
            catch
            {
                ShowError("Unexpected error while navigating.");
            }
        }

        private void ForgottenPassword(object sender, RoutedEventArgs e)
        {
            if (TryGetMainFrame(out Frame mainFrame))
            {
                mainFrame.Navigate(new ChangePasswordPage());
                return;
            }

            NavigationService?.Navigate(new ChangePasswordPage());
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (TryGetMainFrame(out Frame mainFrame))
            {
                mainFrame.Navigate(new SettingsPage());
                return;
            }

            NavigationService?.Navigate(new SettingsPage());
        }
    }
}
