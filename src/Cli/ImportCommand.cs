using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Core.Abstractions;
using Core.Import;
using Core.Services;

namespace Cli;

public static class ImportCommand
{
    private static readonly TimeSpan TimeoutAfter = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SlowDelay = TimeSpan.FromMilliseconds(300);

    public static async Task<int> RunAsync(OrderService service, string[] args, string? pathArg)
    {
        string path = pathArg ?? Path.Combine("data", "orders-import.json");
        bool stream = args.Contains("--stream");
        bool useTimeout = args.Contains("--timeout");
        bool slow = useTimeout || args.Contains("--slow");

        // Ctrl+C -> userCts, таймаут -> timeoutCts; в імпорт іде зв'язаний токен
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);

        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            e.Cancel = true;   // не вбиваємо процес, а просимо імпорт зупинитись
            userCts.Cancel();
        };
        Console.CancelKeyPress += onCancel;

        if (useTimeout)
            timeoutCts.CancelAfter(TimeoutAfter);

        TimeSpan? delay = slow ? SlowDelay : null;
        IOrderImporter importer = new JsonOrderImporter(delay);
        var progress = new ConsoleProgress(showCount: stream);

        try
        {
            int added = stream
                ? await service.ImportStreamAsync(importer, path, linkedCts.Token, progress)
                : await service.ImportAsync(importer, path, linkedCts.Token, progress);

            progress.EndLine();
            Console.WriteLine($"Імпортовано записів: {added}");
            return 0;
        }
        // Порядок важливий: скасування перед усіма іншими, воно не є помилкою
        catch (OperationCanceledException)
        {
            progress.EndLine();
            Console.WriteLine("Імпорт скасовано.");
            Console.WriteLine(timeoutCts.IsCancellationRequested && !userCts.IsCancellationRequested
                ? "Причина: таймаут (1 с)"
                : "Причина: Ctrl+C");
            return 2;
        }
        catch (DomainException ex)
        {
            progress.EndLine();
            Console.WriteLine($"Дані відхилено: {ex.Message}");
            return 1;
        }
        catch (IOException ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            Console.WriteLine($"Файл не знайдено: {path}");
            return 1;
        }
        catch (StorageException ex)
        {
            Console.WriteLine($"Технічна помилка: {ex.Message}");
            if (ex.InnerException is not null)
                Console.WriteLine($"  Причина: {ex.InnerException.Message}");
            return 1;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Помилка введення-виведення: {ex.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }
    }

    // Синхронний IProgress (Progress<T> викликав би обробник у пулі потоків, і рядки могли б переплутатись)
    private sealed class ConsoleProgress(bool showCount) : IProgress<int>
    {
        public bool Started { get; private set; }

        public void Report(int value)
        {
            Started = true;
            Console.Write(showCount ? $"\rІмпорт: {value} записів" : $"\rІмпорт: {value}%");
        }

        public void EndLine()
        {
            if (Started)
                Console.WriteLine();
        }
    }
}