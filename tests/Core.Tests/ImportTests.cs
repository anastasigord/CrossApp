using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class ImportTests
{
    // Синхронний IProgress: Progress<T> викликає обробник у пулі потоків, і порядок значень не гарантований
    private sealed class SyncProgress : IProgress<int>
    {
        public List<int> Values { get; } = [];
        public void Report(int value) => Values.Add(value);
    }

    [Fact] // 11) скасування: токен уже скасований
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new OrderService(new InMemoryOrderStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonOrderImporter(), "any.json", cts.Token));
    }

    [Fact] // 12) ідемпотентність і подія для кожного доданого
    public async Task ImportAsync_IsIdempotent_AndRaisesEventPerAddedOrder()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);
        var importer = new FakeImporter(TestData.Items());

        var first = await service.ImportAsync(importer, "any.json");
        var second = await service.ImportAsync(importer, "any.json");

        Assert.Equal(4, first);
        Assert.Equal(0, second);
        Assert.Equal(4, events.Count);
    }

    [Fact] // 13) прогрес у відсотках
    public async Task ImportAsync_ReportsProgressUpToHundredPercent()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var progress = new SyncProgress();

        await service.ImportAsync(new FakeImporter(TestData.Items()), "any.json", progress: progress);

        Assert.Equal(new[] { 25, 50, 75, 100 }, progress.Values);
    }

    [Fact] // 14) потоковий імпорт
    public async Task ImportStreamAsync_AddsAll_AndReportsCount()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var progress = new SyncProgress();

        var added = await service.ImportStreamAsync(
            new FakeImporter(TestData.Items()), "any.json", progress: progress);

        Assert.Equal(4, added);
        Assert.Equal(new[] { 1, 2, 3, 4 }, progress.Values);
    }

    [Fact] // 15) зв'язані токени: спрацював саме таймаут, а не користувач
    public async Task ImportAsync_WhenTimeoutFires_ThrowsCancelled_AndTimeoutIsTheCause()
    {
        using var user = new CancellationTokenSource();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(user.Token, timeout.Token);
        var service = new OrderService(new InMemoryOrderStore());
        var slowImporter = new FakeImporter(TestData.Items(), TimeSpan.FromSeconds(5));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(slowImporter, "any.json", linked.Token));

        Assert.True(timeout.IsCancellationRequested);
        Assert.False(user.IsCancellationRequested);
    }

    [Fact] // 16) тимчасовий зіпсований файл: DomainException з номером запису
    public async Task LoadAsync_WithBrokenRecord_ThrowsDomainException_WithRecordNumber()
    {
        string path = Path.Combine(Path.GetTempPath(), $"orders-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, """
                [
                  { "id": "X-1", "customerId": "C-1", "isConfirmed": false,
                    "lines": [ { "productId": "P-1", "name": "Кабель", "price": 10, "quantity": 2 } ] },
                  { "id": "X-2", "customerId": "C-1", "isConfirmed": false,
                    "lines": [ { "productId": "P-1", "name": "Кабель", "price": 10, "quantity": -1 } ] }
                ]
                """);

            var ex = await Assert.ThrowsAsync<DomainException>(
                () => new JsonOrderImporter().LoadAsync(path));

            Assert.Contains("Запис 2", ex.Message);
            Assert.IsAssignableFrom<ArgumentException>(ex.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }
}