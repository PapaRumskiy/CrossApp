namespace Core.Reports;

public static class EnumerableExtensions
{
    /// <summary>Ranks by descending value, then by ascending ordinal Id. Exact ties keep source order.</summary>
    public static IEnumerable<T> TopBy<T>(
        this IEnumerable<T> source, int count, Func<T, decimal> valueSelector, Func<T, string> idSelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(valueSelector);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        return count == 0
            ? Enumerable.Empty<T>()
            : source.OrderByDescending(valueSelector).ThenBy(idSelector, StringComparer.Ordinal).Take(count);
    }
}
