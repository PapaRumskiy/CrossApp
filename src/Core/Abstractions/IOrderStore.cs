using Core.Domain;

namespace Core.Abstractions;

/// <summary>
/// Stores order aggregates by case-insensitive, trimmed Id.
/// Reads return detached snapshots and writes capture the state at the time of the call.
/// </summary>
public interface IOrderStore
{
    IReadOnlyList<Order> List();
    Order? GetById(string id);

    /// <exception cref="InvalidOperationException">The order Id already exists.</exception>
    void Add(Order order);

    /// <exception cref="KeyNotFoundException">The order Id does not exist.</exception>
    void Update(Order order);

    bool Remove(string id);
}
