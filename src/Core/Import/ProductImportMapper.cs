
using Core.Domain;
using Core.Dto;

namespace Core.Import;

public static class ProductImportMapper
{
    public static ImportResult<OrderLine> Convert(
        ImportResult<ProductDto> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var items = new List<OrderLine>();
        var errors = new List<string>(source.Errors);

        for (int i = 0; i < source.Items.Count; i++)
        {
            ProductDto dto = source.Items[i];

            try
            {
                OrderLine line = OrderLine.Create(
                    dto.Id,
                    dto.Name,
                    dto.Price,
                    1);

                items.Add(line);
            }
            catch (ArgumentException ex)
            {
                errors.Add(
                    $"Імпортований запис {i + 1}: {ex.Message}");
            }
        }

        return new ImportResult<OrderLine>(items, errors);
    }
}
