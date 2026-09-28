using System.Text;
using Core.Dto;
using Core.Import;

Console.OutputEncoding = Encoding.UTF8;

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

Console.WriteLine($"Шлях файлу: {path}");

ImportResult<ImportRowDto>? result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => CsvImporter.Load(path),
    ".json" => JsonImporter.Load(path),
    _ => null
};

if (result is null)
{
    Console.WriteLine($"Непідтримуваний формат '{Path.GetExtension(path)}'. Підтримуються .csv та .json");
    return 1;
}

int accepted = result.Items.Count;
int skipped = result.Errors.Count;
int total = accepted + skipped;
double percent = total == 0 ? 0 : skipped * 100.0 / total;

Console.WriteLine($"Усього: {total} | Прийнято: {accepted} | Пропущено: {skipped} | Помилок: {percent:F2}%");
Console.WriteLine();

foreach (ImportRowDto row in result.Items.Take(10))
{
    string text = row switch
    {
        ProductDto p => $"Товар  | {p.Id,-6} | {p.Name,-28} | {p.Price,10:F2}",
        CustomerDto c => $"Клієнт | {c.Id,-6} | {c.FullName,-28} | {c.Email}",
        _ => row.ToString() ?? ""
    };
    Console.WriteLine(text);
}

if (skipped > 0)
{
    Console.WriteLine();
    Console.WriteLine($"Пропущено рядків: {skipped}");
    foreach (string error in result.Errors)
        Console.WriteLine($"! {error}");
}

return 0;