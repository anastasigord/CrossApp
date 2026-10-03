using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemoryOrderStore(IEnumerable<Order>? seed = null) : IOrderStore
{
    private readonly Dictionary<string, Order> _items =
        (seed ?? Enumerable.Empty<Order>()).ToDictionary(o => o.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Order> List() => _items.Values.ToList();

    public Order? GetById(string id) => _items.GetValueOrDefault(id);

    public void Add(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Замовлення з id={item.Id} уже існує.");
        _items.Add(item.Id, item);
    }

    public void Update(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Немає замовлення з id={item.Id}.");
        _items[item.Id] = item;
    }

    public bool Remove(string id) => _items.Remove(id);
}