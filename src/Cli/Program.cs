using System;
using Core.Domain;
using Core.Import;

Console.WriteLine("=== Сценарій 1: успіх ===");
Order order = Order.Create("O-001", "C-100");
order.AddLine("P-001", "Цемент М400 25кг", 180.50m, 10);
order.AddLine("P-002", "Пісок", 95m, 3);
Console.WriteLine(order);
foreach (var line in order.Lines)
    Console.WriteLine($"  {line}");
order.Confirm();
Console.WriteLine(order);
Console.WriteLine();

Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
Order draft = Order.Create("O-002", "C-200");
draft.AddLine("P-003", "Цегла", 12m, 100);
decimal totalBefore = draft.Total;

TryDo("нульова кількість", () => draft.AddLine("P-004", "Щебінь", 50m, 0));
TryDo("від'ємна ціна", () => draft.AddLine("P-004", "Щебінь", -50m, 5));
TryDo("порожній ідентифікатор товару", () => draft.AddLine(" ", "Щебінь", 50m, 5));
TryDo("порожній ідентифікатор клієнта", () => Order.Create("O-003", " "));
TryDo("підтвердження порожнього замовлення", () => Order.Create("O-004", "C-300").Confirm());
TryDo("рядок у підтверджене замовлення", () => order.AddLine("P-005", "Гравій", 70m, 2));
TryDo("повторне підтвердження", () => order.Confirm());

Console.WriteLine();
Console.WriteLine($"Після всіх відмов: рядків {draft.Lines.Count}, сума {draft.Total} " +
                  $"(до спроб було {totalBefore}) — стан не змінився.");
Console.WriteLine();

Console.WriteLine("=== Сценарій 3: імпорт товарів у замовлення ===");
var import = JsonImporter.Load("data/jsample.json");
var outcome = OrderImportMapper.ToOrder(import, "O-IMPORT", "C-100", quantityPerLine: 2);

if (outcome.Order is not null)
{
    Console.WriteLine(outcome.Order);
    foreach (var line in outcome.Order.Lines)
        Console.WriteLine($"  {line}");
}

Console.WriteLine($"Не пройшли перевірки: {outcome.Errors.Count}");
foreach (var err in outcome.Errors)
    Console.WriteLine($"  {err}");

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}