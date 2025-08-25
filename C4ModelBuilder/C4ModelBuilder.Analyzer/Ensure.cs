using System.Diagnostics.CodeAnalysis;
using Afisha.Tickets.Core.Linq;

namespace Afisha.Tickets.Core.Guard
{
    public static class Ensure
    {
        /// <exception cref="ArgumentNullException" />
        public static void IsNotNull<T>([NotNull] T value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName, "Value cannot be null.");
            }
        }

        /// <exception cref="ArgumentNullException" />
        public static void IsNotNull<T>([NotNull]T? value, string paramName)
            where T : struct
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName, "Value cannot be null.");
            }
        }

        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        public static void IsNotNullOrEmpty([NotNull]string? value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (value.Length == 0)
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsNotEmpty(string value, string paramName)
        {
            if (value.Length == 0)
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        public static void IsNotNullOrWhiteSpace([NotNull]string? value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty or white space.", paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        public static void IsNotNullOrEmptySequence<T>([NotNull]IEnumerable<T>? value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (!value.Any())
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsNotDefault(short value, string paramName)
        {
            if (value == default)
            {
                throw new ArgumentException(paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsNotDefault(int value, string paramName)
        {
            if (value == default)
            {
                throw new ArgumentException(paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsNotDefault(long value, string paramName)
        {
            if (value == default)
            {
                throw new ArgumentException(paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsNotDefault(decimal value, string paramName)
        {
            if (value == default)
            {
                throw new ArgumentException($"Значение параметра не может быть равно {default(decimal)}", paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsNotDefault(Guid value, string paramName)
        {
            if (value == default)
            {
                throw new ArgumentException(paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsNotDefault(DateTime value, string paramName)
        {
            if (value == default)
            {
                throw new ArgumentException(paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsGreaterThenZero(short value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentException(paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsGreaterThenZero(int value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentException(paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsGreaterThenZero(ushort value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentException(paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsGreaterThenZero(long value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentException(paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsGreaterThenZero(decimal value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentException(paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsGreaterThenZero(double value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentException(paramName);
            }
        }

        /// <exception cref="ArgumentOutOfRangeException" />
        public static void IsNonNegative(int value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.");
            }
        }

        /// <exception cref="ArgumentOutOfRangeException" />
        public static void IsNonNegative(int? value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.");
            }
        }

        /// <exception cref="ArgumentOutOfRangeException" />
        public static void IsNonNegative(decimal value, string paramName)
        {
            if (value < decimal.Zero)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.");
            }
        }

        /// <exception cref="ArgumentOutOfRangeException" />
        public static void IsNonNegative(short value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.");
            }
        }

        public static void IsLessThan(long threshold, long value, string paramName)
        {
            if (value >= threshold)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"Value cannot be out of range of {threshold}");
            }
        }

        public static void IsLessThanOrEqual(long threshold, long value, string paramName)
        {
            if (value > threshold)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"Value cannot be out of range of {threshold}");
            }
        }

        public static void IsLessThanOrEqual(decimal threshold, decimal value, string paramName)
        {
            if (value > threshold)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"Value cannot be out of range of {threshold}");
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsNotEmptyGuid(Guid value, string paramName)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException(paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void That(bool assertion, string message)
        {
            if (!assertion)
            {
                throw new ArgumentException(message);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsAnyNotNullOrWhiteSpace(params string?[] values)
        {
            if (!values.Any(p => !string.IsNullOrWhiteSpace(p)))
            {
                throw new ArgumentException("Хотя бы одно из переданных значений должно быть обязательно заполнено");
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsAllNotNull<T>(IEnumerable<T>? values, string paramName)
        {
            if (values.HasAnyItem(x => x is null))
            {
                throw new ArgumentException("В перечислении не должно быть значений null.", paramName);
            }
        }


        /// <exception cref="ArgumentException" />
        public static void Equals(string x, string y, string message)
        {
            if (x != y)
            {
                throw new ArgumentException(message);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void Equals(Guid x, Guid y, string message)
        {
            if (x != y)
            {
                throw new ArgumentException(message);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsNotDefault(decimal value, string paramName, string message)
        {
            if (value == default)
            {
                throw new ArgumentException(message, paramName);
            }
        }

        /// <exception cref="ArgumentException" />
        public static void IsNotDefault(Guid value, string paramName, string message)
        {
            if (value == default)
            {
                throw new ArgumentException(message, paramName);
            }
        }
    }
}
