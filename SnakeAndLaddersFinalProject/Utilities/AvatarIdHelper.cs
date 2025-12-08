using System;
using System.Text.RegularExpressions;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class AvatarIdHelper
    {
        public const string DEFAULT_AVATAR_ID = "A0013";

        private const int MAX_LENGTH = 5;
        private const int REGEX_TIMEOUT_MILLISECONDS = 100;

        private static readonly Regex AVATAR_ID_PATTERN = new Regex(
            @"^A\d{4}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(REGEX_TIMEOUT_MILLISECONDS));

        public static string Normalize(string avatarId)
        {
            if (string.IsNullOrWhiteSpace(avatarId))
            {
                return null;
            }

            string trimmedAvatarId = avatarId.Trim();

            if (trimmedAvatarId.Length > MAX_LENGTH)
            {
                return null;
            }

            return trimmedAvatarId.ToUpperInvariant();
        }

        public static bool IsValid(string avatarId)
        {
            string normalizedAvatarId = Normalize(avatarId);
            if (normalizedAvatarId == null)
            {
                return false;
            }

            return AVATAR_ID_PATTERN.IsMatch(normalizedAvatarId);
        }

        public static string NormalizeOrDefault(string avatarId)
        {
            string normalizedAvatarId = Normalize(avatarId);
            return IsValid(normalizedAvatarId) ? normalizedAvatarId : DEFAULT_AVATAR_ID;
        }
    }
}
