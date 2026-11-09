using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class FileOrderStore(string path) : IOrderStore
{
    private readonly Dictionary<string, Order> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;

        foreach (var order in JsonOrderFile.Load(_path))   // ті самі перевірки, що й у домені
            _cache[order.Id] = order;

        _loaded = true;   // лише після успішного читання
    }

    private void Flush() => JsonOrderFile.Save(_path, _cache.Values);

    public IReadOnlyList<Order> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Order? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Замовлення з id={item.Id} уже існує.");
        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (!_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Немає замовлення з id={item.Id}.");
        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }
}