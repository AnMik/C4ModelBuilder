using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Afisha.Tickets.Core.Guard;

namespace Afisha.Tickets.Core.Linq
{
    public static class CollectionExtensions
    {
        public static bool IsEmpty<TItem>(this ICollection<TItem> sequence) => sequence.Count == 0;

        public static bool IsEmpty<TItem>(this IReadOnlyCollection<TItem> sequence) => sequence.Count == 0;

        public static bool IsEmpty<TItem>(this List<TItem> sequence) => sequence.Count == 0;

        public static bool IsEmpty<TItem>(this TItem[] sequence) => sequence.Length == 0;

        public static bool IsEmpty<TItem>(this HashSet<TItem> sequence) => sequence.Count == 0;

        public static bool IsNullOrEmpty<TItem>([NotNullWhen(false)] this ICollection<TItem>? sequence)
            => sequence == null || sequence.Count == 0;

        public static IEnumerable<(DateTime From, DateTime To)> SplitToRanges(this ICollection<DateTime> range)
            => range.Zip(range.Skip(1), (from, to) => (from, to));

        public static IEnumerable<(DateTime From, DateTime To)> SplitToRanges(this IEnumerable<DateTime> range)
            => range.Zip(range.Skip(1), (from, to) => (from, to));

        public static TItem[]? NullIfEmpty<TItem>(this TItem[] sequence)
            => sequence.Any()
                ? sequence
                : null;

        public static IReadOnlyCollection<TItem>? NullIfEmpty<TItem>(this IReadOnlyCollection<TItem> sequence)
            => sequence.Any()
                ? sequence
                : null;

        // todo: comparer
        public static IEnumerable<TItem> IntersectionFromStart<TItem>(this IList<TItem> left, IList<TItem> right)
        {
            Ensure.IsNotNull(left, nameof(left));
            Ensure.IsNotNull(right, nameof(right));

            var comparer = EqualityComparer<TItem>.Default;
            var maxLength = Math.Min(left.Count, right.Count);

            for (var i = 0; i < maxLength; i++)
            {
                if (!comparer.Equals(left[i], right[i]))
                {
                    break;
                }

                yield return left[i];
            }
        }
    }
}
