using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class OrderService(IOrderStore store, INotificationSink? sink)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly object _gate = new();

    public OrderService(IOrderStore store) : this(store, null) { }

    public event EventHandler<OrderChangedEventArgs>? Changed;

    public Order CreateOrder(string id, string customerId)
    {
        Order order = Order.Create(id, customerId);
        lock (_gate)
        {
            var orders = _store.List();
            if (orders.Any(existing => StringComparer.OrdinalIgnoreCase.Equals(existing.Id, order.Id)))
                throw new InvalidOperationException($"Замовлення {order.Id} вже існує.");

            CustomerOrderService.EnsureCanRegister(orders, order.CustomerId);
            _store.Add(order);
        }

        RaiseChanged(order, OrderChangeKind.Created);
        return order;
    }

    public void AddLine(string id, string productId, string name, decimal price, int quantity) =>
        Change(id, OrderChangeKind.LineAdded, order => order.AddLine(productId, name, price, quantity));

    public void Confirm(string id) => Change(id, OrderChangeKind.Confirmed, order => order.Confirm());

    public void Cancel(string id) => Change(id, OrderChangeKind.Cancelled, order => order.Cancel());

    public bool Remove(string id)
    {
        Order? order;
        lock (_gate)
        {
            order = Find(id);
            if (order is null || !_store.Remove(id))
                return false;
        }

        RaiseChanged(order, OrderChangeKind.Removed);
        return true;
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return All().Where(predicate).ToArray();
    }

    private void Change(string id, OrderChangeKind kind, Action<Order> operation)
    {
        Order order;
        lock (_gate)
        {
            order = Find(id) ?? throw new KeyNotFoundException($"Замовлення {id} не знайдено.");
            operation(order);
            _store.Update(order);
        }

        RaiseChanged(order, kind);
    }

    private void RaiseChanged(Order order, OrderChangeKind kind)
    {
        if (Changed is null && sink is null)
            return;

        var change = new OrderChangedEventArgs(
            order.Id, order.CustomerId, kind, order.Lines.Count, order.Total, order.Status, DateTimeOffset.UtcNow);
        Changed?.Invoke(this, change);
        sink?.Notify(change);
    }
}
