using System;
using System.Collections.Generic;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private IReadOnlyList<Order>? _listCache;

    // Лічильник для демонстрації: скільки разів справді звертались до внутрішнього сховища
    public int InnerListCalls { get; private set; }

    public IReadOnlyList<Order> List()
    {
        if (_listCache is null)
        {
            _listCache = _inner.List();
            InnerListCalls++;
        }
        return _listCache;
    }

    public Order? GetById(string id) => _inner.GetById(id);

    public void Add(Order item)
    {
        _inner.Add(item);
        _listCache = null; // скидання кешу після зміни
    }

    public void Update(Order item)
    {
        _inner.Update(item);
        _listCache = null;
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        _listCache = null;
        return removed;
    }
}