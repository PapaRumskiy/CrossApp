using System.Text.Json;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public sealed class JsonOrderImporterTests
{
    [Fact]
    public Task BatchAndStreamRestoreTheSameOrders() => WithJson(
        $"\uFEFF \r\n[{TestData.JsonRecord("A")},{TestData.JsonRecord("B", "Cancelled", "[]")}]",
        async path =>
        {
            var importer = new JsonOrderImporter();

            var batch = await importer.LoadAsync(path);
            var streamed = new List<Order>();
            await foreach (var order in importer.StreamAsync(path))
                streamed.Add(order);

            Assert.Collection(batch,
                order =>
                {
                    Assert.Equal("A", order.Id);
                    Assert.Equal(OrderStatus.Confirmed, order.Status);
                    Assert.Equal(51m, order.Total);
                    Assert.Equal(2, Assert.Single(order.Lines).Quantity);
                },
                order =>
                {
                    Assert.Equal("B", order.Id);
                    Assert.Equal(OrderStatus.Cancelled, order.Status);
                    Assert.Empty(order.Lines);
                });
            Assert.Equal(batch.Select(order => (order.Id, order.Status, order.Total)),
                streamed.Select(order => (order.Id, order.Status, order.Total)));
        });

    [Fact]
    public Task EmptyArrayLoadsAsEmpty() => WithJson("[]", async path =>
    {
        var importer = new JsonOrderImporter();
        var service = new OrderService(new InMemoryOrderStore());

        Assert.Empty(await importer.LoadAsync(path));
        Assert.Equal(0, await service.ImportStreamAsync(importer, path));
        Assert.Empty(service.All());
    });

    [Theory]
    [InlineData("Confirmed", "[]", typeof(DomainException))]
    [InlineData("Unknown", "[]", typeof(DomainException))]
    [InlineData("Draft", "null", typeof(ArgumentException))]
    [InlineData("Draft", "[null]", typeof(ArgumentNullException))]
    [InlineData("Draft", """[{"ProductId":"P","Name":"Item","Price":-1,"Quantity":2}]""", typeof(ArgumentOutOfRangeException))]
    [InlineData("Draft", """[{"ProductId":"P","Name":"Item","Price":1,"Quantity":0}]""", typeof(ArgumentOutOfRangeException))]
    public Task InvalidRecordIncludesItsNumberAndInnerException(string status, string lines, Type innerType) =>
        WithJson($"[{TestData.JsonRecord("A")},{TestData.JsonRecord("B", status, lines)}]", async path =>
        {
            var service = new OrderService(new InMemoryOrderStore());
            var changes = new List<OrderChangedEventArgs>();
            service.Changed += (_, change) => changes.Add(change);

            var error = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonOrderImporter(), path));

            Assert.Contains("№2", error.Message);
            Assert.IsType(innerType, error.InnerException);
            Assert.Empty(service.All());
            Assert.Empty(changes);
        });

    [Theory]
    [InlineData("[{")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("""[{"Id":"A","CustomerId":"C","Status":"Draft","Lines":[{"ProductId":"P","Name":"Item","Quantity":1}]}]""")]
    public Task InvalidJsonRemainsJsonException(string json) => WithJson(json, async path =>
    {
        var importer = new JsonOrderImporter();
        var service = new OrderService(new InMemoryOrderStore());

        await Assert.ThrowsAsync<JsonException>(() => importer.LoadAsync(path));
        await Assert.ThrowsAsync<JsonException>(() => service.ImportStreamAsync(importer, path));
    });

    [Fact]
    public async Task MissingPathAndPreCancelledReadPreserveExceptionTypes()
    {
        string path = Path.Combine(Path.GetTempPath(), $"crossapp-missing-{Guid.NewGuid():N}.json");
        var importer = new JsonOrderImporter();
        var service = new OrderService(new InMemoryOrderStore());
        var cancelled = new CancellationToken(canceled: true);

        await Assert.ThrowsAsync<FileNotFoundException>(() => importer.LoadAsync(path));
        await Assert.ThrowsAsync<OperationCanceledException>(() => importer.LoadAsync(path, cancelled));
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ImportStreamAsync(importer, path, cancelled));
    }

    [Fact]
    public Task StreamingKeepsValidPrefixWhenLaterRecordFails() => WithJson(
        $"[{TestData.JsonRecord("A")},{TestData.JsonRecord("B", "Confirmed", "[]")}]",
        async path =>
        {
            var service = new OrderService(new InMemoryOrderStore());
            var changes = new List<OrderChangedEventArgs>();
            service.Changed += (_, change) => changes.Add(change);

            var error = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportStreamAsync(new JsonOrderImporter(), path));

            Assert.Contains("№2", error.Message);
            Assert.Equal("A", Assert.Single(service.All()).Id);
            Assert.Equal(OrderChangeKind.Created, Assert.Single(changes).Kind);
        });

    [Fact]
    public Task StreamingSkipsDuplicatesAndReportsProcessedCounts() => WithJson(
        $"[{TestData.JsonRecord("A")},{TestData.JsonRecord("a")},{TestData.JsonRecord("B")}]",
        async path =>
        {
            var service = new OrderService(new InMemoryOrderStore());
            var importer = new JsonOrderImporter();
            var processed = new List<int>();
            var changes = new List<OrderChangedEventArgs>();
            service.Changed += (_, change) => changes.Add(change);

            Assert.Equal(2, await service.ImportStreamAsync(importer, path, processed: new ProgressCallback(processed.Add)));
            Assert.Equal(0, await service.ImportStreamAsync(importer, path));

            Assert.Equal([0, 1, 2, 3], processed);
            Assert.Equal(["A", "B"], changes.Select(change => change.OrderId));
        });

    [Fact]
    public Task StreamingObservesEnumeratorCancellationAndClosesTheFile() => WithJson(
        $"[{TestData.JsonRecord("A")},{TestData.JsonRecord("B")}]",
        async path =>
        {
            using var cancellation = new CancellationTokenSource();
            var importer = new JsonOrderImporter();
            var received = new List<string>();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await foreach (var order in importer.StreamAsync(path).WithCancellation(cancellation.Token))
                {
                    received.Add(order.Id);
                    cancellation.Cancel();
                }
            });

            Assert.Equal("A", Assert.Single(received));
            using var exclusive = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
            Assert.True(exclusive.CanRead);
        });

    private static async Task WithJson(string json, Func<string, Task> use)
    {
        string path = Path.Combine(Path.GetTempPath(), $"crossapp-import-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, json);
            await use(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class ProgressCallback(Action<int> report) : IProgress<int>
    {
        public void Report(int value) => report(value);
    }
}
