using System.Diagnostics;
using Core.Domain;

namespace Core.Reports;

public sealed record BenchmarkResult(
    int Records, int Repetitions, decimal Total, BenchmarkMeasurement Streaming, BenchmarkMeasurement WithList);

public sealed record BenchmarkMeasurement(double Milliseconds, long AllocatedBytes);

public static class ReportBenchmark
{
    public static BenchmarkResult Run()
    {
        const int records = 100_000;
        const int repetitions = 7;
        var orders = new OrderReportDto[records];
        for (int i = 0; i < orders.Length; i++)
        {
            orders[i] = new OrderReportDto(
                $"BENCH-{i:D6}", $"C-{i % 100:D3}", (OrderStatus)(i % 3), 1, i % 5000);
        }

        decimal expectedTotal = Streaming(orders);
        for (int i = 0; i < 3; i++)
        {
            EnsureEqual(expectedTotal, Streaming(orders));
            EnsureEqual(expectedTotal, WithList(orders));
        }

        var streaming = new BenchmarkMeasurement[repetitions];
        var withList = new BenchmarkMeasurement[repetitions];
        for (int i = 0; i < repetitions; i++)
        {
            if (i % 2 == 0)
            {
                streaming[i] = Measure(Streaming, orders, expectedTotal);
                withList[i] = Measure(WithList, orders, expectedTotal);
            }
            else
            {
                withList[i] = Measure(WithList, orders, expectedTotal);
                streaming[i] = Measure(Streaming, orders, expectedTotal);
            }
        }

        return new BenchmarkResult(records, repetitions, expectedTotal, Median(streaming), Median(withList));
    }

    private static decimal Streaming(OrderReportDto[] orders) =>
        orders.Where(IsLargeConfirmed).Select(order => order.Total).Sum();

    private static decimal WithList(OrderReportDto[] orders) =>
        orders.Where(IsLargeConfirmed).ToList().Select(order => order.Total).Sum();

    private static bool IsLargeConfirmed(OrderReportDto order) =>
        order.Status == OrderStatus.Confirmed && order.Total > 1000m;

    private static BenchmarkMeasurement Measure(
        Func<OrderReportDto[], decimal> query, OrderReportDto[] orders, decimal expectedTotal)
    {
        long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        decimal result = query(orders);
        TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;

        EnsureEqual(expectedTotal, result);
        return new BenchmarkMeasurement(elapsed.TotalMilliseconds, bytes);
    }

    private static BenchmarkMeasurement Median(BenchmarkMeasurement[] measurements) =>
        new(measurements.Select(sample => sample.Milliseconds).Order().ElementAt(measurements.Length / 2),
            measurements.Select(sample => sample.AllocatedBytes).Order().ElementAt(measurements.Length / 2));

    private static void EnsureEqual(decimal expected, decimal actual)
    {
        if (actual != expected)
            throw new InvalidOperationException("Варіанти LINQ-запиту повернули різні суми.");
    }
}
