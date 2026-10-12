namespace Core.Events;

public sealed class OrderChangedEventArgs(
    string orderId, OrderChangeKind kind, int linesCount, decimal total) : EventArgs
{
    public string OrderId { get; } = orderId;
    public OrderChangeKind Kind { get; } = kind;
    public int LinesCount { get; } = linesCount;
    public decimal Total { get; } = total;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}