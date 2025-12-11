using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class InputValidator
    {
        private const int MAX_NORMALIZED_LENGTH = 255;

        private const char DIGIT_ASCII_MIN = '0';
        private const char DIGIT_ASCII_MAX = '9';
        private const char DOUBLE_QUOTE_CHAR = '"';

        private static readonly Regex _emailRegex =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string normalized = value.Trim();

            if (normalized.Length > MAX_NORMALIZED_LENGTH)
            {
                normalized = normalized.Substring(0, MAX_NORMALIZED_LENGTH);
            }

            return normalized;
        }

        public static bool IsRequired(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        public static bool IsLengthInRange(string value, int minLength, int maxLength)
        {
            if (value == null)
            {
                return false;
            }

            int length = value.Length;
            return length >= minLength && length <= maxLength;
        }

        public static bool IsValidEmail(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return _emailRegex.IsMatch(value);
        }


        public static bool IsLettersText(string value, int minLength, int maxLength)
        {
            value = Normalize(value);

            if (!IsRequired(value))
            {
                return false;
            }

            if (!IsLengthInRange(value, minLength, maxLength))
            {
                return false;
            }

            foreach (char character in value)
            {
                if (character == '\r' || character == '\n')
                {
                    return false;
                }

                if (char.IsWhiteSpace(character))
                {
                    continue;
                }

                if (char.IsLetter(character))
                {
                    continue;
                }

                if (character == '-' || character == '\'')
                {
                    continue;
                }

                return false;
            }

            return true;
        }
  
        public static bool IsIdentifierText(string value, int minLength, int maxLength)
        {
            value = Normalize(value);

            if (!IsRequired(value))
            {
                return false;
            }

            if (!IsLengthInRange(value, minLength, maxLength))
            {
                return false;
            }

            foreach (char character in value)
            {
                if (character == '\r' || character == '\n')
                {
                    return false;
                }

                if (char.IsLetterOrDigit(character))
                {
                    continue;
                }

                if (character == '_' ||
                    character == '-' ||
                    character == '.' ||
                    character == '@' ||
                    character == '#' ||
                    character == ' ' ||
                    character == '+')
                {
                    continue;
                }

                if (character == '<' || character == '>')
                {
                    return false;
                }

                return false;
            }

            return true;
        }

        public static bool IsSafeText(
            string value,
            int minLength,
            int maxLength,
            bool allowNewLines)
        {
            value = Normalize(value);

            if (!IsRequired(value))
            {
                return false;
            }

            if (!IsLengthInRange(value, minLength, maxLength))
            {
                return false;
            }

            foreach (char character in value)
            {
                if (!allowNewLines && (character == '\r' || character == '\n'))
                {
                    return false;
                }

                if (char.IsControl(character) &&
                    character != '\r' &&
                    character != '\n' &&
                    character != '\t')
                {
                    return false;
                }

                if (character == '<' || character == '>')
                {
                    return false;
                }

                if (character == DOUBLE_QUOTE_CHAR)
                {
                    return false;
                }
            }

            return true;
        }

     
        public static bool IsNumericCode(string value, int minLength, int maxLength)
        {
            string normalized = Normalize(value);

            if (!IsLengthInRange(normalized, minLength, maxLength))
            {
                return false;
            }

            for (int index = 0; index < normalized.Length; index++)
            {
                char c = normalized[index];

                if (c < DIGIT_ASCII_MIN || c > DIGIT_ASCII_MAX)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsStrongPassword(string value, int minLength, int maxLength)
        {
            value = Normalize(value);

            if (!IsRequired(value))
            {
                return false;
            }

            if (!IsLengthInRange(value, minLength, maxLength))
            {
                return false;
            }

            bool hasUpper = false;
            bool hasLower = false;
            bool hasDigit = false;
            bool hasSpecial = false;

            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    return false;
                }

                if (char.IsUpper(character))
                {
                    hasUpper = true;
                }
                else if (char.IsLower(character))
                {
                    hasLower = true;
                }
                else if (char.IsDigit(character))
                {
                    hasDigit = true;
                }
                else
                {
                    if (!char.IsWhiteSpace(character))
                    {
                        hasSpecial = true;
                    }
                }
            }

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        public static bool IsUrl(string value, int minLength, int maxLength)
        {
            value = Normalize(value);

            if (!IsRequired(value))
            {
                return false;
            }

            if (!IsLengthInRange(value, minLength, maxLength))
            {
                return false;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
            {
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }


    }
}
