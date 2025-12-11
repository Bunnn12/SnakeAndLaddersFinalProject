using log4net;
using System.Diagnostics;
using System.Globalization;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public sealed class SkinAssetDescriptor
    {
        public string SkinKey { get; }
        public string TokenKey { get; }
        public string IdleKey { get; }
        public string SadKey { get; }

        public SkinAssetDescriptor(string skinKey, string tokenKey, string idleKey, string sadKey)
        {
            SkinKey = skinKey;
            TokenKey = tokenKey;
            IdleKey = idleKey;
            SadKey = sadKey;
        }
    }

    public static class SkinAssetHelper
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SkinAssetHelper));

        private const string DEFAULT_SKIN_KEY = "004";
        private const int SKIN_CODE_DIGITS = 3;

        private const string TOKEN_PREFIX = "Token";
        private const string IDLE_SUFFIX = "-idle";
        private const string SAD_SUFFIX = "-sad";

        private const string BASE_SKINS_FOLDER = "/Assets/Images/Skins/";
        private const string TOKENS_SUBFOLDER = "Tokens/";

        public static string NormalizeSkinKey(int skinId)
        {
            if (skinId <= 0)
            {
                _logger.InfoFormat(
                    "NormalizeSkinKey(int): skinId={0} inválido, usando default {1}.",
                    skinId,
                    DEFAULT_SKIN_KEY);

                return DEFAULT_SKIN_KEY;
            }

            string normalized = skinId.ToString(
                "D" + SKIN_CODE_DIGITS.ToString(CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);

            _logger.InfoFormat(
                "NormalizeSkinKey(int): skinId={0} -> {1}.",
                skinId,
                normalized);

            return normalized;
        }

        public static string NormalizeSkinKey(int? skinId)
        {
            if (!skinId.HasValue || skinId.Value <= 0)
            {
                var stackTrace = new StackTrace();

                _logger.WarnFormat(
                    "NormalizeSkinKey(int?) llamado con NULL o <= 0. Usando default {0}. StackTrace:\n{1}",
                    DEFAULT_SKIN_KEY,
                    stackTrace);

                return DEFAULT_SKIN_KEY;
            }

            return NormalizeSkinKey(skinId.Value);
        }

        public static string NormalizeSkinKey(string skinId)
        {
            if (string.IsNullOrWhiteSpace(skinId))
            {
                _logger.InfoFormat(
                    "NormalizeSkinKey(string): skinId vacío o nulo, usando default {0}.",
                    DEFAULT_SKIN_KEY);

                return DEFAULT_SKIN_KEY;
            }

            string trimmed = skinId.Trim();

            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericId))
            {
                string normalizedFromInt = NormalizeSkinKey(numericId);

                _logger.InfoFormat(
                    "NormalizeSkinKey(string): raw='{0}' (num={1}) -> {2}.",
                    skinId,
                    numericId,
                    normalizedFromInt);

                return normalizedFromInt;
            }

            _logger.InfoFormat(
                "NormalizeSkinKey(string): raw='{0}' no numérico, se usa tal cual -> '{1}'.",
                skinId,
                trimmed);

            return trimmed;
        }


        public static SkinAssetDescriptor ResolveAssets(int? skinId)
        {
            string skinKey = NormalizeSkinKey(skinId);

            _logger.InfoFormat(
                "ResolveAssets(int?): skinId={0} -> skinKey='{1}'.",
                skinId,
                skinKey);

            string tokenKey = TOKEN_PREFIX + skinKey;
            string idleKey = skinKey + IDLE_SUFFIX;
            string sadKey = skinKey + SAD_SUFFIX;

            return new SkinAssetDescriptor(skinKey, tokenKey, idleKey, sadKey);
        }

        public static SkinAssetDescriptor ResolveAssets(string skinId)
        {
            string skinKey = NormalizeSkinKey(skinId);

            _logger.InfoFormat(
                "ResolveAssets(string): skinId='{0}' -> skinKey='{1}'.",
                skinId,
                skinKey);

            string tokenKey = TOKEN_PREFIX + skinKey;
            string idleKey = skinKey + IDLE_SUFFIX;
            string sadKey = skinKey + SAD_SUFFIX;

            return new SkinAssetDescriptor(skinKey, tokenKey, idleKey, sadKey);
        }

        public static string GetSkinRelativePath(string skinKey)
        {
            return $"{BASE_SKINS_FOLDER}{skinKey}.png";
        }

        public static string GetTokenRelativePath(string skinKey)
        {
            return $"{BASE_SKINS_FOLDER}{TOKENS_SUBFOLDER}{TOKEN_PREFIX}{skinKey}.png";
        }

        public static string GetSkinPathFromSkinId(string skinId)
        {
            string skinKey = NormalizeSkinKey(skinId);
            return GetSkinRelativePath(skinKey);
        }

        public static string GetSkinPathFromSkinId(int? skinId)
        {
            string skinKey = NormalizeSkinKey(skinId);
            return GetSkinRelativePath(skinKey);
        }

        public static string GetTokenPathFromSkinId(string skinId)
        {
            string skinKey = NormalizeSkinKey(skinId);
            return GetTokenRelativePath(skinKey);
        }

        public static string GetTokenPathFromSkinId(int? skinId)
        {
            string skinKey = NormalizeSkinKey(skinId);
            return GetTokenRelativePath(skinKey);
        }
    }
}
