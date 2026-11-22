using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, OrderDto?> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private int _lookupHits;

    public int LookupHits
    {
        get
        {
            lock (_gate)
                return _lookupHits;
        }
    }

    public IReadOnlyList<Order> List()
    {
        lock (_gate)
            return _inner.List();
    }

    public Order? GetById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        id = id.Trim();
        lock (_gate)
        {
            if (_byId.TryGetValue(id, out OrderDto? snapshot))
                _lookupHits++;
            else
            {
                snapshot = _inner.GetById(id)?.ToDto();
                _byId.Add(id, snapshot);
            }

            return snapshot is null ? null : Order.FromDto(snapshot);
        }
    }

    public void Add(Order order)
    {
        lock (_gate)
        {
            _inner.Add(order);
            _byId.Remove(order.Id);
        }
    }

    public void Update(Order order)
    {
        lock (_gate)
        {
            _inner.Update(order);
            _byId.Remove(order.Id);
        }
    }

    public bool Remove(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        id = id.Trim();
        lock (_gate)
        {
            bool removed = _inner.Remove(id);
            _byId.Remove(id);
            return removed;
        }
    }
}
