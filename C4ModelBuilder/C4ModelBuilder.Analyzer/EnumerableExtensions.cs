using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Afisha.Tickets.Core.Guard;

namespace Afisha.Tickets.Core.Linq
{
    public static class EnumerableExtensions
    {
        [Obsolete("Use JoinStrings")]
        public static string Join<TSource>(this IEnumerable<TSource>? source, string separator)
            => source != null
                ? string.Join(separator, source)
                : string.Empty;

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            using var enumerator = source.GetEnumerator();

            while (enumerator.MoveNext())
            {
                yield return GetBatch(enumerator, batchSize);
            }
        }

        private static IEnumerable<T> GetBatch<T>(IEnumerator<T> enumerator, int batchSize)
        {
            var elementsLeft = batchSize;
            var batch = new List<T>(batchSize);
            do
            {
                batch.Add(enumerator.Current);
                elementsLeft--;
            }
            while (elementsLeft > 0 && enumerator.MoveNext());

            return batch;
        }

        [Obsolete]
        public static void ForEach<TItem>(this IEnumerable<TItem> sequence, Action<TItem> action)
        {
            foreach (var item in sequence)
            {
                action(item);
            }
        }

        public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? sequence) => sequence?.Any() != true;

        public static bool HasAnyItem<TItem>([NotNullWhen(true)] this IEnumerable<TItem>? sequence) => sequence?.Any() ?? false;

        public static bool HasAnyItem<TItem>([NotNullWhen(true)] this IEnumerable<TItem>? sequence, Func<TItem, bool> condition)
            => sequence?.Any(condition) ?? false;

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

        public static string JoinStrings<TItem>(
            this IEnumerable<TItem> sequence,
            string separator,
            Func<TItem, int> selector)
        {
            Ensure.IsNotNull(sequence, nameof(sequence));

            const int averageItemLength = 3;

            var sb = sequence is ICollection collection
                ? new StringBuilder(collection.Count * (averageItemLength + separator.Length))
                : new StringBuilder();

            foreach (var item in sequence)
            {
                if (sb.Length > 0)
                {
                    sb.Append(separator);
                }

                sb.Append(selector(item));
            }

            return sb.ToString();
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string JoinStrings<TItem>(
            this IEnumerable<TItem> sequence,
            string separator,
            Func<TItem, long> selector)
        {
            Ensure.IsNotNull(sequence, nameof(sequence));

            const int averageItemLength = 5;

            var sb = sequence is ICollection collection
                ? new StringBuilder(collection.Count * (averageItemLength + separator.Length))
                : new StringBuilder();

            foreach (var item in sequence)
            {
                if (sb.Length > 0)
                {
                    sb.Append(separator);
                }

                sb.Append(selector(item));
            }

            return sb.ToString();
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string JoinStrings(this IEnumerable<long> sequence, string separator)
            => sequence.JoinStrings(separator, x => x);

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string JoinStrings(this IEnumerable<string> sequence, string separator)
            => sequence.JoinStrings(separator, x => x);

        public static bool EmptyOrContains<TItem>(this IEnumerable<TItem>? sequence, TItem item)
            => !sequence.HasAnyItem() || sequence.Contains(item);

        public static bool EmptyOrContains<TItem>(this IEnumerable<TItem>? sequence, Func<TItem, bool> predicate)
            => !sequence.HasAnyItem() || sequence.Any(predicate);

        public static bool NotContains<TItem>(this IEnumerable<TItem> sequence, TItem item) => !sequence.Contains(item);

        public static bool NotContains<TItem>(this IEnumerable<TItem> sequence, Predicate<TItem> predicate)
            => sequence.All(item => !predicate(item));

        public static async Task<bool> Any<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, Task<bool>> predicate)
        {
            foreach (var item in source)
            {
                if (await predicate(item).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        public static (IEnumerable<T> Satisfied, IEnumerable<T> NotSatisfied) SplitBy<T>(
            this IEnumerable<T> items,
            Func<T, bool> splitCondition)
        {
            var splitted = items.ToLookup(splitCondition);

            return (splitted[true], splitted[false]);
        }

        public static (IEnumerable<TResult> Satisfied, IEnumerable<TResult> NotSatisfied) SplitBy<T, TResult>(
            this IEnumerable<T> items,
            Func<T, bool> splitCondition,
            Func<T, TResult> elementSelector)
        {
            var splitted = items.ToLookup(splitCondition, elementSelector);

            return (splitted[true], splitted[false]);
        }

        public static (T[] Satisfied, T[] NotSatisfied) SplitByArray<T>(
            this IEnumerable<T> items,
            Func<T, bool> splitCondition)
        {
            var (satisfied, notSatisfied) = SplitBy(items, splitCondition);
            return (satisfied.ToArray(), notSatisfied.ToArray());
        }

        public static decimal? NullableSum(this IEnumerable<decimal?> source)
            => source.Aggregate(
                default(decimal?),
                (sum, t) => sum.HasValue
                    ? sum + (t ?? 0)
                    : t);

        public static Dictionary<TKey, List<TItem>> GroupToDictionary<TKey, TItem>(
            this IEnumerable<TItem> source,
            Func<TItem, TKey> keySelector)
            => source.GroupBy(keySelector).ToDictionary(x => x.Key, x => x.ToList());

        public static Dictionary<TKey, TResult> GroupToDictionary<TKey, TItem, TResult>(
            this IEnumerable<TItem> source,
            Func<TItem, TKey> keySelector,
            Func<IGrouping<TKey, TItem>, TResult> itemsSelector)
            => source.GroupBy(keySelector).ToDictionary(x => x.Key, itemsSelector);

        public static Dictionary<TKey, TItem> ToDistinctDictionary<TItem, TKey>(
            this IEnumerable<TItem> source,
            Func<TItem, TKey> keySelector)
            => source.GroupBy(keySelector).ToDictionary(x => x.Key, x => x.First());

        public static Dictionary<TKey, TValue> ToDistinctDictionary<TItem, TKey, TValue>(
            this IEnumerable<TItem> source,
            Func<TItem, TKey> keySelector,
            Func<TItem, TValue> valueSelector)
            => source.GroupBy(keySelector).ToDictionary(x => x.Key, x => valueSelector(x.First()));
        
        public static bool SequenceEqual<TLeft, TRight>(
            this IReadOnlyList<TLeft> sourceLeft,
            IReadOnlyList<TRight> sourceRight,
            Func<TLeft, TRight, bool> comparer)
        {
            if (sourceLeft.Count != sourceRight.Count)
            {
                return false;
            }

            for (var i = 0; i < sourceLeft.Count; i++)
            {
                if (!comparer(sourceLeft[i], sourceRight[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool SequencesAreNotEqual<TLeft, TRight>(
            this IReadOnlyList<TLeft>? sourceLeft,
            IReadOnlyList<TRight>? sourceRight,
            Func<TLeft, TRight, bool> comparer)
        {
            if (sourceLeft == null && sourceRight == null)
            {
                return false;
            }

            if (sourceLeft?.Count != sourceRight?.Count)
            {
                return true;
            }

            for (var i = 0; i < sourceLeft.Count; i++)
            {
                if (!comparer(sourceLeft[i], sourceRight[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<T> ConcatIf<T>(this IEnumerable<T> source, bool condition, IEnumerable<T> enumerable)
        {
            if (condition)
            {
                return source.Concat(enumerable);
            }

            return source;
        }

        public static T MaxBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
            where TKey : IComparable<TKey>
        {
            Ensure.IsNotNull(source, nameof(source));
            Ensure.IsNotNull(selector, nameof(selector));

            using var iterator = source.GetEnumerator();
            if (!iterator.MoveNext())
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }

            var maxElement = iterator.Current;
            var maxKey = selector(maxElement);

            while (iterator.MoveNext())
            {
                var currentKey = selector(iterator.Current);
                if (currentKey.CompareTo(maxKey) > 0)
                {
                    maxElement = iterator.Current;
                    maxKey = currentKey;
                }
            }

            return maxElement;
        }
    }
}
