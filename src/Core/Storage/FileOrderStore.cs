using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileOrderStore : IOrderStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;
    private readonly object _gate = new();
    private InMemoryOrderStore? _orders;

    public FileOrderStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
    }

    private InMemoryOrderStore Orders => _orders ??= Load();

    public IReadOnlyList<Order> List()
    {
        lock (_gate)
            return Orders.List();
    }

    public Order? GetById(string id)
    {
        lock (_gate)
            return Orders.GetById(id);
    }

    public void Add(Order order) => Change(store =>
    {
        store.Add(order);
        return true;
    });

    public void Update(Order order) => Change(store =>
    {
        store.Update(order);
        return true;
    });

    public bool Remove(string id) => Change(store => store.Remove(id));

    private bool Change(Func<InMemoryOrderStore, bool> change)
    {
        lock (_gate)
        {
            var updated = new InMemoryOrderStore(Orders.List());
            if (!change(updated))
                return false;

            Save(updated.List());
            _orders = updated;
            return true;
        }
    }

    private InMemoryOrderStore Load()
    {
        try
        {
            string json = File.ReadAllText(_path);
            if (string.IsNullOrWhiteSpace(json))
                return new InMemoryOrderStore();

            OrderDto[] orders = JsonSerializer.Deserialize<OrderDto[]>(json, JsonOptions)
                ?? throw new JsonException("Очікується масив замовлень.");

            return new InMemoryOrderStore(orders.Select(Order.FromDto));
        }
        catch (FileNotFoundException)
        {
            return new InMemoryOrderStore();
        }
        catch (DirectoryNotFoundException)
        {
            return new InMemoryOrderStore();
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or DomainException)
        {
            throw new InvalidDataException($"Некоректні дані у файлі «{_path}». {ex.Message}", ex);
        }
    }

    private void Save(IReadOnlyList<Order> orders)
    {
        string directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(_path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            string json = JsonSerializer.Serialize(orders.Select(order => order.ToDto()).ToArray(), JsonOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
