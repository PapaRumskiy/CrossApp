using Core.Domain;

namespace Core;

public static class SampleData
{
    public static IReadOnlyList<Customer> CreateCustomers() =>
    [
        Customer.Create("C-501", "Олена Коваль"),
        Customer.Create("C-502", "Андрій Мельник"),
        Customer.Create("C-503", "Ірина Шевченко"),
        Customer.Create("C-504", "Олег Бондар"),
        Customer.Create("C-505", "Марія Кравець"),
        Customer.Create("C-506", "Іван Ткаченко"),
        Customer.Create("C-507", "Софія Лисенко")
    ];

    public static IReadOnlyList<Order> CreateOrders() =>
    [
        CreateOrder("SAMPLE-501", "C-501", OrderStatus.Confirmed, ("P-510", "Монітор", 6200m, 1)),
        CreateOrder("SAMPLE-502", "C-502", OrderStatus.Draft, ("P-511", "Кабель", 180m, 3)),
        CreateOrder("SAMPLE-503", "C-503", OrderStatus.Cancelled, ("P-512", "Підставка", 750m, 1)),
        CreateOrder("SAMPLE-504", "C-501", OrderStatus.Draft,
            ("P-513", "Миша", 550m, 2), ("P-521", "Килимок", 200m, 1)),
        CreateOrder("SAMPLE-505", "C-501", OrderStatus.Cancelled, ("P-514", "Адаптер", 500m, 1)),
        CreateOrder("SAMPLE-506", "C-502", OrderStatus.Confirmed,
            ("P-515", "Док-станція", 4500m, 1), ("P-511", "Кабель", 180m, 2)),
        CreateOrder("SAMPLE-507", "C-502", OrderStatus.Confirmed, ("P-521", "Килимок", 200m, 3)),
        CreateOrder("SAMPLE-508", "C-503", OrderStatus.Confirmed, ("P-516", "Навушники", 5000m, 1)),
        CreateOrder("SAMPLE-509", "C-503", OrderStatus.Draft, ("P-512", "Підставка", 750m, 3)),
        CreateOrder("SAMPLE-510", "C-504", OrderStatus.Confirmed, ("P-515", "Док-станція", 4500m, 1)),
        CreateOrder("SAMPLE-511", "C-504", OrderStatus.Draft, ("P-512", "Підставка", 750m, 2)),
        CreateOrder("SAMPLE-512", "C-504", OrderStatus.Cancelled, ("P-514", "Адаптер", 500m, 1)),
        CreateOrder("SAMPLE-513", "C-505", OrderStatus.Draft, ("P-516", "Навушники", 5000m, 1)),
        CreateOrder("SAMPLE-514", "C-505", OrderStatus.Confirmed, ("P-514", "Адаптер", 500m, 2)),
        CreateOrder("SAMPLE-515", "C-505", OrderStatus.Cancelled, ("P-517", "Перехідник", 250m, 1)),
        CreateOrder("SAMPLE-516", "C-506", OrderStatus.Confirmed, ("P-515", "Док-станція", 4500m, 1)),
        CreateOrder("SAMPLE-517", "C-506", OrderStatus.Draft, ("P-518", "Зарядний пристрій", 700m, 1)),
        CreateOrder("SAMPLE-518", "C-506", OrderStatus.Confirmed, ("P-519", "Чохол", 300m, 1))
    ];

    private static Order CreateOrder(
        string id, string customerId, OrderStatus status,
        params (string ProductId, string Name, decimal Price, int Quantity)[] lines)
    {
        Order order = Order.Create(id, customerId);
        foreach (var line in lines)
            order.AddLine(line.ProductId, line.Name, line.Price, line.Quantity);
        if (status == OrderStatus.Confirmed)
            order.Confirm();
        else if (status == OrderStatus.Cancelled)
            order.Cancel();

        return order;
    }
}
