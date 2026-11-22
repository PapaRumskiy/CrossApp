
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

        EnsureCanRegister(_orders, customer.Id);
        _orders.Add(order);
    }

    internal static void EnsureCanRegister(IEnumerable<Order> orders, string customerId)
    {
        int activeCount = orders.Count(o =>
            o.CustomerId == customerId &&
            o.Status != OrderStatus.Cancelled);

        if (activeCount >= MaxActiveOrders)
            throw new InvalidOperationException(
                $"Клієнт {customerId} вже має " +
                $"{MaxActiveOrders} активних замовлень");

    }
}
