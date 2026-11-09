using System;
using System.Collections.Generic;
using Core.Events;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class OrderServiceTests
{
    [Fact] // 1) інваріант: відмова і незмінний стан
    public void AddLine_WithNegativeQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);
        var order = service.CreateOrder("C-1");
        service.AddLine(order.Id, "P-1", "Кабель", 10m, 2);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.AddLine(order.Id, "P-2", "Гвинт", 5m, -5));

        Assert.Single(store.GetById(order.Id)!.Lines);
    }

    [Fact] // 2) подія як спосіб перевірки
    public void CreateOrder_RaisesChangedExactlyOnce()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.CreateOrder("C-1");

        Assert.Single(events);
        Assert.Equal(OrderChangeKind.OrderCreated, events[0].Kind);
    }

    [Fact] // 3) операція, що впала на інваріанті, події не створює
    public void FailedOperation_DoesNotRaiseChanged()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var order = service.CreateOrder("C-1");
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        Assert.Throws<DomainException>(() => service.ConfirmOrder(order.Id));

        Assert.Empty(events);
    }

    [Fact] // 4) бізнес-правило: порожнє замовлення не підтверджується
    public void ConfirmOrder_WhenEmpty_ThrowsDomainException()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var order = service.CreateOrder("C-1");

        Assert.Throws<DomainException>(() => service.ConfirmOrder(order.Id));
    }

    [Fact] // 5) бізнес-правило: підтверджене замовлення не змінюється
    public void AddLine_ToConfirmedOrder_ThrowsDomainException()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var order = service.CreateOrder("C-1");
        service.AddLine(order.Id, "P-1", "Кабель", 10m, 1);
        service.ConfirmOrder(order.Id);

        Assert.Throws<DomainException>(
            () => service.AddLine(order.Id, "P-2", "Гвинт", 5m, 1));
    }

    [Fact] // 6) сума замовлення після додавання рядків
    public void Total_AfterAddingLines_IsSumOfSubtotals()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var order = service.CreateOrder("C-1");

        service.AddLine(order.Id, "P-1", "Кабель", 10m, 3);    // 30
        service.AddLine(order.Id, "P-2", "Гвинт", 2.5m, 4);    // 10

        Assert.Equal(40m, service.GetOrder(order.Id).Total);
    }
}