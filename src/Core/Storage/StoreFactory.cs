using System;
using System.IO;
using System.Linq;
using Core.Abstractions;

namespace Core.Storage;

public static class StoreFactory
{
    // filePath приходить із конфігурації (appsettings / змінні середовища)
    public static IOrderStore Create(string[] args, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(args);

        IOrderStore store = args.Contains("--file")
            ? new FileOrderStore(filePath ?? Path.Combine(AppContext.BaseDirectory, "data", "orders.json"))
            : new InMemoryOrderStore(SampleData.Orders());

        return args.Contains("--cache") ? new CachingOrderStore(store) : store;
    }
}