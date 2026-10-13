using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

public static class CsvImporter
{
    // Роздільник — крапка з комою; формат: P;id;name;price  або  C;id;fullName;email
    private const char Separator = ';';

    // Лише крапка як десятковий роздільник, без роздільників тисяч ("1,5" не стане 15)
    private const NumberStyles PriceStyles =
        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

        public static ImportResult<ImportRowDto> Load(string path, Action<string>? onSkipped = null)
    {
        var items = new List<ImportRowDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                {
                    string message = $"рядок {number}: {failed.Reason}";
                    errors.Add(message);
                    onSkipped?.Invoke(message); // повідомляємо зовнішній код, не знаючи, що він зробить
                    break;
                }
            }
        }

        return new ImportResult<ImportRowDto>(items, errors);
    }
    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", ..] and { Length: not 4 }
                => new ParseFailed($"для товару очікую 4 колонки, отримав {parts.Length}"),
            ["C", ..] and { Length: not 4 }
                => new ParseFailed($"для клієнта очікую 4 колонки, отримав {parts.Length}"),

            ["P", "", _, _] => new ParseFailed("у товару id порожній"),
            ["P", _, "", _] => new ParseFailed("у товару назва порожня"),
            ["P", _, _, var price] when !TryParsePrice(price, out _)
                => new ParseFailed($"ціна товару '{price}' не є невід'ємним числом"),
            ["P", var id, var name, var price]
                => new ParseOk(new ProductDto(id, name, ParsePrice(price))),

            ["C", "", _, _] => new ParseFailed("у клієнта id порожній"),
            ["C", _, "", _] => new ParseFailed("у клієнта ім'я порожнє"),
            ["C", _, _, ""] => new ParseFailed("у клієнта email порожній"),
            ["C", _, _, var email] when !email.Contains('@')
                => new ParseFailed($"email '{email}' некоректний"),
            ["C", var id, var name, var email]
                => new ParseOk(new CustomerDto(id, name, email)),

            [var prefix, ..] => new ParseFailed($"невідомий префікс типу '{prefix}'"),
            _ => new ParseFailed("порожній рядок")
        };
    }

    private static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text, PriceStyles, CultureInfo.InvariantCulture, out price) && price >= 0;

    private static decimal ParsePrice(string text) =>
        decimal.Parse(text, PriceStyles, CultureInfo.InvariantCulture);
}