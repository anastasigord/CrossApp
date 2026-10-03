using System.Collections.Generic;
using Core.Domain;

namespace Core;

public static class SampleData
{
    public static List<Order> Orders()
    {
        var o1 = Order.Create("ORD-001", "C-100");
        o1.AddLine("P-001", "Цемент М400 25кг", 180.50m, 10);
        o1.Confirm();

        var o2 = Order.Create("ORD-002", "C-101");
        o2.AddLine("P-002", "Пісок", 95m, 3);
        o2.AddLine("P-003", "Цегла", 12m, 200);

        var o3 = Order.Create("ORD-003", "C-102");
        o3.AddLine("P-004", "Кабель UTP cat6", 25m, 50);
        o3.AddLine("P-005", "Роз'єм RJ-45", 3.5m, 100);
        o3.AddLine("P-006", "Патч-панель", 640m, 2);
        o3.Confirm();

        var o4 = Order.Create("ORD-004", "C-103");
        o4.AddLine("P-007", "Гравій", 70m, 5);

        var o5 = Order.Create("ORD-005", "C-100");
        o5.AddLine("P-008", "Арматура 12мм", 38m, 40);
        o5.AddLine("P-009", "Дріт в'язальний", 55m, 4);
        o5.AddLine("P-010", "Рукавички", 22m, 12);
        o5.AddLine("P-011", "Каска", 130m, 6);
        o5.AddLine("P-012", "Відро будівельне", 85m, 3);

        var o6 = Order.Create("ORD-006", "C-104");
        o6.AddLine("P-013", "Фарба біла 10л", 520m, 2);
        o6.Confirm();

        return [o1, o2, o3, o4, o5, o6];
    }
}