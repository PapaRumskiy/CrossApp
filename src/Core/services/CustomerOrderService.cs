
using Core.Domain;

namespace Core.Services;

public sealed class CustomerOrderService
{
    private const int MaxActiveOrders = 5;

    private readonly List<Order> _orders = [];

    public IReadOnlyList<Order> Orders =>
        _orders.AsReadOnly();

    public void Register(Customer customer, Order order)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(order);

        if (order.CustomerId != customer.Id)
            throw new ArgumentException(
                "Замовлення належить іншому клієнту",
                nameof(order));

        if (_orders.Any(o => o.Id == order.Id))
            throw new InvalidOperationException(
                $"Замовлення {order.Id} вже зареєстровано");

        int activeCount = _orders.Count(o =>
            o.CustomerId == customer.Id &&
            o.Status != OrderStatus.Cancelled);

        if (activeCount >= MaxActiveOrders)
            throw new InvalidOperationException(
                $"Клієнт {customer.Id} вже має " +
                $"{MaxActiveOrders} активних замовлень");

        _orders.Add(order);
    }
}
