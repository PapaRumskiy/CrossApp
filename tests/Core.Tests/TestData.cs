using Core.Domain;

namespace Core.Tests;

internal static class TestData
{
    public const string ValidLines = """
        [{"ProductId":"P-1","Name":"Кабель","Price":25.50,"Quantity":2}]
        """;

    public static Order OrderWithLine(string id, decimal price = 25.50m, string customerId = "C-1")
    {
        Order order = Order.Create(id, customerId);
        order.AddLine("P-1", "Кабель", price, 2);
        return order;
    }

    public static string JsonRecord(string id, string status = "Confirmed", string lines = ValidLines) =>
        $$"""
        {"Id":"{{id}}","CustomerId":"C-1","Status":"{{status}}","Lines":{{lines}}}
        """;
}
