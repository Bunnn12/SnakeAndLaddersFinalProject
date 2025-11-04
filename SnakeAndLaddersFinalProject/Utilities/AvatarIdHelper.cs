using System;
using System.Text.RegularExpressions;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class AvatarIdHelper
    {
        public const string DefaultId = "A0013";

        private const int MaxLength = 5;

        private static readonly Regex Pattern = new Regex(
            @"^A\d{4}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        public static string Normalize(string avatarId)
        {
            if (string.IsNullOrWhiteSpace(avatarId))
            {
                return null;
            }

            string trimmed = avatarId.Trim();

            if (trimmed.Length > MaxLength)
            {
                return null;
            }

            return trimmed.ToUpperInvariant();
        }

        public static bool IsValid(string avatarId)
        {
            string normalizedId = Normalize(avatarId);
            if (normalizedId == null)
            {
                return false;
            }

            return Pattern.IsMatch(normalizedId);
        }

        public static string NormalizeOrDefault(string avatarId)
        {
            string normalizedId = Normalize(avatarId);
            return IsValid(normalizedId) ? normalizedId : DefaultId;
        }

        public static string MapFromDb(string dbValue)
        {
            string normalizedDbValue = Normalize(dbValue);
            return IsValid(normalizedDbValue) ? normalizedDbValue : DefaultId;
        }

        public static string MapToDb(string appValue)
        {
            string normalizedAppValue = Normalize(appValue);

            if (normalizedAppValue == null)
            {
                return null;
            }

            if (!IsValid(normalizedAppValue))
            {
                throw new ArgumentException(
                    "Avatar id must have format 'A' followed by 4 digits (example: A0001).",
                    nameof(appValue));
            }

            return normalizedAppValue;
        }
    }
}
