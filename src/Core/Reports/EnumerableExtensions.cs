using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Reports;

public static class EnumerableExtensions
{
    // Власний метод розширення: топ-N за числовим ключем з обов'язковим другим ключем.
    // Запит лишається відкладеним: виконається, коли викликач зробить ToList() або foreach.
    public static IEnumerable<T> TopBy<T>(
        this IEnumerable<T> source, Func<T, decimal> key, Func<T, string> tieBreaker, int n)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(tieBreaker);

        return source.OrderByDescending(key)
                     .ThenBy(tieBreaker, StringComparer.Ordinal)
                     .Take(n);
    }
}