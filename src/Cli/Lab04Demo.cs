using Core.Domain;
using Core.Dto;
using Core.Import;
using Core.Services;

internal static class Lab04Demo
{
    public static void Run()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("=== Лабораторна робота №4 ===");
        Console.WriteLine(
            "Доменна модель: сутності, інваріанти, інкапсуляція");

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 1: Успіх ===");

        Order order = Order.Create("ORD-001", "C-001");

        Console.WriteLine("Створено замовлення:");
        Console.WriteLine(order);

        order.AddLine("P-001", "Laptop", 35000.50m, 2);
        order.AddLine("P-003", "Mouse", 800.99m, 3);

        Console.WriteLine("\nДодано товари:");

        foreach (OrderLine line in order.Lines)
        {
            Console.WriteLine(
                $" {line.ProductId,-8} " +
                $"{line.Name,-15} " +
                $"{line.Price,10:F2} x {line.Quantity} " +
                $"= {line.Total:F2}");
        }

        Console.WriteLine($"\nЗагальна сума: {order.Total:F2}");

        order.Confirm();

        Console.WriteLine("\nЗамовлення підтверджено:");
        Console.WriteLine(order);

        // Перевірка перетворення в DTO та назад
        Console.WriteLine("\n=== Перевірка DTO ===");

        OrderDto dto = order.ToDto();
        Order restored = Order.FromDto(dto);

        Console.WriteLine("Замовлення відновлено з DTO:");
        Console.WriteLine(restored);

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 2: Порушення інваріантів ===");

        TryDo("Порожній CustomerId",
            () => Order.Create("ORD-002", ""));

        TryDo("Від'ємна ціна",
            () => OrderLine.Create(
                "P-005", "Keyboard", -100m, 2));

        TryDo("Нульова кількість",
            () => OrderLine.Create(
                "P-005", "Keyboard", 500m, 0));

        TryDo("Порожній ProductId",
            () => OrderLine.Create(
                "", "Keyboard", 500m, 2));

        Order emptyOrder = Order.Create("ORD-003", "C-002");

        TryDo("Підтвердження порожнього замовлення",
            () => emptyOrder.Confirm());

        TryDo("Додавання після підтвердження",
            () => order.AddLine(
                "P-006", "Monitor", 12000m, 1));

        TryDo("Повторне підтвердження",
            () => order.Confirm());

        TryDo("Скасування підтвердженого замовлення",
            () => order.Cancel());

        // Перевірка захисту FromDto
        var invalidDto = new OrderDto(
            "ORD-004",
            "C-003",
            "Confirmed",
            new[]
            {
                new OrderLineDto(
                    "P-007", "Broken Product", -500m, 2)
            });

        TryDo("Відновлення некоректного DTO",
            () => Order.FromDto(invalidDto));

        Console.WriteLine();
        Console.WriteLine("=== Перевірка незмінності стану ===");

        Console.WriteLine(order);

        Console.WriteLine();
        Console.WriteLine("=== Додатково: скасування чернетки ===");

        Order cancelledOrder =
            Order.Create("ORD-005", "C-004");

        cancelledOrder.Cancel();

        Console.WriteLine(cancelledOrder);

        TryDo("Повторне скасування",
            () => cancelledOrder.Cancel());

        RunAdditionalTasks();
        }

    
        private static void RunAdditionalTasks()
        {
            Console.WriteLine();
            Console.WriteLine("=== Додаткове завдання 1: Інтеграція імпорту ===");

            // Реальний CSV-імпортер із лабораторної №3
            var imported = ProductCsvImporter.Load(
                Path.Combine("data", "sample.csv"));

            // Перетворення DTO на доменні сутності
            ImportResult<OrderLine> mapped =
                ProductImportMapper.Convert(imported);

            Console.WriteLine(
                $"CSV: прийнято {imported.Items.Count}, " +
                $"помилок {imported.Errors.Count}");

            Console.WriteLine(
                $"Після доменних перевірок: " +
                $"прийнято {mapped.Items.Count}, " +
                $"помилок {mapped.Errors.Count}");

            foreach (OrderLine line in mapped.Items.Take(5))
            {
                Console.WriteLine(
                    $" Товар: {line.ProductId} {line.Name} " +
                    $"— {line.Price:F2}");
            }

            foreach (string error in mapped.Errors)
            {
                Console.WriteLine($" ! {error}");
            }

            Console.WriteLine();
            Console.WriteLine("=== Додаткове завдання 2: Міжсутнісний інваріант ===");

            Customer customer = Customer.Create(
                "C-100", "Test Customer");

            var service = new CustomerOrderService();

            for (int i = 1; i <= 5; i++)
            {
                Order order = Order.Create(
                    $"ORD-{i:000}", customer.Id);

                service.Register(customer, order);

                Console.WriteLine(
                    $"Зареєстровано замовлення {order.Id}");
            }

            Order sixthOrder = Order.Create(
                "ORD-006", customer.Id);

            TryDo("Реєстрація шостого активного замовлення",
                () => service.Register(customer, sixthOrder));

            Console.WriteLine(
                $"Зареєстровано замовлень: {service.Orders.Count}");

            Console.WriteLine("Скасовуємо перше замовлення...");
            service.Orders[0].Cancel();

            service.Register(customer, sixthOrder);

            Console.WriteLine(
                $"Успішно зареєстровано {sixthOrder.Id}");

            Console.WriteLine(
                $"Активних замовлень: " +
                service.Orders.Count(o =>
                    o.Status != OrderStatus.Cancelled));

            Console.WriteLine();
            Console.WriteLine("=== Додаткове завдання 3: OrderStatus ===");

            Console.WriteLine(
                "Використовується enum: Draft, Confirmed, Cancelled");

            Console.WriteLine(
                "Дозволені переходи: Draft -> Confirmed, " +
                "Draft -> Cancelled");

            Console.WriteLine(
                "Заборонені переходи перевіряються в " +
                "Confirm() та Cancel() через бізнес-правила.");
        }


    private static void TryDo(string title, Action action)
    {
        try
        {
            action();

            Console.WriteLine(
                $" {title}: ПОМИЛКА — інваріант не спрацював!");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(
                $" {title}: {ex.GetType().Name} — {ex.Message}");
        }
        catch (DomainException ex)
        {
            Console.WriteLine(
                $" {title}: {ex.GetType().Name} — {ex.Message}");
        }
    }
}
