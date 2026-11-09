using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Domain;

namespace Core.Abstractions;

public interface IOrderImporter
{
    Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default);

    // Потокове читання: замовлення віддаються по одному
    IAsyncEnumerable<Order> StreamAsync(string path, CancellationToken ct = default);
}