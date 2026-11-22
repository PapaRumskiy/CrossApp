using Core.Domain;

namespace Core;

public static class SampleData
{
    public static IReadOnlyList<Customer> CreateCustomers() =>
    [
        Customer.Create("C-501", "Олена Коваль"),
        Customer.Create("C-502", "Андрій Мельник"),
        Customer.Create("C-503", "Ірина Шевченко")
    ];

    public static IReadOnlyList<Order> CreateOrders() =>
    [
        CreateOrder("SAMPLE-501", "C-501", OrderStatus.Confirmed, "P-510", "Монітор", 6200m, 1),
        CreateOrder("SAMPLE-502", "C-502", OrderStatus.Draft, "P-511", "Кабель", 180m, 3),
        CreateOrder("SAMPLE-503", "C-503", OrderStatus.Cancelled, "P-512", "Підставка", 750m, 1)
    ];

    private static Order CreateOrder(
        string id, string customerId, OrderStatus status, string productId, string name, decimal price, int quantity)
    {
        Order order = Order.Create(id, customerId);
        order.AddLine(productId, name, price, quantity);
        if (status == OrderStatus.Confirmed)
            order.Confirm();
        else if (status == OrderStatus.Cancelled)
            order.Cancel();

        return order;
    }
}
