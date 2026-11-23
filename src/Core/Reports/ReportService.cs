using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    public static IReadOnlyList<CustomerTotalDto> TopCustomers(IEnumerable<Order> orders) =>
        orders.GroupBy(order => order.CustomerId, StringComparer.Ordinal)
            .Select(group => new CustomerTotalDto(group.Key, group.Sum(order => order.Total)))
            .TopBy(5, customer => customer.Total, customer => customer.CustomerId)
            .ToArray();

    public static IReadOnlyList<OrderReportDto> TopOrders(IEnumerable<Order> orders, int count) =>
        orders.Select(order => (Order: order, Total: order.Total))
            .TopBy(count, entry => entry.Total, entry => entry.Order.Id)
            .Select(entry => ToReport(entry.Order, entry.Total))
            .ToArray();

    public static IReadOnlyList<OrderReportDto> AboveTotal(IEnumerable<Order> orders, decimal threshold) =>
        orders.Select(order => (Order: order, Total: order.Total))
            .Where(entry => entry.Total > threshold)
            .OrderByDescending(entry => entry.Total)
            .ThenBy(entry => entry.Order.Id, StringComparer.Ordinal)
            .Select(entry => ToReport(entry.Order, entry.Total))
            .ToArray();

    public static IReadOnlyList<StatusGroupSummary> ByStatus(IEnumerable<Order> orders) =>
        orders.GroupBy(order => order.Status)
            .OrderBy(group => group.Key)
            .Select(group => new StatusGroupSummary(group.Key, group.Count(), group.Sum(order => order.Total)))
            .ToArray();

    public static IReadOnlyList<StatusGroupSummary> ByStatusQuery(IEnumerable<Order> orders)
    {
        var query =
            from order in orders
            group order by order.Status into statusGroup
            orderby statusGroup.Key
            select new StatusGroupSummary(statusGroup.Key, statusGroup.Count(), statusGroup.Sum(order => order.Total));

        return query.ToArray();
    }

    public static OrderTotalsDto Totals(IEnumerable<Order> orders)
    {
        decimal[] totals = orders.Select(order => order.Total).ToArray();
        return totals.Length == 0
            ? new OrderTotalsDto(0, 0m, 0m, 0m)
            : new OrderTotalsDto(totals.Length, totals.Sum(), totals.Average(), totals.Max());
    }

    public static IReadOnlyList<CustomerOrderSummary> ByCustomer(
        IEnumerable<Customer> customers, IEnumerable<Order> orders) =>
        customers.GroupJoin(
                orders,
                customer => customer.Id,
                order => order.CustomerId,
                (customer, matches) => new CustomerOrderSummary(
                    customer.Id, customer.Name, matches.Count(), matches.Sum(order => order.Total)),
                StringComparer.Ordinal)
            .OrderBy(customer => customer.CustomerId, StringComparer.Ordinal)
            .ToArray();

    private static OrderReportDto ToReport(Order order, decimal total) =>
        new(order.Id, order.CustomerId, order.Status, order.Lines.Count, total);
}
