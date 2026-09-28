using System.Globalization;
using System.Text;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class JsonImporter
{
    public static ImportResult<ImportRowDto> Load(string path)
    {
        var items = new List<ImportRowDto>();
        var errors = new List<string>();

        string json = File.ReadAllText(path, Encoding.UTF8);

        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add("очікується JSON-масив");
                return new ImportResult<ImportRowDto>(items, errors);
            }

            int number = 0;
            foreach (JsonElement element in doc.RootElement.EnumerateArray())
            {
                number++;
                switch (ParseElement(element))
                {
                    case ParseOk ok:
                        items.Add(ok.Value);
                        break;
                    case ParseFailed failed:
                        errors.Add($"об'єкт {number}: {failed.Reason}");
                        break;
                }
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"некоректний JSON: {ex.Message}");
        }

        return new ImportResult<ImportRowDto>(items, errors);
    }

    private static ParseOutcome ParseElement(JsonElement e)
    {
        if (e.ValueKind != JsonValueKind.Object)
            return new ParseFailed("елемент не є об'єктом");

        string type = GetString(e, "type");

        return type switch
        {
            "P" => ParseProduct(e),
            "C" => ParseCustomer(e),
            "" => new ParseFailed("відсутнє поле type"),
            _ => new ParseFailed($"невідомий тип запису '{type}'")
        };
    }

    private static ParseOutcome ParseProduct(JsonElement e)
    {
        string id = GetString(e, "id");
        string name = GetString(e, "name");

        decimal? price = e.TryGetProperty("price", out JsonElement p)
            && p.ValueKind == JsonValueKind.Number
            && p.TryGetDecimal(out decimal value) ? value : null;

        return (id, name, price) switch
        {
            ("", _, _) => new ParseFailed("у товару id порожній"),
            (_, "", _) => new ParseFailed("у товару назва порожня"),
            (_, _, null) => new ParseFailed("ціна товару відсутня або не є числом"),
            (_, _, < 0m) => new ParseFailed(
                $"ціна товару '{price?.ToString(CultureInfo.InvariantCulture)}' не є невід'ємним числом"),
            var (i, n, pr) => new ParseOk(new ProductDto(i, n, pr.GetValueOrDefault()))
        };
    }

    private static ParseOutcome ParseCustomer(JsonElement e)
    {
        string id = GetString(e, "id");
        string fullName = GetString(e, "fullName");
        string email = GetString(e, "email");

        return (id, fullName, email) switch
        {
            ("", _, _) => new ParseFailed("у клієнта id порожній"),
            (_, "", _) => new ParseFailed("у клієнта ім'я порожнє"),
            (_, _, "") => new ParseFailed("у клієнта email порожній"),
            (_, _, var mail) when !mail.Contains('@')
                => new ParseFailed($"email '{mail}' некоректний"),
            var (i, n, m) => new ParseOk(new CustomerDto(i, n, m))
        };
    }

    private static string GetString(JsonElement e, string name) =>
        e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()?.Trim() ?? ""
            : "";
}