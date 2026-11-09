using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

// sinks необов'язковий: старий виклик new OrderService(store) працює без змін
public sealed class OrderService(IOrderStore store, IEnumerable<INotificationSink>? sinks = null)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink[] _sinks = sinks?.ToArray() ?? [];

    // event, а не публічне поле: ззовні лише += і -=
    public event EventHandler<OrderChangedEventArgs>? Changed;

    // Єдине місце сповіщення: і подія, і sink-и
    private void OnChanged(Order o, OrderChangeKind kind)
    {
        var args = new OrderChangedEventArgs(o.Id, kind, o.Lines.Count, o.Total);

        Changed?.Invoke(this, args);

        foreach (var sink in _sinks)
            sink.Notify(args);
    }

    public Order CreateOrder(string customerId)
    {
        var order = Order.Create("NEW-" + Guid.NewGuid().ToString("N")[..8], customerId);
        _store.Add(order);
        OnChanged(order, OrderChangeKind.OrderCreated); // після зміни, не до
        return order;
    }

    public Order AddLine(string orderId, string productId, string name, decimal price, int quantity)
    {
        var order = GetOrder(orderId);
        order.AddLine(productId, name, price, quantity); // інваріанти: при винятку сповіщення не буде
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded);
        return order;
    }

    public Order ConfirmOrder(string orderId)
    {
        var order = GetOrder(orderId);
        order.Confirm(); // інваріанти: при винятку сповіщення не буде
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderConfirmed);
        return order;
    }

    // Імпорт: токен передається в LoadAsync і перевіряється в циклі.
    // Ідемпотентність: замовлення, яке вже є в сховищі, пропускається.
    public async Task<int> ImportAsync(
        IOrderImporter importer, string path,
        CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var items = await importer.LoadAsync(path, ct);

        var added = 0;
        var processed = 0;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            if (_store.GetById(item.Id) is null)
            {
                _store.Add(item);
                OnChanged(item, OrderChangeKind.OrderCreated); // подія для кожного доданого
                added++;
            }

            processed++;
            progress?.Report(processed * 100 / items.Count); // відсотки
        }
        return added;
    }

    // Потоковий імпорт: загальна кількість невідома, тому прогрес = кількість оброблених
    public async Task<int> ImportStreamAsync(
        IOrderImporter importer, string path,
        CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var added = 0;
        var processed = 0;
        await foreach (var item in importer.StreamAsync(path, ct).WithCancellation(ct))
        {
            if (_store.GetById(item.Id) is null)
            {
                _store.Add(item);
                OnChanged(item, OrderChangeKind.OrderCreated);
                added++;
            }

            processed++;
            progress?.Report(processed);
        }
        return added;
    }

    public IReadOnlyList<Order> All() => _store.List();

    // Нормальна відсутність запису: повертає null
    public Order? Find(string id) => _store.GetById(id);

    // Відсутність запису як порушення бізнес-правила
    public Order GetOrder(string id) =>
        _store.GetById(id) ?? throw new DomainException($"Немає замовлення з id={id}.");

    // Func: критерій пошуку задає той, хто викликає
    public IReadOnlyList<Order> FindAll(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }
}