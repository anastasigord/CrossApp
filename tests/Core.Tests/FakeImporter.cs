using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Domain;

namespace Core.Tests;

// Імпортер у пам'яті: без диска, з керованою затримкою
internal sealed class FakeImporter(IReadOnlyList<Order> items, TimeSpan delay = default) : IOrderImporter
{
    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, ct);
        return items;
    }

    public async IAsyncEnumerable<Order> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ct);
            yield return item;
        }
    }
}