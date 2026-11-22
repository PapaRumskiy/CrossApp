using Core;
using Core.Abstractions;
using Core.Storage;

internal static class StoreFactory
{
    internal static readonly string OrderFilePath = Path.Combine("data", "orders.json");

    public static IOrderStore Create(string[] args) =>
        args.Contains("--file", StringComparer.Ordinal)
            ? new FileOrderStore(OrderFilePath)
            : new InMemoryOrderStore(SampleData.CreateOrders());
}
