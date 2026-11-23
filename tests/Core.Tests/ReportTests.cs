using Core.Domain;
using Core.Reports;

namespace Core.Tests;

public sealed class ReportTests
{
    [Fact]
    public void TopOrdersTieBreaksByIdAndUsesRequestedLimit()
    {
        Order[] orders = [TestData.OrderWithLine("B", 100m), TestData.OrderWithLine("A", 100m),
            TestData.OrderWithLine("C", 200m)];

        var result = ReportService.TopOrders(orders, 2);

        Assert.Equal(["C", "A"], result.Select(order => order.OrderId));
    }

    [Fact]
    public void EmptyReportsReturnEmptyCollectionsAndZeroTotals()
    {
        Order[] orders = [];

        Assert.Equal(new OrderTotalsDto(0, 0m, 0m, 0m), ReportService.Totals(orders));
        Assert.Empty(ReportService.TopCustomers(orders));
        Assert.Empty(ReportService.TopOrders(orders, 5));
        Assert.Empty(ReportService.AboveTotal(orders, 0));
        Assert.Empty(ReportService.ByStatus(orders));
        Assert.Empty(ReportService.ByStatusQuery(orders));
    }

    [Fact]
    public void StatusQueriesAgreeAndJoinIncludesUnmatchedCustomer()
    {
        Order confirmed = TestData.OrderWithLine("A");
        confirmed.Confirm();
        Order[] orders = [confirmed, TestData.OrderWithLine("B", 10m)];
        Customer[] customers = [Customer.Create("C-1", "Олена"), Customer.Create("C-2", "Іван")];

        Assert.Equal(new OrderTotalsDto(2, 71m, 35.5m, 51m), ReportService.Totals(orders));
        Assert.Equal(ReportService.ByStatus(orders), ReportService.ByStatusQuery(orders));
        Assert.Equal("A", Assert.Single(ReportService.AboveTotal(orders, 20m)).OrderId);
        Assert.Equal([new CustomerTotalDto("C-1", 71m)], ReportService.TopCustomers(orders));
        Assert.Equal(
            [new CustomerOrderSummary("C-1", "Олена", 2, 71m), new CustomerOrderSummary("C-2", "Іван", 0, 0m)],
            ReportService.ByCustomer(customers, orders));
    }
}
