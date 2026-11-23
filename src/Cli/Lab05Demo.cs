using Core;
using Core.Domain;
using Core.Services;
using Core.Storage;

internal static class Lab05Demo
{
    private const string DemoId = "LAB05-001";

    public static void Run(OrderService service, string storeName, CachingOrderStore cache)
    {
        Customer customer = SampleData.CreateCustomers()[0];
        Console.WriteLine($"=== Лабораторна робота №5 | {storeName} ===");
        Console.WriteLine(storeName == nameof(FileOrderStore)
            ? $"Дані між запусками зберігаються у {Path.GetFullPath(StoreFactory.OrderFilePath)}"
            : "Дані поточного запуску зберігаються в пам’яті процесу.");

        Order? existing = service.Find(DemoId);
        if (existing is null)
        {
            existing = service.CreateOrder(DemoId, customer.Id);
            Console.WriteLine($"Створено {DemoId} зі статусом {existing.Status}.");
        }
        else
        {
            Console.WriteLine($"Завантажено збережене замовлення {DemoId} ({existing.Status}).");
        }

        if (existing.Status == OrderStatus.Draft)
        {
            AddDemoLine("P-501", "Клавіатура", 1250m, 1);
            AddDemoLine("P-502", "Миша", 450.50m, 2);
            service.Confirm(DemoId);
            Console.WriteLine($"Підтверджено {DemoId}.");
        }

        int before = cache.LookupHits;
        Order order = service.Find(DemoId)!;
        _ = service.Find(DemoId.ToLowerInvariant());
        Console.WriteLine($"Пошук за Id — {order}");
        foreach (OrderLine line in order.Lines)
            Console.WriteLine($"  {line.ProductId} {line.Name} × {line.Quantity} = {line.Total:F2}");

        Console.WriteLine($"Кеш повернув {cache.LookupHits - before} із двох пошуків без звернення до сховища.");
        Console.WriteLine("Список замовлень");
        foreach (Order item in service.All())
            Console.WriteLine($"  {item}");

        PrintMatches("Підтверджені", service.Find(item => item.Status == OrderStatus.Confirmed));
        PrintMatches($"Клієнт {customer.Id}", service.Find(item => item.CustomerId == customer.Id));
        PrintMatches("Сума понад 2000", service.Find(item => item.Total > 2000m));

        ShowExpectedError<DomainException>(() => service.CreateOrder(DemoId, customer.Id));
        string missingId = "LAB05-MISSING";
        while (service.Find(missingId) is not null)
            missingId += "-X";

        ShowExpectedError<KeyNotFoundException>(() => service.Confirm(missingId));

        void AddDemoLine(string productId, string name, decimal price, int quantity)
        {
            // A draft left by an interrupted run may already contain this line.
            if (existing.Lines.Any(line => line.ProductId == productId))
                return;

            service.AddLine(DemoId, productId, name, price, quantity);
            Console.WriteLine($"Додано {name} × {quantity}.");
        }
    }

    private static void PrintMatches(string title, IReadOnlyList<Order> orders) =>
        Console.WriteLine($"{title} — {string.Join(", ", orders.Select(order => order.Id))}");

    private static void ShowExpectedError<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException ex)
        {
            Console.WriteLine($"Очікувана помилка ({ex.GetType().Name}) — {ex.Message}");
            return;
        }

        throw new InvalidOperationException($"Очікувався виняток {typeof(TException).Name}.");
    }
}
