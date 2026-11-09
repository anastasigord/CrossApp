using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cli;
using Cli.Sinks;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = Encoding.UTF8;

// Прапорці зі значенням: значення (шлях) не є командою
var valueFlags = new[] { "--import", "--export" };
var positional = new List<string>();
for (int i = 0; i < args.Length; i++)
{
    if (valueFlags.Contains(args[i])) { i++; continue; }
    if (args[i].StartsWith("--")) continue;
    positional.Add(args[i]);
}
string command = positional.FirstOrDefault() ?? "report-top";
string? importPath = OptionValue(args, "--import");
string? exportPath = OptionValue(args, "--export");

if ((args.Contains("--import") && importPath is null) || (args.Contains("--export") && exportPath is null))
{
    Console.WriteLine("Вкажіть шлях до файлу: --import <файл> або --export <файл>");
    return 1;
}

// ---------- Конфігурація (ЛР 9): appsettings, secrets, змінні середовища ----------
AppSettings settings;
try
{
    settings = AppSettings.Load(args);
}
catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
{
    Console.WriteLine($"Помилка конфігурації: {ex.Message}");
    return 1;
}

// ---------- Імпорт / експорт колекції в JSON (ЛР 9) ----------
if (importPath is not null || exportPath is not null)
    return await SettingsCommands.RunDataAsync(settings, importPath, exportPath);

// ---------- Composition root: сховище, шлях до файлу з конфігурації ----------
IOrderStore store = StoreFactory.Create(args, settings.DataFile);
var service = new OrderService(store);

// Зіпсований файл даних: зрозуміле повідомлення замість стектрейсу
try
{
    _ = service.All();
}
catch (StorageException ex)
{
    Console.WriteLine($"Помилка сховища: {ex.Message}");
    if (ex.InnerException is not null)
        Console.WriteLine($"  Причина: {ex.InnerException.Message}");
    return 1;
}

if (command == "config")
{
    SettingsCommands.PrintSettings(settings, includeLogLevel: true);
    return 0;
}

// ---------- Асинхронний імпорт (ЛР 8): команда import ----------
if (command == "import")
    return await ImportCommand.RunAsync(service, args, positional.Skip(1).FirstOrDefault());

// ---------- Звіти (ЛР 7) ----------
if (command != "events")
    return ReportCommands.Run(command, service.All());

// =====================================================================
// Нижче сценарій ЛР 6 (події). Запускається командою: events
// =====================================================================
bool useFile = args.Contains("--file");
bool useCache = args.Contains("--cache");
bool useSearch = args.Contains("--search");

Console.WriteLine("Створення сховища: StoreFactory.Create(args)");
Console.WriteLine($"Сховище: {store.GetType().Name}");
if (useCache)
    Console.WriteLine($"Обгортає: {(useFile ? nameof(FileOrderStore) : nameof(InMemoryOrderStore))}");
if (useFile)
    Console.WriteLine($"Файл: {settings.DataFile}");
Console.WriteLine($"Замовлень у сховищі на старті: {service.All().Count}");
Console.WriteLine();

// ---------- Лог: шлях через Path.Combine, каталог створюємо кодом ----------
string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
File.Delete(logPath); // свіжий лог на кожен запуск (для демонстрації)

// ---------- Підписник 1: локальний метод (його можна відписати) ----------
void OnOrderChanged(object? sender, OrderChangedEventArgs e)
{
    string line = $"{e.At:HH:mm:ss} {e.Kind,-14} {e.OrderId} рядків: {e.LinesCount,2} -> сума {e.Total:F2}";
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}
service.Changed += OnOrderChanged;

// ---------- Підписник 2: лямбда із замиканням над змінною changes ----------
int changes = 0;
service.Changed += (_, _) => changes++;

// ---------- Створення, рядки, підтвердження (кожна операція дає рівно одну подію) ----------
Console.WriteLine("Успішні операції (події):");
var created = service.CreateOrder("C-200");
Console.WriteLine($"Створено замовлення {created.Id} (клієнт {created.CustomerId})");

var o = service.AddLine(created.Id, "P-101", "Кабель UTP cat6", 25m, 50);
Console.WriteLine($"Додано рядок: {o.Lines[^1]}");
o = service.AddLine(created.Id, "P-102", "Роз'єм RJ-45", 3.5m, 100);
Console.WriteLine($"Додано рядок: {o.Lines[^1]}");

service.ConfirmOrder(created.Id);
Console.WriteLine($"Підтверджено: {service.GetOrder(created.Id)}");
Console.WriteLine($"Подій отримано: {changes}");
Console.WriteLine();

Console.WriteLine("Список замовлень:");
foreach (var order in service.All())
    Console.WriteLine($"  {order}");
Console.WriteLine();

// ---------- Пошук за id ----------
Console.WriteLine("Пошук за id:");
Console.WriteLine($"  {created.Id}: {service.Find(created.Id)?.ToString() ?? "не знайдено"}");
Console.WriteLine($"  NEW-nope: {service.Find("NEW-nope")?.ToString() ?? "не знайдено"}");
Console.WriteLine();

// ---------- Сценарій відмови: текст помилки є, події немає ----------
var draft = service.CreateOrder("C-202"); // порожня чернетка (це ще одна подія)
int beforeFailures = changes;

Console.WriteLine();
Console.WriteLine("Обробка помилок (нових подій бути не повинно):");
TryDo("від'ємна кількість",
    () => service.AddLine(draft.Id, "P-1", "Товар", 10m, -5));
TryDo("підтвердження порожнього замовлення",
    () => service.ConfirmOrder(draft.Id));
TryDo("рядок у неіснуюче замовлення",
    () => service.AddLine("NEW-nope", "P-1", "Товар", 10m, 1));
TryDo("дубль id у сховищі",
    () => store.Add(Order.Create(created.Id, "C-999")));
TryDo("рядок у підтверджене замовлення",
    () => service.AddLine(created.Id, "P-1", "Товар", 10m, 1));
TryDo("повторне підтвердження",
    () => service.ConfirmOrder(created.Id));
Console.WriteLine($"Нових подій після відмов: {changes - beforeFailures}");
Console.WriteLine();

// ---------- Відписка: дані змінюються, повідомлень у консолі й логу немає ----------
Console.WriteLine("Відписка (-=):");
int beforeUnsubscribe = changes;
service.Changed -= OnOrderChanged;
service.AddLine(draft.Id, "P-2", "Товар", 10m, 1);
Console.WriteLine($"  Рядків у чернетці {draft.Id}: {service.GetOrder(draft.Id).Lines.Count}");
Console.WriteLine($"  Подій отримано: {changes} (+{changes - beforeUnsubscribe}: лише лямбда-лічильник)");
Console.WriteLine();

// ---------- Func: делегат, коли потрібне значення у відповідь ----------
Console.WriteLine("Пошук за делегатом Func<Order, bool>:");
PrintFound("Сума більша за 1000", service.FindAll(ord => ord.Total > 1000m));
Console.WriteLine();

if (useSearch)
{
    string prefix = "O-";
    Func<Order, bool> byPrefix = ord => ord.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    Func<Order, bool> confirmedOnly = ord => ord.IsConfirmed;

    PrintFound("Id починається з O-", service.FindAll(byPrefix));
    PrintFound("Лише підтверджені", service.FindAll(confirmedOnly));
    PrintFound("Id починається з NEW- і підтверджене",
        service.FindAll(ord => ord.Id.StartsWith("NEW-", StringComparison.OrdinalIgnoreCase)
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
    Console.WriteLine();
}

// ---------- Додаткові завдання ЛР 6 (лише з --extra) ----------
if (args.Contains("--extra"))
{
    Console.WriteLine("Додаткове завдання 1: INotificationSink (ConsoleSink + FileSink)");
    string sinkLog = Path.Combine(AppContext.BaseDirectory, "logs", "sinks.log");
    File.Delete(sinkLog);

    var sinkService = new OrderService(
        new InMemoryOrderStore(),
        new INotificationSink[] { new ConsoleSink(), new FileSink(sinkLog) });

    var sinkOrder = sinkService.CreateOrder("C-300");
    sinkService.AddLine(sinkOrder.Id, "P-1", "Товар", 100m, 2);
    sinkService.ConfirmOrder(sinkOrder.Id);
    Console.WriteLine($"  Рядків у {sinkLog}: {File.ReadAllLines(sinkLog).Length}");
    Console.WriteLine();

    Console.WriteLine("Додаткове завдання 2: Action<string> в імпорті");
    string csvPath = Path.Combine("data", "sample.csv");
    if (File.Exists(csvPath))
    {
        int skippedCount = 0;
        var imported = CsvImporter.Load(csvPath, message =>
        {
            skippedCount++;
            Console.WriteLine($"  пропущено → {message}");
        });
        Console.WriteLine($"  Імпортовано: {imported.Items.Count}, повідомлень через Action: {skippedCount}");
    }
    else
    {
        Console.WriteLine($"  Файл не знайдено: {Path.GetFullPath(csvPath)} (запускайте з кореня репозиторію)");
    }
    Console.WriteLine();
}

Console.WriteLine($"Лог: {logPath}");
Console.WriteLine($"Рядків у логу: {File.ReadAllLines(logPath).Length}");
return 0;

static string? OptionValue(string[] a, string name)
{
    int idx = Array.IndexOf(a, name);
    return idx >= 0 && idx + 1 < a.Length && !a[idx + 1].StartsWith("--") ? a[idx + 1] : null;
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