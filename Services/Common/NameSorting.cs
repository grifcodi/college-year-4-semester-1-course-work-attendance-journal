using System.Globalization;

namespace AttendanceJournal.Services.Common;

/// <summary>
/// SQLite orders strings by code point, which puts Є, І, Ї, Ґ outside the Ukrainian alphabet order.
/// Lists are small, so names are sorted in memory with a uk-UA comparer.
/// </summary>
internal static class NameSorting
{
    public static readonly StringComparer Comparer = CreateComparer();

    public static IOrderedEnumerable<T> OrderByName<T>(this IEnumerable<T> source, Func<T, string> keySelector) =>
        source.OrderBy(keySelector, Comparer);

    private static StringComparer CreateComparer()
    {
        try
        {
            return StringComparer.Create(new CultureInfo("uk-UA"), ignoreCase: true);
        }
        catch (CultureNotFoundException)
        {
            // Invariant-globalization mode: fall back to a case-insensitive ordinal order.
            return StringComparer.OrdinalIgnoreCase;
        }
    }
}
