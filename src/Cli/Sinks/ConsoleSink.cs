using Core.Abstractions;
using Core.Events;

namespace Cli.Sinks;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(OrderChangedEventArgs e) =>
        Console.WriteLine(
            $"[ConsoleSink] {e.At:HH:mm:ss} {e.Kind,-14} {e.OrderId} рядків: {e.LinesCount,2} -> сума {e.Total:F2}");
}