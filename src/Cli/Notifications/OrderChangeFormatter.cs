using Core.Events;

namespace Cli.Notifications;

internal static class OrderChangeFormatter
{
    public static string Format(OrderChangedEventArgs change) => FormattableString.Invariant(
        $"{change.At:O} | {change.Kind} | {change.OrderId} | customer={change.CustomerId} | status={change.Status} | lines={change.LinesCount} | total={change.Total:F2}");
}
