using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class OrderService(IOrderStore store)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly object _gate = new();

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

        return order;
    }

    public void AddLine(string id, string productId, string name, decimal price, int quantity) =>
        Change(id, order => order.AddLine(productId, name, price, quantity));

    public void Confirm(string id) => Change(id, order => order.Confirm());

    public void Cancel(string id) => Change(id, order => order.Cancel());

    public bool Remove(string id)
    {
        lock (_gate)
            return _store.Remove(id);
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return All().Where(predicate).ToArray();
    }

    private void Change(string id, Action<Order> operation)
    {
        lock (_gate)
        {
            Order order = Find(id) ?? throw new KeyNotFoundException($"Замовлення {id} не знайдено.");
            operation(order);
            _store.Update(order);
        }
    }
}
