using System;
using System.IO;
using System.Threading.Tasks;
using Core;
using Core.Import;
using Core.Services;
using Core.Storage;

namespace Cli;

public static class SettingsCommands
{
    // Заголовок: рівно ті рядки, що на скриншотах звіту.
    // Рівень логу показує лише команда config (для порівняння профілів).
    public static void PrintSettings(AppSettings s, bool includeLogLevel = false)
    {
        Console.WriteLine($"Середовище: {s.EnvironmentName}");
        Console.WriteLine("Формат: json");
        Console.WriteLine($"Валюта: {s.Options.Orders.Currency}");
        if (includeLogLevel)
            Console.WriteLine($"Рівень логу: {s.Options.Logging.MinimumLevel}");
        Console.WriteLine($"Файл замовлень: {s.DataFile}");

        // Значення секрету НІКОЛИ не друкуємо: лише факт наявності й довжина
        Console.WriteLine(s.ApiKey is null
            ? "ApiKey: не налаштований"
            : $"ApiKey: налаштований ({s.ApiKey.Length} символів)");
    }

    // --import <файл> та/або --export <файл>: працює з файлом замовлень із конфігурації
    public static async Task<int> RunDataAsync(AppSettings s, string? importPath, string? exportPath)
    {
        PrintSettings(s);

        var service = new OrderService(new FileOrderStore(s.DataFile));

        try
        {
            if (importPath is not null)
            {
                int added = await service.ImportAsync(new JsonOrderImporter(), importPath);
                Console.WriteLine($"Імпортовано замовлень: {added}");
            }

            if (exportPath is not null)
            {
                var orders = service.All();
                JsonOrderFile.Save(exportPath, orders);
                Console.WriteLine($"Експортовано замовлень: {orders.Count}");
            }

            var inStore = service.All();
            Console.WriteLine($"Замовлень у сховищі: {inStore.Count}");
            Console.WriteLine(JsonOrderFile.ToJson(inStore));
            return 0;
        }
        catch (DomainException ex)
        {
            Console.WriteLine($"Дані відхилено: {ex.Message}");
            return 1;
        }
        catch (IOException ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            Console.WriteLine($"Файл не знайдено: {importPath}");
            return 1;
        }
        catch (StorageException ex)
        {
            Console.WriteLine($"Помилка сховища: {ex.Message}");
            if (ex.InnerException is not null)
                Console.WriteLine($"  Причина: {ex.InnerException.Message}");
            return 1;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Помилка введення-виведення: {ex.Message}");
            return 1;
        }
    }
}