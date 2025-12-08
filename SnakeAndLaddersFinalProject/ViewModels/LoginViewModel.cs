using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Utilities;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class LoginViewModel
    {
        private const string AUTH_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IAuthService";

        private const int IDENTIFIER_MIN_LENGTH = 1;
        private const int IDENTIFIER_MAX_LENGTH = 90;
        private const int PASSWORD_MIN_LENGTH = 1;   // en login solo exigimos que no esté vacío
        private const int PASSWORD_MAX_LENGTH = 510;

        public sealed class LoginServiceResult
        {
            public bool IsSuccess { get; set; }

            public bool IsEndpointNotFound { get; set; }

            public bool IsGenericError { get; set; }

            public bool HasAuthToken { get; set; }

            public string Code { get; set; } = string.Empty;

            public Dictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
        }

        public string[] ValidateLogin(string identifier, string password)
        {
            var errors = new List<string>();

            string normalizedIdentifier = InputValidator.Normalize(identifier);
            string normalizedPassword = InputValidator.Normalize(password);

            // ===== IDENTIFICADOR (email o username) =====
            if (!InputValidator.IsRequired(normalizedIdentifier))
            {
                errors.Add(T("UiIdentifierRequired"));
            }
            else
            {
                if (!InputValidator.IsLengthInRange(
                        normalizedIdentifier,
                        IDENTIFIER_MIN_LENGTH,
                        IDENTIFIER_MAX_LENGTH))
                {
                    if (normalizedIdentifier.Length < IDENTIFIER_MIN_LENGTH)
                    {
                        errors.Add(T("UiIdentifierTooShort"));
                    }
                    else
                    {
                        errors.Add(T("UiIdentifierTooLong"));
                    }
                }
                else if (normalizedIdentifier.Contains("@"))
                {
                    // email
                    if (!InputValidator.IsValidEmail(normalizedIdentifier))
                    {
                        errors.Add(T("UiEmailInvalid"));
                    }
                }
                else
                {
                    // username / identificador (permite chino, dígitos, algunos símbolos seguros)
                    if (!InputValidator.IsIdentifierText(
                            normalizedIdentifier,
                            IDENTIFIER_MIN_LENGTH,
                            IDENTIFIER_MAX_LENGTH))
                    {
                        errors.Add(T("UiIdentifierInvalid"));
                    }
                }
            }

            // ===== PASSWORD (login: NO fuerza formato fuerte) =====
            if (!InputValidator.IsRequired(normalizedPassword))
            {
                errors.Add(T("UiPasswordRequired"));
            }
            else
            {
                if (!InputValidator.IsLengthInRange(
                        normalizedPassword,
                        PASSWORD_MIN_LENGTH,
                        PASSWORD_MAX_LENGTH))
                {
                    if (normalizedPassword.Length < PASSWORD_MIN_LENGTH)
                    {
                        errors.Add(T("UiPasswordTooShort"));
                    }
                    else
                    {
                        errors.Add(T("UiPasswordTooLong"));
                    }
                }
                else if (!InputValidator.IsSafeText(
                             normalizedPassword,
                             PASSWORD_MIN_LENGTH,
                             PASSWORD_MAX_LENGTH,
                             allowNewLines: false))
                {
                    // sin formato fuerte, solo texto “seguro”
                    errors.Add(T("UiPasswordInvalid"));
                }
            }

            return errors.ToArray();
        }

        public async Task<LoginServiceResult> LoginAsync(string identifier, string password)
        {
            var result = new LoginServiceResult();

            string normalizedIdentifier = InputValidator.Normalize(identifier);
            string normalizedPassword = InputValidator.Normalize(password);

            var loginDto = new AuthService.LoginDto
            {
                Email = normalizedIdentifier,
                Password = normalizedPassword
            };

            var authClient = new AuthService.AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                AuthService.AuthResult response = await Task
                    .Run(() => authClient.Login(loginDto))
                    .ConfigureAwait(true);

                if (response != null && response.Success)
                {
                    SessionContext session = SessionContext.Current;

                    if (session == null)
                    {
                        authClient.Close();

                        result.IsSuccess = false;
                        result.IsGenericError = true;
                        return result;
                    }

                    int userId = response.UserId ?? 0;
                    string displayName = response.DisplayName ?? string.Empty;
                    string profilePhotoId = response.ProfilePhotoId ?? string.Empty;
                    string token = response.Token ?? string.Empty;

                    string currentSkinId = response.CurrentSkinId ?? string.Empty;
                    int? currentSkinUnlockedId = response.CurrentSkinUnlockedId;

                    session.UserId = userId;
                    session.UserName =
                        string.IsNullOrWhiteSpace(displayName) ? normalizedIdentifier : displayName;
                    session.Email = normalizedIdentifier.Contains("@") ? normalizedIdentifier : string.Empty;
                    session.ProfilePhotoId = AvatarIdHelper.NormalizeOrDefault(profilePhotoId);
                    session.AuthToken = token;
                    session.CurrentSkinId = currentSkinId;
                    session.CurrentSkinUnlockedId = currentSkinUnlockedId;

                    authClient.Close();

                    result.IsSuccess = true;
                    result.HasAuthToken = !string.IsNullOrWhiteSpace(session.AuthToken);
                    return result;
                }

                string code = response?.Code ?? string.Empty;
                Dictionary<string, string> meta = response?.Meta ?? new Dictionary<string, string>();

                authClient.Close();

                result.IsSuccess = false;
                result.Code = code;
                result.Meta = meta;

                return result;
            }
            catch (EndpointNotFoundException)
            {
                authClient.Abort();

                result.IsEndpointNotFound = true;
                return result;
            }
            catch (Exception)
            {
                authClient.Abort();

                result.IsGenericError = true;
                return result;
            }
        }

        private static string T(string key)
        {
            return Globalization.LocalizationManager.Current[key];
        }
    }
}
