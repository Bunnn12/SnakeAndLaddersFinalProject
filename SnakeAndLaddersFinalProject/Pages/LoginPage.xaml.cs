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
        private const int GUEST_ID_MAX_EXCLUSIVE = 1_000_000;

        private const string GUEST_NAME_PREFIX = "Guest";
        private const string GUEST_TOKEN_PREFIX = "GUEST-";

        private const string DEFAULT_GUEST_SKIN_ID = "001";

        private const string AUTH_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IAuthService";
        private const string AUTH_CODE_THROTTLE_WAIT = "Auth.ThrottleWait";

        private const string META_KEY_SECONDS = "seconds";
        private const string META_KEY_SANCTION_TYPE = "sanctionType";
        private const string META_KEY_BAN_ENDS_AT_UTC = "banEndsAtUtc";

        private const string BAN_DATE_DISPLAY_FORMAT = "dd/MM/yyyy HH:mm";

        private const string ICON_KIND_WARNING = "warning";
        private const string ICON_KIND_INFO = "info";
        private const string ICON_KIND_ERROR = "error";

        private const string ICON_URI_WARNING = "pack://application:,,,/Assets/Icons/warning.png";
        private const string ICON_URI_INFO = "pack://application:,,,/Assets/Icons/info.png";
        private const string ICON_URI_ERROR = "pack://application:,,,/Assets/Icons/error.png";

        public LoginPage()
        {
            InitializeComponent();
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

        private string[] ValidateLogin(string identifier, string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(identifier))
            {
                errors.Add(T("UiIdentifierRequired"));
            }
            else if (identifier.Contains("@") &&
                     !Regex.IsMatch(identifier, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                errors.Add(T("UiEmailInvalid"));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add(T("UiPasswordRequired"));
            }

            return errors.ToArray();
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
            btnNavLogin.IsChecked = false;
            btnSignUp.IsChecked = false;
            NavigationService?.Navigate(new SignUpPage());
        }

        private async void Login(object sender, RoutedEventArgs e)
        {
            string identifier = txtUsername.Text.Trim();
            string password = pwdPassword.Password;

            string[] errors = ValidateLogin(identifier, password);
            if (errors.Any())
            {
                ShowWarn(string.Join("\n", errors));
                return;
            }

            var loginDto = new AuthService.LoginDto
            {
                Email = identifier,
                Password = password
            };

            var authClient = new AuthService.AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            dynamic response = null;
            Window owner = Window.GetWindow(this);

            try
            {
                owner?.Dispatcher.Invoke(() =>
                {
                    var frame = owner.FindName("MainFrame") as Frame;
                    frame?.Navigate(new LoadingPage());
                });

                await Task.Delay(500);

                response = await Task.Run(() => authClient.Login(loginDto));

                bool isSuccess;
                try
                {
                    isSuccess = (bool)(response?.Success ?? false);
                }
                catch
                {
                    isSuccess = false;
                }

                if (isSuccess)
                {
                    int userId;
                    try
                    {
                        userId = (int)(response?.UserId ?? 0);
                    }
                    catch
                    {
                        userId = 0;
                    }

                    string displayName;
                    try
                    {
                        displayName = (string)response?.DisplayName;
                    }
                    catch
                    {
                        displayName = null;
                    }

                    string profilePhotoId;
                    try
                    {
                        profilePhotoId = (string)response?.ProfilePhotoId;
                    }
                    catch
                    {
                        profilePhotoId = null;
                    }

                    string token = TryGetToken(response);

                    string currentSkinId;
                    int? currentSkinUnlockedId;

                    try
                    {
                        currentSkinId = (string)response?.CurrentSkinId;
                    }
                    catch
                    {
                        currentSkinId = null;
                    }

                    try
                    {
                        currentSkinUnlockedId = (int?)response?.CurrentSkinUnlockedId;
                    }
                    catch
                    {
                        currentSkinUnlockedId = null;
                    }

                    SessionContext.Current.UserId = userId;
                    SessionContext.Current.UserName =
                        string.IsNullOrWhiteSpace(displayName) ? identifier : displayName;
                    SessionContext.Current.Email = identifier.Contains("@") ? identifier : string.Empty;
                    SessionContext.Current.ProfilePhotoId =
                        AvatarIdHelper.NormalizeOrDefault(profilePhotoId);
                    SessionContext.Current.AuthToken = token ?? string.Empty;

                    SessionContext.Current.CurrentSkinId = currentSkinId;
                    SessionContext.Current.CurrentSkinUnlockedId = currentSkinUnlockedId;

                    if (string.IsNullOrWhiteSpace(SessionContext.Current.AuthToken))
                    {
                        ShowWarn(
                            "Sesión iniciada, pero el servicio no devolvió token. Algunas funciones pueden no estar disponibles.");
                    }

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

                    string code;
                    Dictionary<string, string> meta;

                    try
                    {
                        code = (string)response?.Code;
                    }
                    catch
                    {
                        code = null;
                    }

                    try
                    {
                        meta = response?.Meta as Dictionary<string, string>;
                    }
                    catch
                    {
                        meta = null;
                    }

                    ShowWarn(MapAuth(code, meta));
                }

                authClient.Close();
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                owner?.Dispatcher.Invoke(() =>
                {
                    var frame = owner.FindName("MainFrame") as Frame;
                    frame?.Navigate(new LoginPage());
                });

                ShowError(T("UiEndpointNotFound"));
                authClient.Abort();
            }
            catch (Exception ex)
            {
                owner?.Dispatcher.Invoke(() =>
                {
                    var frame = owner.FindName("MainFrame") as Frame;
                    frame?.Navigate(new LoginPage());
                });

                ShowError($"{T("UiGenericError")} {ex.Message}");
                authClient.Abort();
            }
        }

        private static string TryGetToken(dynamic response)
        {
            try
            {
                return (string)(response?.Token
                                ?? response?.AuthToken
                                ?? response?.SessionToken
                                ?? response?.AccessToken);
            }
            catch
            {
                return null;
            }
        }

        private static string T(string key) =>
            Globalization.LocalizationManager.Current[key];

        private void ShowWarn(string message) =>
            ShowDialog(T("UiTitleWarning"), message, DialogButtons.Ok, GetIcon(ICON_KIND_WARNING));

        private void ShowInfo(string message) =>
            ShowDialog(T("UiTitleInfo"), message, DialogButtons.Ok, GetIcon(ICON_KIND_INFO));

        private void ShowError(string message) =>
            ShowDialog(T("UiTitleError"), message, DialogButtons.Ok, GetIcon(ICON_KIND_ERROR));

        private void ShowDialog(string title, string message, DialogButtons buttons, string iconPackUri = null)
        {
            Window owner = Window.GetWindow(this);
            _ = DialogBasicWindow.Show(owner, title, message, buttons, iconPackUri);
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
                    return null;
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
                            : "45");

                case "Auth.CodeNotRequested":
                    return T("AuthCodeNotRequested");

                case "Auth.CodeExpired":
                    return T("AuthCodeExpired");

                case "Auth.CodeInvalid":
                    return T("AuthCodeInvalid");

                case "Auth.EmailSendFailed":
                    return T("AuthEmailSendFailed");

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
            btnNavLogin.IsChecked = true;
            btnSignUp.IsChecked = false;
        }

        private void PlayAsGuest(object sender, RoutedEventArgs e)
        {
            try
            {
                var random = new Random();

                int suffix = random.Next(GUEST_SUFFIX_MIN_VALUE, GUEST_SUFFIX_MAX_EXCLUSIVE);
                string guestName = $"{GUEST_NAME_PREFIX}{suffix:D2}";

                int guestRandomId = random.Next(GUEST_ID_MIN_VALUE, GUEST_ID_MAX_EXCLUSIVE);
                int guestUserId = guestRandomId * -1;

                SessionContext.Current.UserId = guestUserId;
                SessionContext.Current.UserName = guestName;
                SessionContext.Current.Email = string.Empty;
                SessionContext.Current.ProfilePhotoId = AvatarIdHelper.DEFAULT_AVATAR_ID;

                SessionContext.Current.AuthToken = $"{GUEST_TOKEN_PREFIX}{Guid.NewGuid():N}";

                SessionContext.Current.CurrentSkinId = DEFAULT_GUEST_SKIN_ID;
                SessionContext.Current.CurrentSkinUnlockedId = null;

                if (NavigationService != null)
                {
                    NavigationService.Navigate(new MainPage());
                    return;
                }

                Window currentWindow = Window.GetWindow(this);
                var mainFrame = currentWindow?.FindName("MainFrame") as Frame;
                if (mainFrame != null)
                {
                    mainFrame.Navigate(new MainPage());
                    return;
                }
            }
            catch
            {
                ShowError("Unexpected error while navigating.");
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            Window currentWindow = Window.GetWindow(this);
            var mainFrame = currentWindow?.FindName("MainFrame") as Frame;
            mainFrame?.Navigate(new SettingsPage());
        }
    }
}
