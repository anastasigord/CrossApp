using System.Runtime.InteropServices;
using System.Text.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var info = new
{
    Student = "Гордієнко Анастасія, група ФЕІ-31",
    OSDescription = RuntimeInformation.OSDescription,
    OSVersion = Environment.OSVersion.ToString(),
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotnetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    AppDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Замовлення (Customer, Product, Order, OrderLine)"
};

if (args.Contains("--json"))
{
    string json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine($"Студент: {info.Student}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription) : {info.OSDescription}");
    Console.WriteLine($"ОС (Environment) : {info.OSVersion}");
    Console.WriteLine($"Архітектура процесу : {info.Architecture}");
    Console.WriteLine($"Версія .NET (CLR) : {info.DotnetVersion}");
    Console.WriteLine($"Runtime : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку : {info.AppDirectory}");
    Console.WriteLine($"Поточний каталог : {info.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область: {info.Domain}");
}