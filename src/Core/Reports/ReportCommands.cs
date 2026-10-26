using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core;
using Core.Domain;
using Core.Reports;

namespace Cli;

public static class ReportCommands
{
    public const string Names =
        "report-top | report-low | report-groups | report-sum | demo-deferred | " +
        "report-join | report-customers | report-empty | benchmark | events";

    public static int Run(string command, IReadOnlyList<Order> items)
    {
        switch (command)
        {
            case "report-top":
                PrintOrders(ReportService.TopByTotal(items, 5));
                break;

            case "report-low":
                PrintOrders(ReportService.LowValueOrders(items, 1000m));
                break;

            case "report-groups":
                foreach (var g in ReportService.ByStatus(items))
                    Console.WriteLine($" {g.Key,-12} замовлень: {g.Count,3} сумарно: {g.TotalAmount,10:F2} грн");
                break;

            case "report-sum":
            {
                var t = ReportService.Totals(items);
                Console.WriteLine(
                    $" позицій {t.Items}, всього {t.Total:F2}, середнє {t.Average:F2}, макс {t.Max:F2}");
                break;
            }

            case "demo-deferred":
                DeferredDemo(items);
                break;

            case "report-join":
                PrintCustomers("=== GroupJoin (Клієнти та Замовлення) ===",
                    ReportService.CustomerOrdersReport(SampleData.Customers(), items));
                break;

            case "report-customers":
                PrintCustomers("=== Топ-3 клієнти (TopBy) ===",
                    ReportService.TopCustomersReport(SampleData.Customers(), items, 3));
                break;

            case "report-empty":
                EmptyDemo();
                break;

            case "benchmark":
                Benchmark();
                break;

            default:
                Console.WriteLine(Names);
                return 1;
        }

        return 0;
    }

    private static void PrintOrders(IReadOnlyList<OrderReportLine> lines)
    {
        Console.WriteLine($" {"Id",-9} {"CustomerId",-14} {"Сума (грн)",10} {"Статус",-12} {"Позицій",8}");
        Console.WriteLine(new string('-', 58));
        foreach (var l in lines)
            Console.WriteLine($" {l.Id,-9} {l.CustomerId,-14} {l.Total,10:F2} {l.Status,-12} {l.LinesCount,8}");
    }

    private static void PrintCustomers(string title, IReadOnlyList<CustomerOrdersSummary> rows)
    {
        Console.WriteLine(title);
        Console.WriteLine($" {"Id",-6} {"Ім'я клієнта",-22} {"Замовлень",9}   {"Сума покупок (грн)"}");
        Console.WriteLine(new string('-', 58));
        foreach (var c in rows)
            Console.WriteLine($" {c.CustomerId,-6} {c.Name,-22} {c.OrdersCount,9}   {c.TotalSpent,18:F2}");
    }

    // Демонстрація відкладеного виконання: запит виконується під час перебору, а не під час оголошення
    private static void DeferredDemo(IReadOnlyList<Order> items)
    {
        Console.WriteLine("=== Демонстрація відкладеного виконання (Deferred Execution) ===");

        var source = items.ToList();                         // локальна копія, яку змінюватимемо
        var query = source.Where(ord => ord.Total < 1000m);  // запит ПОБУДОВАНО, але не виконано

        Console.WriteLine($"Малих замовлень (< 1000 грн) до змін: {query.Count()}");

        var small = Order.Create("O-999", "C-001");
        small.AddLine("P-999", "Скоба будівельна", 50m, 6);  // 300 грн
        source.Add(small);                                   // джерело змінили ПІСЛЯ побудови запиту

        Console.WriteLine($"Малих замовлень після змін: {query.Count()}");

        var snapshot = query.ToList();                       // результат ЗАФІКСОВАНО
        source.Clear();
        Console.WriteLine($"У знімку лишилось: {snapshot.Count}");
    }

    // Порожнє джерело не повинно ламати звіти
    private static void EmptyDemo()
    {
        var empty = new List<Order>();
        Console.WriteLine("=== Порожнє джерело ===");
        Console.WriteLine($" TopByTotal:      {ReportService.TopByTotal(empty, 5).Count} рядків");
        Console.WriteLine($" LowValueOrders:  {ReportService.LowValueOrders(empty, 1000m).Count} рядків");
        Console.WriteLine($" ByStatus:        {ReportService.ByStatus(empty).Count} груп");
        var t = ReportService.Totals(empty);
        Console.WriteLine($" Totals: позицій {t.Items}, всього {t.Total:F2}, середнє {t.Average:F2}, макс {t.Max:F2}");
    }

    private static void Benchmark()
    {
        Console.WriteLine("Генерація 100 000 замовлень для бенчмарку...");
        var big = BenchmarkData.GenerateOrders(100_000);

        // прогрів JIT: у вимір не входить
        BenchmarkData.TopLazy(big, 500m, 10);
        BenchmarkData.TopWithMiddleToList(big, 500m, 10);

        var sw = Stopwatch.StartNew();
        BenchmarkData.TopLazy(big, 500m, 10);
        sw.Stop();
        long lazyMs = sw.ElapsedMilliseconds;
        long lazyTicks = sw.ElapsedTicks;

        sw.Restart();
        BenchmarkData.TopWithMiddleToList(big, 500m, 10);
        sw.Stop();
        long midMs = sw.ElapsedMilliseconds;
        long midTicks = sw.ElapsedTicks;

        Console.WriteLine();
        Console.WriteLine("=== Результати вимірювання Stopwatch (100 000 записів) ===");
        Console.WriteLine($"1. Без ToList у середині ланцюжка:   {lazyMs,4} мс ({lazyTicks,9:N0} тіків)");
        Console.WriteLine($"2. З проміжними ToList() у середині: {midMs,4} мс ({midTicks,9:N0} тіків)");

        string faster = lazyTicks <= midTicks ? "без ToList" : "з проміжними ToList()";
        double ratio = (double)Math.Max(lazyTicks, midTicks) / Math.Max(1, Math.Min(lazyTicks, midTicks));
        Console.WriteLine($"Різниця: варіант {faster} швидший приблизно у {ratio:F2} раза");
    }
}