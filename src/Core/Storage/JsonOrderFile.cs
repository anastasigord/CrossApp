using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Читання й запис колекції замовлень у JSON. Використовують FileOrderStore і прапорці --import / --export.
public static class JsonOrderFile
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping   // кирилиця без \uXXXX
    };

    // JSON-текст колекції (порожня колекція дає "[]")
    public static string ToJson(IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);
        return JsonSerializer.Serialize(orders.Select(o => o.ToDto()).ToList(), Options);
    }

    // Немає файлу: порожня колекція. Зіпсований файл: StorageException зі зрозумілим текстом.
    public static IReadOnlyList<Order> Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            return [];

        List<OrderDto?> dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<OrderDto?>>(File.ReadAllText(path), Options) ?? [];
        }
        catch (JsonException ex)
        {
            throw new StorageException($"Файл {Path.GetFileName(path)} містить некоректний JSON.", ex);
        }

        var result = new List<Order>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            try
            {
                result.Add(Order.FromDto(dtos[i]!));   // повторно перевіряє інваріанти домену
            }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            {
                throw new StorageException(
                    $"Файл {Path.GetFileName(path)}: запис {i + 1} пошкоджений: {ex.Message}", ex);
            }
        }
        return result;
    }

    // Атомарний запис: спочатку тимчасовий файл, потім заміна цільового
    public static void Save(string path, IEnumerable<Order> orders)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(orders);

        string full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        string tmp = full + ".tmp";

        File.WriteAllText(tmp, ToJson(orders), Utf8NoBom);
        File.Move(tmp, full, overwrite: true);   // заміна без «напівзаписаного» файлу
    }
}