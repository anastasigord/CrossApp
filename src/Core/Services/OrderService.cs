using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class OrderService(IOrderStore store)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Order CreateOrder(string customerId)
    {
        var order = Order.Create("NEW-" + Guid.NewGuid().ToString("N")[..8], customerId);
        _store.Add(order);
        return order;
    }

    public Order AddLine(string orderId, string productId, string name, decimal price, int quantity)
    {
        var order = GetOrder(orderId);
        order.AddLine(productId, name, price, quantity); // інваріанти перевіряє сутність
        _store.Update(order);
        return order;
    }

    public Order ConfirmOrder(string orderId)
    {
        var order = GetOrder(orderId);
        order.Confirm();
        _store.Update(order);
        return order;
    }

    public IReadOnlyList<Order> All() => _store.List();

    // Нормальна відсутність запису: повертає null
    public Order? Find(string id) => _store.GetById(id);

    // Відсутність запису як помилка сценарію
    public Order GetOrder(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає замовлення з id={id}.");

    // Пошук за довільною умовою (додаткове завдання)
    public IReadOnlyList<Order> FindWhere(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }
}