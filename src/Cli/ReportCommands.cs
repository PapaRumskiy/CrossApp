using System.Globalization;
using Core;
using Core.Domain;
using Core.Reports;

internal static class ReportCommands
{
    public static bool CanHandle(string[] args) => Command(args) is
        "report-top" or "report-high" or "report-groups" or "report-sum" or
        "report-orders" or "report-deferred" or "report-customers" or "report-benchmark";

    public static int Run(string[] args)
    {
        string? command = Command(args);
        int fileFlags = args.Count(arg => arg == "--file");
        bool useFile = fileFlags == 1;
        string[] options = args.Where(arg => arg != "--file").Skip(1).ToArray();
        bool acceptsValue = command is "report-high" or "report-orders";
        if (options.Length > (acceptsValue ? 1 : 0) ||
            fileFlags > 1 ||
            (useFile && (command is "report-deferred" or "report-benchmark")))
        {
            return UsageError();
        }

        decimal threshold = 2000m;
        int count = 5;
        if (options.Length == 1 &&
            ((command == "report-high" && !TryParseTotal(options[0], out threshold)) ||
             (command == "report-orders" && (!int.TryParse(options[0], out count) || count < 0))))
        {
            return UsageError();
        }

        if (command == "report-deferred")
        {
            RunDeferred();
            return 0;
        }

        if (command == "report-benchmark")
        {
            PrintBenchmark(ReportBenchmark.Run());
            return 0;
        }

        try
        {
            var store = StoreFactory.Create(args);
            var orders = store.List();
            Console.WriteLine(useFile ? "Джерело — data/orders.json" : "Джерело — SampleData");
            switch (command)
            {
                case "report-top":
                    Console.WriteLine("Топ-5 клієнтів за сумою");
                    Console.WriteLine($"{"Клієнт",-16} {"Сума",12}");
                    foreach (var customer in ReportService.TopCustomers(orders))
                        Console.WriteLine($"{customer.CustomerId,-16} {customer.Total,12:F2}");
                    break;
                case "report-high":
                    PrintOrders($"Замовлення дорожчі за {threshold:F2}", ReportService.AboveTotal(orders, threshold));
                    break;
                case "report-orders":
                    PrintOrders($"Топ-{count} замовлень за сумою", ReportService.TopOrders(orders, count));
                    break;
                case "report-groups":
                    PrintGroups(orders);
                    break;
                case "report-sum":
                    var totals = ReportService.Totals(orders);
                    Console.WriteLine($"Замовлень         {totals.OrdersCount,12}");
                    Console.WriteLine($"Загальна сума     {totals.Total,12:F2}");
                    Console.WriteLine($"Середній чек      {totals.Average,12:F2}");
                    Console.WriteLine($"Максимальна сума  {totals.Maximum,12:F2}");
                    break;
                case "report-customers":
                    Console.WriteLine("Замовлення клієнтів (GroupJoin)");
                    Console.WriteLine($"{"Клієнт",-10} {"Ім’я",-24} {"Замовлень",9} {"Сума",12}");
                    foreach (var customer in ReportService.ByCustomer(SampleData.CreateCustomers(), orders))
                        Console.WriteLine($"{customer.CustomerId,-10} {customer.Name,-24} {customer.OrdersCount,9} {customer.Total,12:F2}");
                    break;
            }

            return 0;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string? Command(string[] args) => args is ["--file", var command, ..]
        ? command
        : args.FirstOrDefault();

    private static int UsageError()
    {
        Console.Error.WriteLine("Некоректні аргументи. report-high [поріг], report-orders [N]. --file доступний для звітів зі сховища.");
        return 1;
    }

    private static bool TryParseTotal(string value, out decimal total)
    {
        const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        return decimal.TryParse(value, style, CultureInfo.InvariantCulture, out total) ||
            decimal.TryParse(value, style, CultureInfo.CurrentCulture, out total);
    }

    private static void PrintOrders(string title, IReadOnlyList<OrderReportDto> orders)
    {
        Console.WriteLine(title);
        Console.WriteLine($"{"Замовлення",-16} {"Клієнт",-10} {"Статус",-12} {"Позицій",8} {"Сума",12}");
        foreach (var order in orders)
            Console.WriteLine($"{order.OrderId,-16} {order.CustomerId,-10} {order.Status,-12} {order.LinesCount,8} {order.Total,12:F2}");
    }

    private static void PrintGroups(IReadOnlyList<Order> orders)
    {
        var groups = ReportService.ByStatus(orders);
        var queryGroups = ReportService.ByStatusQuery(orders);
        if (!groups.SequenceEqual(queryGroups))
            throw new InvalidOperationException("Method syntax і query syntax повернули різні групи.");

        Console.WriteLine("Групи за статусом");
        Console.WriteLine($"{"Статус",-14} {"Замовлень",9} {"Сума",12}");
        foreach (var group in groups)
            Console.WriteLine($"{group.Status,-14} {group.OrdersCount,9} {group.Total,12:F2}");

        Console.WriteLine($"Method syntax і query syntax — збіг для {groups.Count} груп.");
    }

    private static void RunDeferred()
    {
        var source = new List<Order> { Order.Create("DEFERRED-1", "C-501"), Confirmed("DEFERRED-2") };
        var query = source.Where(order => order.Status == OrderStatus.Confirmed);
        Console.WriteLine($"Початковий Count() — {query.Count()}");
        source.Add(Confirmed("DEFERRED-3"));
        Console.WriteLine($"Count() після додавання — {query.Count()}");
        var snapshot = query.ToList();
        source.Clear();
        Console.WriteLine($"Count() після очищення джерела — {query.Count()}");
        Console.WriteLine($"Знімок ToList() після очищення — {snapshot.Count}");

        static Order Confirmed(string id)
        {
            Order order = Order.Create(id, "C-501");
            order.AddLine("P-511", "Кабель", 180m, 1);
            order.Confirm();
            return order;
        }
    }

    private static void PrintBenchmark(BenchmarkResult result)
    {
        Console.WriteLine($"Stopwatch — {result.Records} записів, {result.Repetitions} повторень, медіани");
        Console.WriteLine($"Сума в обох варіантах — {result.Total:F2}");
        Console.WriteLine($"{"Варіант",-26} {"Час, мс",10} {"Виділено, Б",14}");
        PrintMeasurement("Без проміжного списку", result.Streaming);
        PrintMeasurement("ToList() після Where", result.WithList);
    }

    private static void PrintMeasurement(string name, BenchmarkMeasurement measurement) =>
        Console.WriteLine($"{name,-26} {measurement.Milliseconds,10:F3} {measurement.AllocatedBytes,14}");
}
