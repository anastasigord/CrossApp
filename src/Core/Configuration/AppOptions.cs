using System;
using System.IO;

namespace Core.Configuration;

public sealed class AppOptions
{
    public StorageOptions Storage { get; init; } = new();
    public OrdersOptions Orders { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public string EnvironmentName { get; init; } = "Development";
}

public sealed class OrdersOptions
{
    public string Currency { get; init; } = "UAH";
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; init; } = "Information";
}

public sealed class StorageOptions
{
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "orders.json";

    // Повний шлях до файлу даних. Каталог створюється, якщо його немає.
    public string ResolveFullPath(string baseDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDir);

        // порожній DataPath: системний каталог локальних даних користувача;
        // відносний шлях відраховується від baseDir, а не від поточного каталогу процесу
        string root = string.IsNullOrWhiteSpace(DataPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrossApp")
            : Path.IsPathRooted(DataPath) ? DataPath : Path.Combine(baseDir, DataPath);

        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }
}