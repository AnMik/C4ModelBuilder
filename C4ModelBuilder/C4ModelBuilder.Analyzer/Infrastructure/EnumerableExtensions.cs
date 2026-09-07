using System.Text;

namespace C4ModelBuilder.Analyzer.Infrastructure;

internal static class EnumerableExtensions
{
    public static string JoinStrings<TItem>(
        this IEnumerable<TItem> sequence,
        string separator,
        Func<TItem, string?> converter)
    {
        Ensure.IsNotNull(sequence, nameof(sequence));

        var sb = new StringBuilder();
        sequence.Aggregate(sb, (builder, item) =>
        {
            if (builder.Length > 0)
            {
                builder.Append(separator);
            }
            builder.Append(converter(item));
            return builder;
        });
        return sb.ToString();
    }

    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string JoinStrings(this IEnumerable<string> sequence, string separator)
        => sequence.JoinStrings(separator, x => x);
}
