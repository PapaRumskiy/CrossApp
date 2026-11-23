using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonOrderImporter : IStreamingOrderImporter
{
    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await using FileStream stream = Open(path);
        OrderDto?[] records = await JsonSerializer.DeserializeAsync<OrderDto?[]>(stream, cancellationToken: ct)
            .ConfigureAwait(false) ?? throw new JsonException("Очікується JSON-масив замовлень.");

        var orders = new Order[records.Length];
        for (int i = 0; i < records.Length; i++)
            orders[i] = ToOrder(records[i], i + 1, ct);

        ct.ThrowIfCancellationRequested();
        return orders;
    }

    public async IAsyncEnumerable<Order> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await using FileStream stream = Open(path);
        await EnsureArrayStartAsync(stream, ct).ConfigureAwait(false);
        int recordNumber = 0;
        await foreach (OrderDto? dto in JsonSerializer.DeserializeAsyncEnumerable<OrderDto>(stream, cancellationToken: ct)
            .ConfigureAwait(false))
        {
            yield return ToOrder(dto, ++recordNumber, ct);
        }

        ct.ThrowIfCancellationRequested();
    }

    private static FileStream Open(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

    private static async Task EnsureArrayStartAsync(FileStream stream, CancellationToken ct)
    {
        // DeserializeAsyncEnumerable also accepts a null root as an empty sequence.
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var prefix = new char[128];
        int length;
        while ((length = await reader.ReadAsync(prefix.AsMemory(), ct).ConfigureAwait(false)) != 0)
        {
            for (int i = 0; i < length; i++)
            {
                if (prefix[i] is ' ' or '\t' or '\r' or '\n')
                    continue;

                if (prefix[i] != '[')
                    throw new JsonException("Очікується JSON-масив замовлень.");

                stream.Position = 0;
                return;
            }
        }

        throw new JsonException("Очікується JSON-масив замовлень.");
    }

    private static Order ToOrder(OrderDto? dto, int recordNumber, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            ArgumentNullException.ThrowIfNull(dto);
            Order order = Order.FromDto(dto, ct);
            _ = order.Total;
            return order;
        }
        catch (Exception ex) when (ex is ArgumentException or DomainException or OverflowException)
        {
            throw new DomainException($"Запис №{recordNumber}: {ex.Message}", ex);
        }
    }
}
