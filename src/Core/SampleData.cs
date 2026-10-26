using System.Collections.Generic;
using Core.Domain;
using Core.Dto;

namespace Core;

public static class SampleData
{
    // Набір із 4 рядків = 21910 грн
    private static readonly (string ProductId, string Name, decimal Price, int Quantity)[] FourLines =
    [
        ("P-101", "Ноутбук Lenovo IdeaPad", 18500m, 1),
        ("P-103", "Бездротова миша", 850m, 2),
        ("P-104", "USB-C хаб", 320m, 3),
        ("P-105", "Килимок для миші", 750m, 1),
    ];

    // Набір із 5 рядків = 27910 грн
    private static readonly (string ProductId, string Name, decimal Price, int Quantity)[] FiveLines =
    [
        .. FourLines,
        ("P-102", "Монітор 24 дюйми", 6000m, 1),
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

    public static List<Order> Orders() =>
    [
        Make("O-001", "C-001", OrderStatus.Draft,     ("P-201", "Кабель живлення", 180m, 1)),
        Make("O-002", "C-002", OrderStatus.Draft,     ("P-202", "Флешка 64 ГБ", 210m, 2)),
        Make("O-003", "C-003", OrderStatus.Confirmed, ("P-203", "Клавіатура мембранна", 850m, 1)),
        Make("O-004", "C-004", OrderStatus.Draft,     FourLines),
        Make("O-005", "C-005", OrderStatus.Cancelled, FiveLines),
        Make("O-006", "C-006", OrderStatus.Confirmed, ("P-203", "Клавіатура мембранна", 850m, 1)),
        Make("O-007", "C-001", OrderStatus.Confirmed, ("P-204", "Роутер Wi-Fi 6", 1430m, 8)),
        Make("O-008", "C-002", OrderStatus.Draft,     ("P-205", "Принтер лазерний", 4238m, 5)),
        Make("O-009", "C-003", OrderStatus.Confirmed, FourLines),
        Make("O-010", "C-004", OrderStatus.Cancelled, FiveLines),
        Make("O-011", "C-005", OrderStatus.Draft,     ("P-203", "Клавіатура мембранна", 850m, 1)),
        Make("O-012", "C-006", OrderStatus.Draft,     ("P-204", "Роутер Wi-Fi 6", 1430m, 8)),
        Make("O-013", "C-001", OrderStatus.Confirmed, ("P-204", "Роутер Wi-Fi 6", 1430m, 8)),
        Make("O-014", "C-002", OrderStatus.Draft,     FourLines),
        Make("O-015", "C-003", OrderStatus.Confirmed, FiveLines),
        Make("O-016", "C-004", OrderStatus.Draft,     ("P-203", "Клавіатура мембранна", 850m, 1)),
        Make("O-017", "C-005", OrderStatus.Draft,     ("P-206", "Веб-камера Full HD", 845m, 2)),
        Make("O-018", "C-006", OrderStatus.Draft,     ("P-204", "Роутер Wi-Fi 6", 1430m, 8)),
    ];

    public static List<CustomerDto> Customers() =>
    [
        new("C-001", "Олександр Коваленко", "o.kovalenko@example.com"),
        new("C-002", "Марія Шевченко", "m.shevchenko@example.com"),
        new("C-003", "Іван Мельник", "i.melnyk@example.com"),
        new("C-004", "Олена Ткаченко", "o.tkachenko@example.com"),
        new("C-005", "Дмитро Кравченко", "d.kravchenko@example.com"),
        new("C-006", "Анна Бондаренко", "a.bondarenko@example.com"),
    ];
}