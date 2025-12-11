using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Properties.Langs;
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
        private const int LOGIN_LOADING_DELAY_MILLISECONDS = 200;

        private const string GUEST_TOKEN_PREFIX = "GUEST-";
        private const string DEFAULT_GUEST_SKIN_ID = "001";

        private const string AUTH_CODE_THROTTLE_WAIT = "Auth.ThrottleWait";
        private const string AUTH_CODE_SESSION_ALREADY_ACTIVE = "Auth.SessionAlreadyActive";
        private const string META_KEY_SECONDS = "seconds";
        private const string META_KEY_SANCTION_TYPE = "sanctionType";
        private const string META_KEY_BAN_ENDS_AT_UTC = "banEndsAtUtc";
        private const string BAN_DATE_DISPLAY_FORMAT = "dd/MM/yyyy HH:mm";
        private const string DEFAULT_THROTTLE_SECONDS_TEXT = "45";
        private const string KICK_TYPE_PERMANENT = "S4";

        private static readonly Random _guestRandom = new Random();
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LoginPage));

        private LoginViewModel ViewModel
        {
            get { return DataContext as LoginViewModel; }
        }

        public LoginPage()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();
            Loaded += PageLoaded;
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            if (btnNavLogin != null)
            {
                btnNavLogin.IsChecked = true;
                btnNavLogin.Content = Lang.btnLoginText;
            }

            if (btnSignUp != null)
            {
                btnSignUp.IsChecked = false;
                btnSignUp.Content = Lang.btnSignUpText;
            }

            if (lblUsername != null)
            {
                lblUsername.Content = Lang.txtUsernameText;
            }

            if (lblPassword != null)
            {
                lblPassword.Content = Lang.pwdPasswordText;
            }

            if (btnLogin != null)
            {
                btnLogin.Content = Lang.btnLoginText;
            }

            if (btnPlayAsGuest != null)
            {
                btnPlayAsGuest.Content = Lang.btnPlayAsGuestText;
            }

            if (btnForgottenPassword != null)
            {
                btnForgottenPassword.Content = Lang.btnForgottenPasswordText;
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
            try
            {
                string identifier = txtUsername.Text?.Trim() ?? string.Empty;
                string password = pwdPassword.Password ?? string.Empty;

                LoginViewModel viewModel = ViewModel;
                if (viewModel == null)
                {
                    ShowError(Lang.UiGenericError);
                    return;
                }

                string[] errors = LoginViewModel.ValidateLogin(identifier, password);
                if (errors.Any())
                {
                    ShowWarn(string.Join("\n", errors));
                    return;
                }

                NavigateToLoadingPage();

                await Task.Delay(LOGIN_LOADING_DELAY_MILLISECONDS)
                    .ConfigureAwait(true);

                LoginViewModel.LoginServiceResult result =
                    await viewModel.LoginAsync(identifier, password)
                        .ConfigureAwait(true);

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

                if (result.IsGenericError || result.IsEndpointNotFound)
                {
                    string technicalMessage = result.UserMessage;

                    if (string.IsNullOrWhiteSpace(technicalMessage))
                    {
                        technicalMessage = Lang.UiGenericError;
                    }

                    ShowError(technicalMessage);
                    return;
                }

                ShowWarn(MapAuth(result.Code, result.Meta));
            }
            catch (Exception ex)
            {
                UiExceptionHelper.ShowModuleError(
                    ex,
                    "LoginPage.Login",
                    _logger,
                    Lang.UiLoginGenericError);

                NavigateToLoginPage();
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

                int suffix = _guestRandom.Next(
                    GUEST_SUFFIX_MIN_VALUE,
                    GUEST_SUFFIX_MAX_EXCLUSIVE);

                string guestName = string.Format(
                    "{0}{1:D2}",
                    Lang.UiGuestNamePrefix,
                    suffix);

                int guestRandomId = _guestRandom.Next(
                    GUEST_ID_MIN_VALUE,
                    GUEST_ID_MAX_EXCLUSIVE);

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
            catch (Exception ex)
            {
                UiExceptionHelper.ShowModuleError(
                    ex,
                    "LoginPage.PlayAsGuest",
                    _logger,
                    Lang.UiUnexpectedNavigationError);
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
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    if (TryGetMainFrame(out Frame mainFrame))
                    {
                        mainFrame.Navigate(new LoginPage());
                        return;
                    }

                    NavigationService?.Navigate(new LoginPage());
                });
        }

        private void NavigateToMainPage()
        {
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    if (TryGetMainFrame(out Frame mainFrame))
                    {
                        mainFrame.Navigate(new MainPage());
                        return;
                    }

                    NavigationService?.Navigate(new MainPage());
                });
        }

        private bool TryGetMainFrame(out Frame mainFrame)
        {
            Window owner = Window.GetWindow(this) ?? Application.Current?.MainWindow;
            mainFrame = owner?.FindName("MainFrame") as Frame;
            return mainFrame != null;
        }

        private void ShowWarn(string message)
        {
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    MessageBox.Show(
                        message,
                        Lang.UiTitleWarning,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
        }

        private void ShowError(string message)
        {
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    MessageBox.Show(
                        message,
                        Lang.UiTitleError,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
        }

        private static string MapAuth(string code, Dictionary<string, string> meta)
        {
            Dictionary<string, string> metaDictionary =
                meta ?? new Dictionary<string, string>();

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
                    if (metaDictionary.TryGetValue(META_KEY_SANCTION_TYPE, out string sanctionType)
                        && string.Equals(
                            sanctionType,
                            KICK_TYPE_PERMANENT,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return Lang.AuthBannedPermanent;
                    }

                    if (metaDictionary.TryGetValue(META_KEY_BAN_ENDS_AT_UTC, out string rawDate)
                        && DateTime.TryParse(rawDate, out DateTime banEndsUtc))
                    {
                        DateTime local = banEndsUtc.ToLocalTime();

                        return string.Format(
                            Lang.AuthBannedUntilFmt,
                            local.ToString(BAN_DATE_DISPLAY_FORMAT));
                    }

                    return Lang.AuthBannedGeneric;

                case AUTH_CODE_SESSION_ALREADY_ACTIVE:
                    return Lang.AuthSessionAlreadyActive;

                default:
                    return Lang.AuthServerError;
            }
        }
    }
}
