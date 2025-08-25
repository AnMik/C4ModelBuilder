using System;
using System.Text;
using Afisha.Tickets.Core.Guard;

namespace Afisha.Tickets.Core.Objects
{
    public static class StringExtensions
    {
        public static string? EnsureStartsWith(this string? value, string startWith)
        {
            if (value == null)
            {
                return null;
            }

            return !value.StartsWith(startWith)
                ? $"{startWith}{value}"
                : value;
        }

        public static string TrimEndWith(this string value, int maxLength)
            => value.TrimEndWith(maxLength, string.Empty);

        public static string TrimEndWith(this string value, int maxLength, string replaceSymbols)
        {
            Ensure.IsNotNull(value, nameof(value));
            Ensure.IsGreaterThenZero(maxLength, nameof(maxLength));
            Ensure.That(
                maxLength >= replaceSymbols.Length,
                "Максимальная длина строки не может быть меньше длины замещающей строки");

            return value.Length <= maxLength
                ? value
                : $"{value.Substring(0, maxLength - replaceSymbols.Length)}{replaceSymbols}";
        }

        public static string WithMaxByteLength(this string input, int maxLength, Encoding encoding)
        {
            Ensure.IsGreaterThenZero(maxLength, nameof(maxLength));

            var byteLength = encoding.GetByteCount(input);
            if (byteLength <= maxLength)
            {
                return input;
            }

            var message = input;
            for (var i = input.Length; i >= 0; i--)
            {
                message = message.Substring(0, i);

                if (encoding.GetByteCount(message) <= maxLength)
                {
                    return message;
                }
            }

            return string.Empty;
        }

        public static bool ContainsIgnoreCase(this string? baseString, string valueString)
        {
            if (baseString == null)
            {
                return false;
            }

            return baseString.IndexOf(valueString, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        public static bool EqualsIgnoreCase(this string? left, string? right)
            => StringComparer.CurrentCultureIgnoreCase.Equals(left, right);

        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        public static string SubstringEnd(this string value, int length)
        {
            Ensure.That(value.Length >= length, "Длина итоговой строки должна быть меньше исходной");

            return value.Substring(length, value.Length - length);
        }

        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        public static string TrimEnd(this string value, string trimmedString)
        {
            Ensure.IsNotNull(value, nameof(value));

            return value.EndsWith(trimmedString)
                ? value.Remove(value.LastIndexOf(trimmedString))
                : value;
        }
    }
}
