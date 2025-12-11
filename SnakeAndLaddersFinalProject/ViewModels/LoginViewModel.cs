using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Utilities;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using log4net;
using SnakeAndLaddersFinalProject.AuthService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class LoginViewModel
    {
        private const string AUTH_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IAuthService";
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LoginViewModel));

        private const int IDENTIFIER_MIN_LENGTH = 1;
        private const int IDENTIFIER_MAX_LENGTH = 150;
        private const int PASSWORD_MIN_LENGTH = 1;
        private const int PASSWORD_MAX_LENGTH = 50;
        private const int INVALID_USER_ID = 0;
        private const int DEFAULT_SKIN_UNLOCKED_ID = 0;

        public sealed class LoginServiceResult
        {
            public bool IsSuccess { get; set; }
            public bool IsEndpointNotFound { get; set; }
            public bool IsGenericError { get; set; }
            public bool HasAuthToken { get; set; }
            public string Code { get; set; } = string.Empty;
            public Dictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
        }

        public static string[] ValidateLogin(string identifier, string password)
        {
            string normalizedIdentifier = InputValidator.Normalize(identifier);
            string normalizedPassword = InputValidator.Normalize(password);

            var errors = new List<string>();

            AddIdentifierValidationErrors(normalizedIdentifier, errors);
            AddPasswordValidationErrors(normalizedPassword, errors);

            return errors.ToArray();
        }

        private static void AddIdentifierValidationErrors(
            string normalizedIdentifier,
            ICollection<string> errors)
        {
            if (!InputValidator.IsRequired(normalizedIdentifier))
            {
                errors.Add(T("UiIdentifierRequired"));
                return;
            }

            if (!InputValidator.IsLengthInRange(
                    normalizedIdentifier,
                    IDENTIFIER_MIN_LENGTH,
                    IDENTIFIER_MAX_LENGTH))
            {
                string messageKey =
                    normalizedIdentifier.Length < IDENTIFIER_MIN_LENGTH
                        ? "UiIdentifierTooShort"
                        : "UiIdentifierTooLong";

                errors.Add(T(messageKey));
                return;
            }

            if (normalizedIdentifier.Contains("@"))
            {
                if (!InputValidator.IsValidEmail(normalizedIdentifier))
                {
                    errors.Add(T("UiEmailInvalid"));
                }

                return;
            }

            if (!InputValidator.IsIdentifierText(
                    normalizedIdentifier,
                    IDENTIFIER_MIN_LENGTH,
                    IDENTIFIER_MAX_LENGTH))
            {
                errors.Add(T("UiIdentifierInvalid"));
            }
        }

        private static void AddPasswordValidationErrors(
            string normalizedPassword,
            ICollection<string> errors)
        {
            if (!InputValidator.IsRequired(normalizedPassword))
            {
                errors.Add(T("UiPasswordRequired"));
                return;
            }

            if (!InputValidator.IsLengthInRange(
                    normalizedPassword,
                    PASSWORD_MIN_LENGTH,
                    PASSWORD_MAX_LENGTH))
            {
                string messageKey =
                    normalizedPassword.Length < PASSWORD_MIN_LENGTH
                        ? "UiPasswordTooShort"
                        : "UiPasswordTooLong";

                errors.Add(T(messageKey));
            }
        }

        private static string T(string key)
        {
            return Properties.Langs.Lang.ResourceManager.GetString(key);
        }

        public async Task<LoginServiceResult> LoginAsync(string identifier, string password)
        {
            var result = new LoginServiceResult();

            string normalizedIdentifier = InputValidator.Normalize(identifier);
            string cleanPassword = password ?? string.Empty;

            var loginDto = new LoginDto
            {
                Email = normalizedIdentifier,
                Password = cleanPassword
            };

            var authClient = new AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                AuthResult response = await Task
                    .Run(() => authClient.Login(loginDto))
                    .ConfigureAwait(true);

                authClient.Close();

                if (response != null && response.Success)
                {
                    SessionContext session = SessionContext.Current;

                    if (session == null)
                    {
                        result.IsSuccess = false;
                        result.IsGenericError = true;
                        return result;
                    }

                    int userId = response.UserId ?? INVALID_USER_ID;
                    string displayName = response.DisplayName ?? string.Empty;
                    string profilePhotoId = response.ProfilePhotoId ?? string.Empty;
                    string token = response.Token ?? string.Empty;

                    string currentSkinId = response.CurrentSkinId ?? string.Empty;
                    int? currentSkinUnlockedId = response.CurrentSkinUnlockedId;

                    session.UserId = userId;
                    session.UserName = string.IsNullOrWhiteSpace(displayName) ? normalizedIdentifier : displayName;
                    session.Email = normalizedIdentifier.Contains("@") ? normalizedIdentifier : string.Empty;
                    session.ProfilePhotoId = AvatarIdHelper.NormalizeOrDefault(profilePhotoId);
                    session.AuthToken = token;
                    session.CurrentSkinId = currentSkinId;
                    session.CurrentSkinUnlockedId = currentSkinUnlockedId ?? DEFAULT_SKIN_UNLOCKED_ID;

                    result.IsSuccess = true;
                    result.HasAuthToken = !string.IsNullOrWhiteSpace(session.AuthToken);
                    return result;
                }

                string code = response?.Code ?? string.Empty;
                Dictionary<string, string> meta = response?.Meta ?? new Dictionary<string, string>();

                result.IsSuccess = false;
                result.Code = code;
                result.Meta = meta;

                return result;
            }
            catch (EndpointNotFoundException ex)
            {
                _logger.Error("Endpoint no encontrado durante Login.", ex);
                authClient.Abort();
                result.IsEndpointNotFound = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error("Error genérico durante Login.", ex);
                authClient.Abort();
                result.IsGenericError = true;
                return result;
            }
        }
    }
}
