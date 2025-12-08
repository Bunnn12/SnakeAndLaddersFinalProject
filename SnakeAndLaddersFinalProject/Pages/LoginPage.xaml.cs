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
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class LoginPage : Page
    {
        private const int GUEST_SUFFIX_MIN_VALUE = 0;
        private const int GUEST_SUFFIX_MAX_EXCLUSIVE = 100;

        private const int GUEST_ID_MIN_VALUE = 1;
        private const int GUEST_ID_MAX_EXCLUSIVE = 1_000_000;

        private const string GUEST_TOKEN_PREFIX = "GUEST-";
        private const string DEFAULT_GUEST_SKIN_ID = "001";

        private const string AUTH_CODE_THROTTLE_WAIT = "Auth.ThrottleWait";

        private const string META_KEY_SECONDS = "seconds";
        private const string META_KEY_SANCTION_TYPE = "sanctionType";
        private const string META_KEY_BAN_ENDS_AT_UTC = "banEndsAtUtc";

        private const string BAN_DATE_DISPLAY_FORMAT = "dd/MM/yyyy HH:mm";
        private const string DEFAULT_THROTTLE_SECONDS_TEXT = "45";

        private const int LOGIN_LOADING_DELAY_MILLISECONDS = 200;

        private static readonly Random _guestRandom = new Random();

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
                ShowError(Lang.UiGenericError);
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
                ShowError(Lang.UiEndpointNotFound);
                return;
            }

            if (result.IsGenericError)
            {
                NavigateToLoginPage();
                ShowError(Lang.UiGenericError);
                return;
            }

            if (result.IsSuccess)
            {
                NavigateToMainPage();

                if (!result.HasAuthToken)
                {
                    ShowWarn(Lang.UiSessionTokenMissingWarn);
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

        private void ShowWarn(string message)
        {
            MessageBox.Show(
                message,
                Lang.UiTitleWarning,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void ShowError(string message)
        {
            MessageBox.Show(
                message,
                Lang.UiTitleError,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static string MapAuth(string code, Dictionary<string, string> meta)
        {
            var metaDictionary = meta ?? new Dictionary<string, string>();

            switch (code)
            {
                case "Auth.Ok":
                    return Lang.AuthOk;

                case "Auth.EmailRequired":
                    return Lang.AuthEmailRequired;

                case "Auth.EmailAlreadyExists":
                    return Lang.AuthEmailAlreadyExists;

                case "Auth.UserNameAlreadyExists":
                    return Lang.AuthUserNameAlreadyExists;

                case "Auth.InvalidCredentials":
                    return Lang.AuthInvalidCredentials;

                case AUTH_CODE_THROTTLE_WAIT:
                    return string.Format(
                        Lang.Auth_ThrottleWaitFmt,
                        metaDictionary.TryGetValue(META_KEY_SECONDS, out string secondsText)
                            ? secondsText
                            : DEFAULT_THROTTLE_SECONDS_TEXT);

                case "Auth.CodeNotRequested":
                    return Lang.AuthCodeNotRequested;

                case "Auth.CodeExpired":
                    return Lang.AuthCodeExpired;

                case "Auth.CodeInvalid":
                    return Lang.AuthCodeInvalid;

                case "Auth.EmailSendFailed":
                    return Lang.AuthEmailSendFailed;

                case "Auth.AccountDeleted":
                    return Lang.AuthAccountDeleted;

                case "Auth.Banned":
                    if (metaDictionary.TryGetValue(META_KEY_SANCTION_TYPE, out string sanctionType) &&
                        string.Equals(sanctionType, "S4", StringComparison.OrdinalIgnoreCase))
                    {
                        return Lang.AuthBannedPermanent;
                    }

                    if (metaDictionary.TryGetValue(META_KEY_BAN_ENDS_AT_UTC, out string rawDate) &&
                        DateTime.TryParse(rawDate, out DateTime banEndsUtc))
                    {
                        DateTime local = banEndsUtc.ToLocalTime();
                        return string.Format(
                            Lang.AuthBannedUntilFmt,
                            local.ToString(BAN_DATE_DISPLAY_FORMAT));
                    }

                    return Lang.AuthBannedGeneric;

                default:
                    return Lang.AuthServerError;
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
                    ShowError(Lang.UiGenericError);
                    return;
                }

                int suffix = _guestRandom.Next(GUEST_SUFFIX_MIN_VALUE, GUEST_SUFFIX_MAX_EXCLUSIVE);

                string guestName = string.Format("{0}{1:D2}", Lang.UiGuestNamePrefix, suffix);

                int guestRandomId = _guestRandom.Next(GUEST_ID_MIN_VALUE, GUEST_ID_MAX_EXCLUSIVE);
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
                ShowError(Lang.UiUnexpectedNavigationError);
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

        private void Settings(object sender, RoutedEventArgs e)
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
