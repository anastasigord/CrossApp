using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

// delayPerRecord імітує повільний ввід-вивід (потрібен для демонстрації Ctrl+C і таймауту)
public sealed class JsonOrderImporter(TimeSpan? delayPerRecord = null) : IOrderImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private readonly TimeSpan _delay = delayPerRecord ?? TimeSpan.Zero;

    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = Open(path);

        List<OrderDto?> dtos;
        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<OrderDto?>>(stream, Options, ct)
                   ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");
        }
        catch (JsonException ex)
        {
            // технічна помилка формату: загортаємо, початковий виняток зберігаємо
            throw new StorageException($"Файл {Path.GetFileName(path)} містить некоректний JSON.", ex);
        }

        var result = new List<Order>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await PauseAsync(ct);
            result.Add(ToOrder(dtos[i], i + 1));
        }
        return result;
    }

    public async IAsyncEnumerable<Order> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = Open(path);
        await using var enumerator = JsonSerializer
            .DeserializeAsyncEnumerable<OrderDto>(stream, Options, ct)
            .GetAsyncEnumerator(ct);

        var number = 0;
        while (true)
        {
            OrderDto? dto = null;
            bool hasNext;
            try
            {
                hasNext = await enumerator.MoveNextAsync();
                if (hasNext)
                    dto = enumerator.Current;
            }
            catch (JsonException ex)
            {
                throw new StorageException($"Файл {Path.GetFileName(path)} містить некоректний JSON.", ex);
            }

            if (!hasNext)
                yield break;

            ct.ThrowIfCancellationRequested();
            await PauseAsync(ct);
            number++;
            yield return ToOrder(dto, number);
        }
    }

    // Справжній асинхронний ввід-вивід: useAsync: true
    private static FileStream Open(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

    private Task PauseAsync(CancellationToken ct) =>
        _delay > TimeSpan.Zero ? Task.Delay(_delay, ct) : Task.CompletedTask;

    // Помилки валідації запису -> DomainException з номером запису
    private static Order ToOrder(OrderDto? dto, int number)
    {
        try
        {
            return Order.FromDto(dto!);
        }
        catch (ArgumentException ex)
        {
            throw new DomainException($"Запис {number}: {ex.Message}", ex);
        }
        catch (DomainException ex)
        {
            throw new DomainException($"Запис {number}: {ex.Message}", ex);
        }
    }
}