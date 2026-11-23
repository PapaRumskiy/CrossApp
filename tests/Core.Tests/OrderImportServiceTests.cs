using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public sealed class OrderImportServiceTests
{
    [Fact]
    public async Task RepeatingImportSkipsExistingIdsAndEmitsNoExtraEvents()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var changes = new List<OrderChangedEventArgs>();
        service.Changed += (_, change) =>
        {
            Assert.NotNull(service.Find(change.OrderId));
            changes.Add(change);
        };
        var importer = FromOrders(TestData.OrderWithLine("A"), TestData.OrderWithLine("a"), TestData.OrderWithLine("B"));

        Assert.Equal(2, await service.ImportAsync(importer, "unused"));
        Assert.Equal(0, await service.ImportAsync(importer, "unused"));

        Assert.Equal(2, service.All().Count);
        Assert.Equal(["A", "B"], changes.Select(change => change.OrderId));
        Assert.All(changes, change => Assert.Equal(OrderChangeKind.Created, change.Kind));
    }

    [Fact]
    public async Task BatchProgressIncludesSkippedRecordsAndReaches100()
    {
        var service = new OrderService(new InMemoryOrderStore([Order.Create("A", "C-1")]));
        var progress = new RecordingProgress();
        var importer = FromOrders(TestData.OrderWithLine("a"), TestData.OrderWithLine("B"), TestData.OrderWithLine("C"));

        int added = await service.ImportAsync(importer, "unused", progress: progress);

        Assert.Equal(2, added);
        Assert.Equal([0, 33, 66, 100], progress.Values);
    }

    [Fact]
    public async Task EmptyBatchReportsStartAndCompletion()
    {
        var progress = new RecordingProgress();
        var service = new OrderService(new InMemoryOrderStore());

        int added = await service.ImportAsync(FromOrders(), "unused", progress: progress);

        Assert.Equal(0, added);
        Assert.Equal([0, 100], progress.Values);
    }

    [Fact]
    public async Task CancellationBetweenRecordsLeavesCommittedPrefixAndPartialProgress()
    {
        using var source = new CancellationTokenSource();
        var progress = new RecordingProgress(value =>
        {
            if (value == 33)
                source.Cancel();
        });
        var service = new OrderService(new InMemoryOrderStore());
        var changes = new List<OrderChangedEventArgs>();
        service.Changed += (_, change) => changes.Add(change);
        var importer = FromOrders(TestData.OrderWithLine("A"), TestData.OrderWithLine("B"), TestData.OrderWithLine("C"));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.ImportAsync(importer, "unused", source.Token, progress));

        Assert.Equal("A", Assert.Single(service.All()).Id);
        Assert.Equal("A", Assert.Single(changes).OrderId);
        Assert.Equal([0, 33], progress.Values);
    }

    [Fact]
    public async Task PreCancelledTokenStopsBeforeCallingImporter()
    {
        var importer = new StubImporter(_ => throw new InvalidOperationException("Importer was called"));
        var service = new OrderService(new InMemoryOrderStore());
        var progress = new RecordingProgress();
        var token = new CancellationToken(canceled: true);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.ImportAsync(importer, "unused", token, progress));

        Assert.Empty(service.All());
        Assert.Empty(progress.Values);
    }

    [Fact]
    public async Task LinkedTokenCancelsBecauseTimeoutFiresAfterImporterStarts()
    {
        using var user = new CancellationTokenSource();
        using var timeout = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(user.Token, timeout.Token);
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var importer = new StubImporter(async ct =>
        {
            var blocked = new TaskCompletionSource<IReadOnlyList<Order>>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = ct.Register(() => blocked.TrySetException(new OperationCanceledException(ct)));
            started.SetResult(ct);
            return await blocked.Task;
        });
        var service = new OrderService(new InMemoryOrderStore());

        Task<int> importing = service.ImportAsync(importer, "unused", linked.Token);
        Assert.Equal(linked.Token, await started.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        timeout.CancelAfter(TimeSpan.Zero);
        var error = await Assert.ThrowsAsync<OperationCanceledException>(
            () => importing.WaitAsync(TimeSpan.FromSeconds(10)));

        Assert.Equal(linked.Token, error.CancellationToken);
        Assert.True(timeout.IsCancellationRequested);
        Assert.False(user.IsCancellationRequested);
        Assert.Empty(service.All());
    }

    [Fact]
    public async Task FailedStoreAddDoesNotRaiseEventOrReportCompletion()
    {
        var service = new OrderService(new FailingStore());
        var changes = new List<OrderChangedEventArgs>();
        var progress = new RecordingProgress();
        service.Changed += (_, change) => changes.Add(change);

        await Assert.ThrowsAsync<IOException>(() =>
            service.ImportAsync(FromOrders(TestData.OrderWithLine("A")), "unused", progress: progress));

        Assert.Empty(service.All());
        Assert.Empty(changes);
        Assert.Equal([0], progress.Values);
    }

    private static IOrderImporter FromOrders(params Order[] orders) =>
        new StubImporter(_ => Task.FromResult<IReadOnlyList<Order>>(orders));

    private sealed class StubImporter(Func<CancellationToken, Task<IReadOnlyList<Order>>> load) : IOrderImporter
    {
        public Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default) => load(ct);
    }

    private sealed class RecordingProgress(Action<int>? afterReport = null) : IProgress<int>
    {
        public List<int> Values { get; } = [];
        public void Report(int value)
        {
            Values.Add(value);
            afterReport?.Invoke(value);
        }
    }

    private sealed class FailingStore : IOrderStore
    {
        private readonly InMemoryOrderStore _inner = new();
        public IReadOnlyList<Order> List() => _inner.List();
        public Order? GetById(string id) => _inner.GetById(id);
        public void Add(Order order) => throw new IOException("Write failed");
        public void Update(Order order) => _inner.Update(order);
        public bool Remove(string id) => _inner.Remove(id);
    }
}
