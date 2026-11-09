using System.Collections.Generic;
using Core.Domain;

namespace Core.Tests;

// Маленький фіксований набір, незалежний від SampleData
internal static class TestData
{
    // Суми: T-001 = 50, T-002 = 2000, T-003 = 700, T-004 = 2000 (рівні суми потрібні для тесту порядку)
    public static List<Order> Items() =>
    [
        Make("T-001", "C-1", OrderStatus.Draft,     ("P-1", "Кабель", 50m, 1)),
        Make("T-002", "C-1", OrderStatus.Confirmed, ("P-2", "Ноутбук", 1000m, 2)),
        Make("T-003", "C-2", OrderStatus.Draft,     ("P-3", "Монітор", 350m, 2)),
        Make("T-004", "C-3", OrderStatus.Cancelled, ("P-2", "Ноутбук", 2000m, 1)),
    ];

    private static Order Make(string id, string customerId, OrderStatus status,
        params (string ProductId, string Name, decimal Price, int Quantity)[] lines)
    {
        var order = Order.Create(id, customerId);
        foreach (var l in lines)
            order.AddLine(l.ProductId, l.Name, l.Price, l.Quantity);

        switch (status)
        {
            case OrderStatus.Confirmed:
                order.Confirm();
                break;
            case OrderStatus.Cancelled:
                order.Cancel();
                break;
        }
        return order;
    }
}