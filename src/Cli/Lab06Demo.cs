using Cli.Notifications;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

internal static class Lab06Demo
{
    public static void Run()
    {
        Console.WriteLine("=== Лабораторна робота №6 | Доменні події ===");
        RunEvents();
        RunInjectedSinks();
        RunImportCallback();
    }

    private static void RunEvents()
    {
        const string orderId = "LAB06-001";
        const string draftId = "LAB06-002";
        const string customerId = "C-601";
        string logPath = Path.Combine("logs", "app.log");
        var service = new OrderService(new InMemoryOrderStore());
        var consoleSink = new ConsoleSink();
        var fileSink = new FileSink(logPath);
        int linesBefore = File.Exists(logPath) ? File.ReadLines(logPath).Count() : 0;
        int changes = 0;
        EventHandler<OrderChangedEventArgs> logger = LogChange;

        service.Changed += logger;
        service.Changed += (_, _) => changes++;

        service.CreateOrder(orderId, customerId);
        service.AddLine(orderId, "P-601", "Клавіатура", 1250m, 1);
        service.Confirm(orderId);
        service.CreateOrder(draftId, "C-602");

        int addedLines = File.ReadLines(logPath).Count() - linesBefore;
        if (changes != 4 || addedLines != changes)
            throw new InvalidOperationException("Обидва підписники мали отримати чотири події.");

        Console.WriteLine($"До відписки — лямбда отримала {changes} події, {logPath} отримав {addedLines} записи.");
        VerifyRejectedChange(service, orderId, logPath, () => changes);

        service.Changed -= logger;
        int beforeCancel = changes;
        string logBeforeCancel = File.ReadAllText(logPath);
        service.Cancel(draftId);

        if (changes != beforeCancel + 1 || File.ReadAllText(logPath) != logBeforeCancel)
            throw new InvalidOperationException("Після відписки подію мала отримати лише лямбда.");

        Console.WriteLine($"Після -= і Cancelled — лічильник {beforeCancel} → {changes}, {logPath} без змін.");
        Console.WriteLine("Лямбда залишається у списку підписників і продовжує отримувати Changed.");

        PrintMatches("Підтверджені", service.Find(order => order.Status == OrderStatus.Confirmed));
        PrintMatches("Сума понад 1000", service.Find(order => order.Total > 1000m));
        PrintMatches("Клієнт C-602", service.Find(order => order.CustomerId == "C-602"));

        void LogChange(object? sender, OrderChangedEventArgs change)
        {
            consoleSink.Notify(change);
            fileSink.Notify(change);
        }
    }

    private static void VerifyRejectedChange(
        OrderService service, string id, string logPath, Func<int> readChanges)
    {
        var original = service.Find(id)!.ToDto();
        int before = readChanges();
        string logBefore = File.ReadAllText(logPath);
        bool rejected = false;
        try
        {
            service.AddLine(id, "P-602", "Миша", 300m, 1);
        }
        catch (InvalidOperationException ex)
        {
            rejected = true;
            Console.WriteLine($"Очікуваний виняток — {ex.Message}");
        }

        Order current = service.Find(id)!;
        if (!rejected || current.Status.ToString() != original.Status ||
            !current.ToDto().Lines.SequenceEqual(original.Lines) ||
            readChanges() != before || File.ReadAllText(logPath) != logBefore)
        {
            throw new InvalidOperationException("Відхилена операція мала зберегти стан замовлення й журнал.");
        }

        Console.WriteLine($"Після помилки — {id} {current.Status}, позицій {current.Lines.Count}, сума {current.Total:F2}.");
        Console.WriteLine($"Подій {before} → {readChanges()}, {logPath} без змін.");
    }

    private static void RunInjectedSinks()
    {
        Console.WriteLine("ConsoleSink через DI");
        var consoleService = new OrderService(new InMemoryOrderStore(), new ConsoleSink());
        consoleService.CreateOrder("LAB06-DI-CONSOLE", "C-601");
        consoleService.Remove("LAB06-DI-CONSOLE");

        string path = Path.Combine("logs", "sink.log");
        int before = File.Exists(path) ? File.ReadLines(path).Count() : 0;
        var fileService = new OrderService(new InMemoryOrderStore(), new FileSink(path));
        fileService.CreateOrder("LAB06-DI-FILE", "C-601");
        var added = File.ReadLines(path).Skip(before).ToArray();
        if (added.Length != 1 || !added[0].Contains(" | Created | LAB06-DI-FILE | ", StringComparison.Ordinal))
            throw new InvalidOperationException("FileSink мав записати одну подію створення.");

        Console.WriteLine($"FileSink через DI — {path} отримав 1 запис Created для LAB06-DI-FILE.");
    }

    private static void RunImportCallback()
    {
        var skipped = new List<string>();
        var result = ProductCsvImporter.Load(Path.Combine("data", "sample.csv"), message =>
        {
            skipped.Add(message);
            Console.WriteLine($"Action<string> у CLI — {message}");
        });

        if (!skipped.SequenceEqual(result.Errors))
            throw new InvalidOperationException("Повідомлення callback і ImportResult.Errors мають збігатися.");

        Console.WriteLine($"CSV — прийнято {result.Items.Count}, помилок {result.Errors.Count}, викликів callback {skipped.Count}.");
    }

    private static void PrintMatches(string label, IReadOnlyList<Order> matches) =>
        Console.WriteLine($"Func — {label} → {string.Join(", ", matches.Select(order => order.Id))}");
}
