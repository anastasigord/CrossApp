using Core.Events;

namespace Core.Abstractions;

public interface INotificationSink
{
    void Notify(OrderChangedEventArgs e);
}