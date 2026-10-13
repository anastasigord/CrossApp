using Core.Abstractions;
using Core.Events;

namespace Cli.Sinks;

public sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
    }

    public void Notify(OrderChangedEventArgs e)
    {
        string line = $"{e.At:HH:mm:ss} {e.Kind,-14} {e.OrderId} рядків: {e.LinesCount,2} -> сума {e.Total:F2}";
        File.AppendAllText(_path, line + Environment.NewLine);
    }
}