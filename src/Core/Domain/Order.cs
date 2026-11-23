
using Core.Dto;

namespace Core.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }
    public string CustomerId { get; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyList<OrderLine> Lines =>
        _lines.AsReadOnly();

    public decimal Total =>
        _lines.Sum(line => line.Total);

    private Order(string id, string customerId)
    {
        Id = id;
        CustomerId = customerId;
        Status = OrderStatus.Draft;
    }

    // Фабричний метод створення замовлення
    public static Order Create(string id, string customerId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException(
                "Ідентифікатор замовлення обов'язковий",
                nameof(id));

        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException(
                "Ідентифікатор клієнта обов'язковий",
                nameof(customerId));

        return new Order(id.Trim(), customerId.Trim());
    }

    // Додавання товару до замовлення
    public void AddLine(
        string productId,
        string name,
        decimal price,
        int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException(
                $"Не можна додавати товари: " +
                $"замовлення {Id} має статус {Status}");

        OrderLine line = OrderLine.Create(
            productId,
            name,
            price,
            quantity);

        _lines.Add(line);
    }

    // Підтвердження замовлення
    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException(
                $"Не можна підтвердити замовлення {Id}: " +
                $"поточний статус {Status}");

        if (_lines.Count == 0)
            throw new DomainException(
                $"Не можна підтвердити порожнє замовлення {Id}");

        Status = OrderStatus.Confirmed;
    }

    // Скасування замовлення
    public void Cancel()
    {
        Status = Status switch
        {
            OrderStatus.Draft => OrderStatus.Cancelled,

            OrderStatus.Confirmed =>
                throw new DomainException(
                    $"Не можна скасувати підтверджене " +
                    $"замовлення {Id}"),

            OrderStatus.Cancelled =>
                throw new DomainException(
                    $"Замовлення {Id} вже скасоване"),

            _ => throw new DomainException(
                $"Невідомий статус замовлення {Id}")
        };
    }

    // Перетворення сутності у DTO
    public OrderDto ToDto()
    {
        return new OrderDto(
            Id,
            CustomerId,
            Status.ToString(),
            _lines.Select(line => line.ToDto()).ToArray());
    }

    // Відновлення сутності з DTO
    public static Order FromDto(OrderDto dto) => FromDto(dto, CancellationToken.None);

    public static Order FromDto(OrderDto dto, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Lines is null)
            throw new ArgumentException(
                "Список позицій не може бути null",
                nameof(dto));

        Order order = Create(dto.Id, dto.CustomerId);

        foreach (OrderLineDto line in dto.Lines)
        {
            ct.ThrowIfCancellationRequested();
            OrderLine validated = OrderLine.FromDto(line);

            order.AddLine(
                validated.ProductId,
                validated.Name,
                validated.Price,
                validated.Quantity);
        }

        ct.ThrowIfCancellationRequested();
        // Відновлення статусу через доменні методи
        switch (dto.Status)
        {
            case nameof(OrderStatus.Draft):
                break;

            case nameof(OrderStatus.Confirmed):
                order.Confirm();
                break;

            case nameof(OrderStatus.Cancelled):
                order.Cancel();
                break;

            default:
                throw new DomainException($"Невідомий статус замовлення: {dto.Status}");
        }

        return order;
    }

    public override string ToString()
    {
        return $"Замовлення {Id} | " +
               $"Клієнт: {CustomerId} | " +
               $"Статус: {Status} | " +
               $"Позицій: {_lines.Count} | " +
               $"Сума: {Total:F2}";
    }
}
