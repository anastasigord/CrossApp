using System;
using System.Collections.Generic;
using System.Linq;
using Core.Domain;
using Core.Dto;

namespace Core.Reports;

public static class ReportService
{
    // Топ-N зі стабільним порядком
    public static IReadOnlyList<OrderReportLine> TopByTotal(IEnumerable<Order> source, int n) =>
        source
            .TopBy(o => o.Total, o => o.Id, n)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Status.ToString(), o.Lines.Count))
            .ToList();

    // Фільтр + сортування + проєкція: замовлення з сумою менше порога
    public static IReadOnlyList<OrderReportLine> LowValueOrders(IEnumerable<Order> source, decimal threshold) =>
        source.Where(o => o.Total < threshold)
              .OrderBy(o => o.Total).ThenBy(o => o.Id, StringComparer.Ordinal)
              .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Status.ToString(), o.Lines.Count))
              .ToList();

    // Групування за статусом (методний синтаксис)
    public static IReadOnlyList<GroupSummary> ByStatus(IEnumerable<Order> source) =>
        source
            .GroupBy(o => o.Status.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(s => s.TotalAmount)
            .ToList();

    // Той самий звіт у синтаксисі запитів (додаткове завдання)
    public static IReadOnlyList<GroupSummary> ByStatusQuerySyntax(IEnumerable<Order> source) =>
        (from o in source
         group o by o.Status.ToString() into g
         orderby g.Sum(x => x.Total) descending
         select new GroupSummary(g.Key, g.Count(), g.Sum(x => x.Total)))
        .ToList();

    // Агрегати з обробкою порожньої послідовності
    public static (int Items, decimal Total, decimal Average, decimal Max) Totals(IEnumerable<Order> source)
    {
        var totals = source.Select(o => o.Total).ToList();   // матеріалізуємо один раз

        return totals.Count == 0
            ? (0, 0m, 0m, 0m)                                  // Average/Max на порожньому кидають виняток
            : (totals.Count, totals.Sum(), totals.Average(), totals.Max());
    }

    // GroupJoin: усі клієнти зі зведенням їхніх замовлень (клієнт без замовлень теж лишається)
    public static IReadOnlyList<CustomerOrdersSummary> CustomerOrdersReport(
        IEnumerable<CustomerDto> customers, IEnumerable<Order> orders) =>
        customers
            .GroupJoin(orders,
                       c => c.Id,
                       o => o.CustomerId,
                       (c, os) => new CustomerOrdersSummary(c.Id, c.FullName, os.Count(), os.Sum(o => o.Total)))
            .OrderByDescending(s => s.TotalSpent).ThenBy(s => s.CustomerId, StringComparer.Ordinal)
            .ToList();

    // TopBy над зведенням клієнтів
    public static IReadOnlyList<CustomerOrdersSummary> TopCustomersReport(
        IEnumerable<CustomerDto> customers,
        IEnumerable<Order> orders,
        int n) =>
        CustomerOrdersReport(customers, orders)
            .TopBy(c => c.TotalSpent, c => c.CustomerId, n)
            .ToList();
}