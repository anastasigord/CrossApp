using System;
using System.Collections.Generic;
using System.Linq;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

public sealed record OrderImportOutcome(
    Order? Order,
    IReadOnlyList<string> Errors);

public static class OrderImportMapper
{
    // Перетворює імпортовані товари на рядки одного замовлення.
    // Некоректні товари не зупиняють імпорт: вони потрапляють у перелік помилок.
    public static OrderImportOutcome ToOrder(
        ImportResult<ImportRowDto> import,
        string orderId,
        string customerId,
        int quantityPerLine = 1)
    {
        ArgumentNullException.ThrowIfNull(import);

        // Помилки розбору файлу з лаб. 3
        var errors = new List<string>(import.Errors);

        Order order;
        try
        {
            order = Order.Create(orderId, customerId);
        }
        catch (ArgumentException ex)
        {
            errors.Add($"Замовлення не створено: {ex.Message}");
            return new OrderImportOutcome(null, errors);
        }

        // CustomerDto пропускаємо: у замовлення потрапляють лише товари
        foreach (var product in import.Items.OfType<ProductDto>())
        {
            try
            {
                order.AddLine(product.Id, product.Name, product.Price, quantityPerLine);
            }
            catch (ArgumentException ex) // також ArgumentOutOfRangeException
            {
                errors.Add($"Товар {product.Id}: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                errors.Add($"Товар {product.Id}: {ex.Message}");
            }
        }

        return new OrderImportOutcome(order, errors);
    }
}