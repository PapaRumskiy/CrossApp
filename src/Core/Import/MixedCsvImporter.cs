using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class MixedCsvImporter
{
    private const char Separator = ';';

    public static ImportResult<object> Load(string path)
    {
        var items = new List<object>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (number == 1 &&
                line.StartsWith("type;", StringComparison.OrdinalIgnoreCase))
                continue;

            switch (ParseLine(line))
            {
                case ProductOk product:
                    items.Add(product.Value);
                    break;

                case WarehouseOk warehouse:
                    items.Add(warehouse.Value);
                    break;

                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<object>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(
            Separator,
            StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["W", var id, var name] =>
                string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(name)
                    ? new ParseFailed("ідентифікатор або назва складу порожні")
                    : new WarehouseOk(new WarehouseDto(id, name)),

            ["W", var id, var name, ""] =>
                string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(name)
                    ? new ParseFailed("ідентифікатор або назва складу порожні")
                    : new WarehouseOk(new WarehouseDto(id, name)),

            ["P", _, "", _] =>
                new ParseFailed("назва товару порожня"),

            ["P", _, _, var price]
                when !decimal.TryParse(
                    price,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out _) =>
                new ParseFailed("некоректна ціна товару"),

            ["P", var id, var name, var price]
                when decimal.TryParse(
                    price,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal parsedPrice)
                => new ProductOk(
                    new ProductDto(id, name, parsedPrice)),

            [var type, ..] =>
                new ParseFailed(
                    $"невідомий тип '{type}'"),

            _ =>
                new ParseFailed(
                    "некоректний формат рядка")
        };
    }

    private abstract record ParseOutcome;

    private sealed record ProductOk(ProductDto Value) : ParseOutcome;

    private sealed record WarehouseOk(WarehouseDto Value) : ParseOutcome;

    private sealed record ParseFailed(string Reason) : ParseOutcome;
}