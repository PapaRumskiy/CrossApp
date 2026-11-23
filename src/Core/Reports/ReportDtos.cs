using Core.Domain;

namespace Core.Reports;

public sealed record OrderReportDto(string OrderId, string CustomerId, OrderStatus Status, int LinesCount, decimal Total);

public sealed record StatusGroupSummary(OrderStatus Status, int OrdersCount, decimal Total);

public sealed record CustomerTotalDto(string CustomerId, decimal Total);

public sealed record OrderTotalsDto(int OrdersCount, decimal Total, decimal Average, decimal Maximum);

public sealed record CustomerOrderSummary(string CustomerId, string Name, int OrdersCount, decimal Total);
