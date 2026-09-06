namespace C4ModelBuilder.Analyzer;

internal static class StringExtensions
{
    public static bool ContainsIgnoreCase(this string? baseString, string valueString)
    {
        if (baseString == null)
        {
            return false;
        }

        return baseString.IndexOf(valueString, StringComparison.InvariantCultureIgnoreCase) >= 0;
    }
}
