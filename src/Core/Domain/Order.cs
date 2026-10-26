using System;
using System.Collections.Generic;
using System.Linq;
using Core.Dto;

namespace Core.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = new();

    public string Id { get; }
    public string CustomerId { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;

    // Для сумісності зі сховищами та попередніми лабораторними
    public bool IsConfirmed => Status == OrderStatus.Confirmed;

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public decimal Total => _lines.Sum(l => l.Subtotal);

    private string StatusText => Status switch
    {
        OrderStatus.Confirmed => "підтверджене",
        OrderStatus.Cancelled => "скасоване",
        _ => "чернетка"
    };

    private Order(string id, string customerId)
    {
        Id = id;
        CustomerId = customerId;
    }

    public static Order Create(string id, string customerId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор замовлення обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Ідентифікатор клієнта обов'язковий", nameof(customerId));

        return new Order(id.Trim(), customerId.Trim());
    }

    public void AddLine(string productId, string name, decimal price, int quantity)
    {
        if (IsConfirmed)
            throw new InvalidOperationException(
                $"Замовлення {Id} вже підтверджене, рядки додавати не можна");
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException(
                $"Замовлення {Id} скасоване, рядки додавати не можна");

        var line = OrderLine.Create(productId, name, price, quantity);

        _lines.Add(line);
    }

    public void Confirm()
    {
        if (IsConfirmed)
            throw new InvalidOperationException(
                $"Замовлення {Id} вже підтверджене");
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException(
                $"Замовлення {Id} скасоване, підтвердити не можна");
        if (_lines.Count == 0)
            throw new InvalidOperationException(
                $"Не можна підтвердити порожнє замовлення {Id}: немає жодного рядка");

        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException(
                $"Замовлення {Id} вже скасоване");

        Status = OrderStatus.Cancelled;
    }

    public OrderDto ToDto() =>
        new(Id, CustomerId, IsConfirmed, _lines.Select(l => l.ToDto()).ToList(), Status.ToString());

    public static Order FromDto(OrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = Create(dto.Id, dto.CustomerId);

        foreach (var l in dto.Lines ?? new List<OrderLineDto>())
            order.AddLine(l.ProductId, l.Name, l.Price, l.Quantity);

        // Старі файли без поля Status: статус виводимо з IsConfirmed
        OrderStatus status = Enum.TryParse(dto.Status, true, out OrderStatus parsed) && Enum.IsDefined(parsed)
            ? parsed
            : dto.IsConfirmed ? OrderStatus.Confirmed : OrderStatus.Draft;

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

    public override string ToString() =>
        $"Замовлення {Id} (клієнт {CustomerId}): рядків {_lines.Count}, сума {Total:F2}, {StatusText}";
}