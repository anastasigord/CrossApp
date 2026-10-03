using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = Encoding.UTF8;

// ---------- Composition root: вибір сховища через фабрику ----------
IOrderStore store = StoreFactory.Create(args);

// Те саме без фабрики (для порівняння у звіті):
// IOrderStore store = args.Contains("--file")
//     ? new FileOrderStore(Path.Combine(AppContext.BaseDirectory, "data", "orders.json"))
//     : new InMemoryOrderStore(SampleData.Orders());
// if (args.Contains("--cache")) store = new CachingOrderStore(store);

var service = new OrderService(store);

bool useFile = args.Contains("--file");
bool useCache = args.Contains("--cache");
bool useSearch = args.Contains("--search");

Console.WriteLine("Створення сховища: StoreFactory.Create(args)");
Console.WriteLine($"Сховище: {store.GetType().Name}");
if (useCache)
    Console.WriteLine($"Обгортає: {(useFile ? nameof(FileOrderStore) : nameof(InMemoryOrderStore))}");
if (useFile)
    Console.WriteLine($"Файл: {Path.Combine(AppContext.BaseDirectory, "data", "orders.json")}");
Console.WriteLine($"Замовлень у сховищі на старті: {service.All().Count}");
Console.WriteLine();

// ---------- Створення, рядки, підтвердження ----------
var created = service.CreateOrder("C-200");
Console.WriteLine($"Створено замовлення {created.Id} (клієнт {created.CustomerId})");

var o = service.AddLine(created.Id, "P-101", "Кабель UTP cat6", 25m, 50);
Console.WriteLine($"Додано рядок: {o.Lines[^1]}");
o = service.AddLine(created.Id, "P-102", "Роз'єм RJ-45", 3.5m, 100);
Console.WriteLine($"Додано рядок: {o.Lines[^1]}");
Console.WriteLine();

Console.WriteLine("Список замовлень:");
foreach (var order in service.All())
    Console.WriteLine($"  {order}");
Console.WriteLine();

service.ConfirmOrder(created.Id);
Console.WriteLine($"Підтверджено: {service.GetOrder(created.Id)}");
Console.WriteLine();

// ---------- Пошук за id ----------
Console.WriteLine("Пошук за id:");
Console.WriteLine($"  {created.Id}: {service.Find(created.Id)?.ToString() ?? "не знайдено"}");
Console.WriteLine($"  NEW-nope: {service.Find("NEW-nope")?.ToString() ?? "не знайдено"}");
Console.WriteLine();

// ---------- Обробка помилок ----------
Console.WriteLine("Обробка помилок:");
TryDo("рядок у неіснуюче замовлення",
    () => service.AddLine("NEW-nope", "P-1", "Товар", 10m, 1));
TryDo("дубль id у сховищі",
    () => store.Add(Order.Create(created.Id, "C-999")));
TryDo("рядок у підтверджене замовлення",
    () => service.AddLine(created.Id, "P-1", "Товар", 10m, 1));
TryDo("повторне підтвердження",
    () => service.ConfirmOrder(created.Id));
Console.WriteLine();

// ---------- Пошук за делегатом Func<Order, bool> (лише з --search) ----------
if (useSearch)
{
    Console.WriteLine("Пошук за делегатом Func<Order, bool>:");

    string prefix = "ORD-";
    Func<Order, bool> byPrefix = ord => ord.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    Func<Order, bool> confirmedOnly = ord => ord.IsConfirmed;
    Func<Order, bool> expensive = ord => ord.Total > 1000m;

    PrintFound("Id починається з ORD-", service.FindWhere(byPrefix));
    PrintFound("Лише підтверджені", service.FindWhere(confirmedOnly));
    PrintFound("Сума більша за 1000", service.FindWhere(expensive));
    PrintFound("Id починається з NEW- і підтверджене",
        service.FindWhere(ord => ord.Id.StartsWith("NEW-", StringComparison.OrdinalIgnoreCase)
                                 && ord.IsConfirmed));
    Console.WriteLine();
}

// ---------- Кеш (лише з --cache) ----------
if (store is CachingOrderStore cache)
{
    Console.WriteLine("Кеш:");
    service.CreateOrder("C-201"); // зміна скидає кеш
    int before = cache.InnerListCalls;
    service.All(); service.All(); service.All();
    Console.WriteLine("  Нове замовлення додано, кеш скинуто");
    Console.WriteLine($"  Три читання списку підряд: звернень до внутрішнього сховища {cache.InnerListCalls - before}");
}

static void PrintFound(string title, IReadOnlyList<Order> found)
{
    Console.WriteLine($"  {title}: {found.Count}");
    foreach (var f in found)
        Console.WriteLine($"    {f}");
}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}