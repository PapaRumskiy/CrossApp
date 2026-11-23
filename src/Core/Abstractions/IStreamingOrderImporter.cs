using Core.Domain;

namespace Core.Abstractions;

public interface IStreamingOrderImporter : IOrderImporter
{
    IAsyncEnumerable<Order> StreamAsync(string path, CancellationToken ct = default);
}
