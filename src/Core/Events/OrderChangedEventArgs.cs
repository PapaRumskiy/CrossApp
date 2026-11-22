using Core.Domain;

namespace Core.Events;

public sealed class OrderChangedEventArgs(
    string orderId,
    string customerId,
    OrderChangeKind kind,
    int linesCount,
    decimal total,
    OrderStatus status,
    DateTimeOffset at) : EventArgs
{
    public string OrderId { get; } = orderId;
    public string CustomerId { get; } = customerId;
    public OrderChangeKind Kind { get; } = kind;
    public int LinesCount { get; } = linesCount;
    public decimal Total { get; } = total;
    public OrderStatus Status { get; } = status;
    public DateTimeOffset At { get; } = at;
}
