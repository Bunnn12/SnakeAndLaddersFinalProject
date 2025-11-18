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
        private const string DEFAULT_SKIN_KEY = "003";
        private const int SKIN_CODE_DIGITS = 3;

        private const string TOKEN_PREFIX = "T";
        private const string IDLE_SUFFIX = "-idle";
        private const string SAD_SUFFIX = "-sad";

        private const string BASE_SKINS_FOLDER = "/Assets/Images/Skins/";
        private const string TOKENS_SUBFOLDER = "Tokens/";

       

        public static string NormalizeSkinKey(int skinId)
        {
            if (skinId <= 0)
            {
                return DEFAULT_SKIN_KEY;
            }

            return skinId.ToString(
                "D" + SKIN_CODE_DIGITS.ToString(CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);
        }

        public static string NormalizeSkinKey(int? skinId)
        {
            if (!skinId.HasValue || skinId.Value <= 0)
            {
                return DEFAULT_SKIN_KEY;
            }

            return NormalizeSkinKey(skinId.Value);
        }

        public static string NormalizeSkinKey(string skinId)
        {
            if (string.IsNullOrWhiteSpace(skinId))
            {
                return DEFAULT_SKIN_KEY;
            }

            skinId = skinId.Trim();

            if (int.TryParse(skinId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericId))
            {
                return NormalizeSkinKey(numericId); // "3" -> "003"
            }

            // Si ya viene "003" o "012" se deja tal cual
            return skinId;
        }

        // ================= DESCRIPTOR LÓGICO =================

        public static SkinAssetDescriptor ResolveAssets(int? skinId)
        {
            var skinKey = NormalizeSkinKey(skinId);
            var tokenKey = TOKEN_PREFIX + skinKey; // T003
            var idleKey = skinKey + IDLE_SUFFIX;   // 003-idle
            var sadKey = skinKey + SAD_SUFFIX;     // 003-sad

            return new SkinAssetDescriptor(skinKey, tokenKey, idleKey, sadKey);
        }

        public static SkinAssetDescriptor ResolveAssets(string skinId)
        {
            var skinKey = NormalizeSkinKey(skinId);
            var tokenKey = TOKEN_PREFIX + skinKey;
            var idleKey = skinKey + IDLE_SUFFIX;
            var sadKey = skinKey + SAD_SUFFIX;

            return new SkinAssetDescriptor(skinKey, tokenKey, idleKey, sadKey);
        }

        // ================= RUTAS =================

        // /Assets/Images/Skins/003.png
        public static string GetSkinRelativePath(string skinKey)
        {
            return $"{BASE_SKINS_FOLDER}{skinKey}.png";
        }

        // /Assets/Images/Skins/Tokens/T003.png
        public static string GetTokenRelativePath(string skinKey)
        {
            return $"{BASE_SKINS_FOLDER}{TOKENS_SUBFOLDER}{TOKEN_PREFIX}{skinKey}.png";
        }

        // Helpers desde IDs que vienen del server

        public static string GetSkinPathFromSkinId(string skinId)
        {
            var skinKey = NormalizeSkinKey(skinId);
            return GetSkinRelativePath(skinKey);
        }

        public static string GetSkinPathFromSkinId(int? skinId)
        {
            var skinKey = NormalizeSkinKey(skinId);
            return GetSkinRelativePath(skinKey);
        }

        public static string GetTokenPathFromSkinId(string skinId)
        {
            var skinKey = NormalizeSkinKey(skinId);
            return GetTokenRelativePath(skinKey);
        }

        public static string GetTokenPathFromSkinId(int? skinId)
        {
            var skinKey = NormalizeSkinKey(skinId);
            return GetTokenRelativePath(skinKey);
        }
    }
}
