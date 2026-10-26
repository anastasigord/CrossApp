using System;
using System.Collections.Generic;
using System.Linq;
using Core.Domain;

namespace Core.Reports;

public static class BenchmarkData
{
    public static List<Order> GenerateOrders(int count)
    {
        var list = new List<Order>(count);
        for (int i = 0; i < count; i++) // ручний цикл виправданий: тут ми створюємо дані, а не запитуємо їх
        {
            var order = Order.Create($"G-{i:D6}", $"C-{i % 500:D3}");
            order.AddLine("P-GEN", "Генерований товар", (i % 1000) + 1, (i % 3) + 1);
            list.Add(order);
        }
        return list;
    }

    // Варіант 1: один ланцюжок, матеріалізація лише в кінці
    public static IReadOnlyList<OrderReportLine> TopLazy(IEnumerable<Order> source, decimal min, int n) =>
        source.Where(o => o.Total > min)
              .OrderByDescending(o => o.Total).ThenBy(o => o.Id, StringComparer.Ordinal)
              .Take(n)
              .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Status.ToString(), o.Lines.Count))
              .ToList();

    // Варіант 2: ToList() після кожного кроку (зайві копії колекції)
    public static IReadOnlyList<OrderReportLine> TopWithMiddleToList(IEnumerable<Order> source, decimal min, int n) =>
        source.Where(o => o.Total > min).ToList()
              .OrderByDescending(o => o.Total).ThenBy(o => o.Id, StringComparer.Ordinal).ToList()
              .Take(n).ToList()
              .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Status.ToString(), o.Lines.Count))
              .ToList();
}