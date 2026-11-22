using Core.Abstractions;
using Core.Events;

namespace Cli.Notifications;

internal sealed class FileSink(string path) : INotificationSink
{
    private readonly string _path = Path.GetFullPath(path);

    public void Notify(OrderChangedEventArgs change)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.AppendAllText(_path, OrderChangeFormatter.Format(change) + Environment.NewLine);
    }
}
