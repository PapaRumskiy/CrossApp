using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class InMemoryOrderStore : IOrderStore
{
    private readonly Dictionary<string, OrderDto> _orders = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public InMemoryOrderStore(IEnumerable<Order>? initialOrders = null)
    {
        foreach (Order order in initialOrders ?? [])
            Add(order);
    }

    public IReadOnlyList<Order> List()
    {
        lock (_gate)
            return _orders.Values.Select(Order.FromDto).ToArray();
    }

    public Order? GetById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        lock (_gate)
            return _orders.TryGetValue(id.Trim(), out OrderDto? order) ? Order.FromDto(order) : null;
    }

    public void Add(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        lock (_gate)
        {
            if (!_orders.TryAdd(order.Id, order.ToDto()))
                throw new InvalidOperationException($"Замовлення {order.Id} вже існує.");
        }
    }

    public void Update(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        lock (_gate)
        {
            if (!_orders.ContainsKey(order.Id))
                throw new KeyNotFoundException($"Замовлення {order.Id} не знайдено.");

            _orders[order.Id] = order.ToDto();
        }
    }

    public bool Remove(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        lock (_gate)
            return _orders.Remove(id.Trim());
    }
}
