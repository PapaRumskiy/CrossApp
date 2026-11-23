using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public sealed class OrderServiceTests
{
    [Theory]
    [InlineData(-1, 1, "price")]
    [InlineData(10, 0, "quantity")]
    public void InvalidLinePreservesStateAndEmitsNoEvent(int price, int quantity, string parameter)
    {
        var store = new InMemoryOrderStore([TestData.OrderWithLine("A")]);
        var service = new OrderService(store);
        var changes = new List<OrderChangedEventArgs>();
        service.Changed += (_, change) => changes.Add(change);

        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => service.AddLine("A", "P-2", "Адаптер", price, quantity));

        Assert.Equal(parameter, error.ParamName);
        var order = Assert.Single(service.All());
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(51m, order.Total);
        Assert.Equal("P-1", Assert.Single(order.Lines).ProductId);
        Assert.Empty(changes);
    }

    [Fact]
    public void CreatePublishesOneEventAfterStoreContainsOrder()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var changes = new List<OrderChangedEventArgs>();
        service.Changed += (sender, change) =>
        {
            Assert.Same(service, sender);
            Assert.Equal(OrderStatus.Draft, service.Find(change.OrderId)?.Status);
            changes.Add(change);
        };

        service.CreateOrder("A", "C-1");

        var created = Assert.Single(changes);
        Assert.Equal("A", created.OrderId);
        Assert.Equal("C-1", created.CustomerId);
        Assert.Equal(OrderChangeKind.Created, created.Kind);
    }

    [Fact]
    public void RejectedTransitionsLeaveConfirmedOrderUnchanged()
    {
        Order order = TestData.OrderWithLine("A");
        order.Confirm();
        var service = new OrderService(new InMemoryOrderStore([order]));
        var changes = new List<OrderChangedEventArgs>();
        service.Changed += (_, change) => changes.Add(change);

        Assert.Throws<DomainException>(() => service.AddLine("A", "P-2", "Миша", 1m, 1));
        Assert.Throws<DomainException>(() => service.Confirm("A"));
        Assert.Throws<DomainException>(() => service.Cancel("A"));

        Order stored = Assert.Single(service.All());
        Assert.Equal(OrderStatus.Confirmed, stored.Status);
        Assert.Equal(51m, stored.Total);
        Assert.Single(stored.Lines);
        Assert.Empty(changes);
    }

    [Fact]
    public void EmptyConfirmationAndDuplicateAreDomainErrors()
    {
        var service = new OrderService(new InMemoryOrderStore([Order.Create("A", "C-1")]));

        Assert.Throws<DomainException>(() => service.Confirm("A"));
        Assert.Throws<DomainException>(() => service.CreateOrder("a", "C-1"));

        Assert.Equal(OrderStatus.Draft, Assert.Single(service.All()).Status);
    }

    [Fact]
    public void CancelledOrderFreesAnActiveSlot()
    {
        var service = new OrderService(new InMemoryOrderStore(
            Enumerable.Range(1, 5).Select(i => Order.Create($"A-{i}", "C-1"))));

        Assert.Throws<DomainException>(() => service.CreateOrder("A-6", "C-1"));
        service.Cancel("A-1");
        service.CreateOrder("A-6", "C-1");

        Assert.Equal(5, service.Find(order => order.Status != OrderStatus.Cancelled).Count);
    }

    [Fact]
    public void CacheUsesDetachedSnapshotsAndInvalidatesOnWrites()
    {
        var store = new CachingOrderStore(new InMemoryOrderStore());
        Assert.Null(store.GetById("a"));

        store.Add(TestData.OrderWithLine("A"));
        Order copy = Assert.IsType<Order>(store.GetById(" a "));
        copy.Confirm();
        Assert.Equal(OrderStatus.Draft, store.GetById("A")?.Status);
        Assert.Equal(1, store.LookupHits);

        store.Update(copy);
        Assert.Equal(OrderStatus.Confirmed, store.GetById("a")?.Status);
        Assert.True(store.Remove("a"));
        Assert.Null(store.GetById("A"));
        Assert.False(store.Remove("missing"));
    }
}
