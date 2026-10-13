using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

// sinks необов'язковий: старий виклик new OrderService(store) працює без змін
public sealed class OrderService(IOrderStore store, IEnumerable<INotificationSink>? sinks = null)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink[] _sinks = sinks?.ToArray() ?? [];

    // Варіант 1: подія (підписники приєднуються ззовні через += / -=)
    public event EventHandler<OrderChangedEventArgs>? Changed;

    // Єдине місце сповіщення: і подія, і sink-и
    private void OnChanged(Order o, OrderChangeKind kind)
    {
        var args = new OrderChangedEventArgs(o.Id, kind, o.Lines.Count, o.Total);

        Changed?.Invoke(this, args);        // подія

        foreach (var sink in _sinks)        // Варіант 2: інтерфейс, переданий через конструктор
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

    public IReadOnlyList<Order> All() => _store.List();

    // Нормальна відсутність запису: повертає null
    public Order? Find(string id) => _store.GetById(id);

    // Відсутність запису як помилка сценарію
    public Order GetOrder(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає замовлення з id={id}.");

    // Func: критерій пошуку задає той, хто викликає, а значення повертається у відповідь
    public IReadOnlyList<Order> FindAll(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }
}