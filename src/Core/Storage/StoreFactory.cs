using System;
using System.IO;
using System.Linq;
using Core.Abstractions;

namespace Core.Storage;

public static class StoreFactory
{
    public static IOrderStore Create(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        IOrderStore store = args.Contains("--file")
            ? new FileOrderStore(Path.Combine(AppContext.BaseDirectory, "data", "orders.json"))
            : new InMemoryOrderStore(SampleData.Orders());

        return args.Contains("--cache") ? new CachingOrderStore(store) : store;
    }
}