using System.Diagnostics.CodeAnalysis;

namespace C4ModelBuilder.Analyzer.Infrastructure;

internal static class Ensure
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
    public static void That(bool assertion, string message)
    {
        if (!assertion)
        {
            throw new ArgumentException(message);
        }
    }
}
