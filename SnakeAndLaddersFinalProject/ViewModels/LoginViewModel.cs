using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class LoginViewModel
    {
        private const string AUTH_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IAuthService";

        private const int IDENTIFIER_MIN_LENGTH = 1;
        private const int IDENTIFIER_MAX_LENGTH = 90;
        private const int PASSWORD_MIN_LENGTH = 8;
        private const int PASSWORD_MAX_LENGTH = 510;

        private static readonly Regex EMAIL_REGEX =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public sealed class LoginServiceResult
        {
            public bool IsSuccess { get; set; }

            public bool IsEndpointNotFound { get; set; }

            public bool IsGenericError { get; set; }

            public bool HasAuthToken { get; set; }

            public string Code { get; set; }

            public Dictionary<string, string> Meta { get; set; }
        }

        public string[] ValidateLogin(string identifier, string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(identifier))
            {
                errors.Add(T("UiIdentifierRequired"));
            }
            else
            {
                if (identifier.Contains("@") && !EMAIL_REGEX.IsMatch(identifier))
                {
                    errors.Add(T("UiEmailInvalid"));
                }

                if (identifier.Length < IDENTIFIER_MIN_LENGTH)
                {
                    errors.Add(T("UiIdentifierTooShort"));
                }

                if (identifier.Length > IDENTIFIER_MAX_LENGTH)
                {
                    errors.Add(T("UiIdentifierTooLong"));
                }
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add(T("UiPasswordRequired"));
            }
            else
            {
                if (password.Length < PASSWORD_MIN_LENGTH)
                {
                    errors.Add(T("UiPasswordTooShort"));
                }

                if (password.Length > PASSWORD_MAX_LENGTH)
                {
                    errors.Add(T("UiPasswordTooLong"));
                }
            }

            return errors.ToArray();
        }

        public async Task<LoginServiceResult> LoginAsync(string identifier, string password)
        {
            var loginDto = new AuthService.LoginDto
            {
                Email = identifier,
                Password = password
            };

            var authClient = new AuthService.AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);
            dynamic response = null;

            try
            {
                response = await Task.Run(
                        () => authClient.Login(loginDto))
                    .ConfigureAwait(true);

                bool isSuccess;

                try
                {
                    isSuccess = response?.Success == true;
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

                    if (SessionContext.Current == null)
                    {
                        authClient.Close();

                        return new LoginServiceResult
                        {
                            IsSuccess = false,
                            IsGenericError = true
                        };
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

                    authClient.Close();

                    return new LoginServiceResult
                    {
                        IsSuccess = true,
                        HasAuthToken = !string.IsNullOrWhiteSpace(SessionContext.Current.AuthToken)
                    };
                }

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

                authClient.Close();

                return new LoginServiceResult
                {
                    IsSuccess = false,
                    Code = code,
                    Meta = meta
                };
            }
            catch (EndpointNotFoundException)
            {
                authClient.Abort();

                return new LoginServiceResult
                {
                    IsEndpointNotFound = true
                };
            }
            catch (Exception)
            {
                authClient.Abort();

                return new LoginServiceResult
                {
                    IsGenericError = true
                };
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

        private static string T(string key)
        {
            return Globalization.LocalizationManager.Current[key];
        }
    }
}
