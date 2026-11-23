using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

internal static class Lab08Demo
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args is not ["--lab08", "--import", var path, ..] || string.IsNullOrWhiteSpace(path))
            return Usage();

        bool streaming = false;
        bool repeat = false;
        bool cancelBefore = false;
        int? timeoutMs = null;
        int delayMs = 0;
        for (int i = 3; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--stream":
                    streaming = true;
                    break;
                case "--repeat":
                    repeat = true;
                    break;
                case "--cancel-before":
                    cancelBefore = true;
                    break;
                case "--file":
                    break;
                case "--cancel-after-ms" when i + 1 < args.Length &&
                    int.TryParse(args[i + 1], out int timeout) && timeout > 0:
                    timeoutMs = timeout;
                    i++;
                    break;
                case "--delay-ms" when i + 1 < args.Length &&
                    int.TryParse(args[i + 1], out delayMs) && delayMs > 0:
                    i++;
                    break;
                default:
                    return Usage();
            }
        }

        if (delayMs > 0 && !streaming)
            return Usage();

        using var userCancellation = new CancellationTokenSource();
        using var timeoutCancellation = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            userCancellation.Token, timeoutCancellation.Token);
        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;
            userCancellation.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            var service = new OrderService(StoreFactory.Create(args));
            service.Changed += (_, change) =>
                Console.WriteLine($"{change.Kind} — {change.OrderId}, {change.Status}, позицій {change.LinesCount}, сума {change.Total:F2}");

            IStreamingOrderImporter importer = new JsonOrderImporter();
            if (delayMs > 0)
                importer = new DelayedImporter(importer, delayMs);

            Console.WriteLine(streaming ? "Потокове читання JSON" : "Пакетне читання JSON");
            if (cancelBefore)
                userCancellation.Cancel();
            if (timeoutMs is int milliseconds)
                timeoutCancellation.CancelAfter(milliseconds);

            await ImportOnce();
            if (repeat)
            {
                Console.WriteLine("Повторний імпорт у те саме сховище");
                await ImportOnce();
            }

            return 0;

            async Task ImportOnce()
            {
                int added = streaming
                    ? await service.ImportStreamAsync(importer, path, linked.Token,
                        new ConsoleProgress(count => $"Оброблено записів — {count}"))
                    : await service.ImportAsync(importer, path, linked.Token,
                        new ConsoleProgress(percent => $"Прогрес — {percent}%"));
                Console.WriteLine($"Імпорт завершено. Додано замовлень — {added}.");
            }
        }
        catch (OperationCanceledException)
        {
            if (!userCancellation.IsCancellationRequested && timeoutCancellation.IsCancellationRequested)
            {
                Console.Error.WriteLine("Імпорт скасовано через таймаут. Уже збережені записи залишилися у сховищі.");
                return 124;
            }

            Console.Error.WriteLine("Імпорт скасовано. Уже збережені записи залишилися у сховищі.");
            return 130;
        }
        catch (DomainException ex)
        {
            Console.Error.WriteLine($"Помилка даних замовлення — {ex.Message}");
            return 1;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Файл не знайдено — {ex.FileName ?? path}");
            return 1;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Помилка введення або виведення — {ex.Message}");
            return 1;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Некоректний JSON — {ex.Message}");
            return 1;
        }
        catch (Exception ex) when (ex is InvalidDataException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Помилка доступу або даних сховища — {ex.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static int Usage()
    {
        Console.Error.WriteLine(
            "Очікується --lab08 --import <шлях> [--stream [--delay-ms N]] [--repeat] [--file] [--cancel-after-ms N] [--cancel-before]");
        return 2;
    }

    private sealed class ConsoleProgress(Func<int, string> format) : IProgress<int>
    {
        public void Report(int value) => Console.WriteLine(format(value));
    }

    private sealed class DelayedImporter(IStreamingOrderImporter inner, int milliseconds) : IStreamingOrderImporter
    {
        public Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default) =>
            inner.LoadAsync(path, ct);

        public async IAsyncEnumerable<Order> StreamAsync(
            string path, [EnumeratorCancellation] CancellationToken ct = default)
        {
            bool first = true;
            await foreach (Order order in inner.StreamAsync(path, ct).WithCancellation(ct))
            {
                if (!first)
                    await Task.Delay(milliseconds, ct);

                first = false;
                yield return order;
            }
        }
    }
}
