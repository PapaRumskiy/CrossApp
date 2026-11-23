using Core.Dto;
using Core.Import;
using Core.Services;
using Core.Storage;

if (ReportCommands.CanHandle(args))
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    return ReportCommands.Run(args);
}

if (args.Length > 0 && args[0] == "--lab06")
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    try
    {
        Lab06Demo.Run();
        return 0;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

if (args.Length > 0 && args[0] == "--lab05")
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    try
    {
        var store = StoreFactory.Create(args);
        var cached = new CachingOrderStore(store);
        var service = new OrderService(cached);
        Lab05Demo.Run(service, store.GetType().Name, cached);
        return 0;
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}


if (args.Length > 0 && args[0] == "--lab04")
{
    Lab04Demo.Run();
    return 0;
}


string path = args.Length > 0
    ? args[0]
    : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine(
        $"Файл не знайдено: {Path.GetFullPath(path)}");

    return 1;
}

if (Path.GetFileName(path).Equals(
        "sample-mixed.csv",
        StringComparison.OrdinalIgnoreCase))
{
    ImportResult<object> result = MixedCsvImporter.Load(path);

    Console.WriteLine(
        $"Завантажено записів: {result.Items.Count}");

    foreach (object item in result.Items.Take(5))
    {
        switch (item)
        {
            case ProductDto product:
                Console.WriteLine(
                    $" Товар: {product.Id,-6} {product.Name,-20} {product.Price,10:F2}");
                break;

            case WarehouseDto warehouse:
                Console.WriteLine(
                    $" Склад: {warehouse.Id,-6} {warehouse.Name}");
                break;
        }
    }

    PrintStatistics(
        result.Items.Count + result.Errors.Count,
        result.Items.Count,
        result.Errors.Count);

    PrintErrors(result.Errors);

    return 0;
}

ImportResult<ProductDto> productResult =
    Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".csv" => ProductCsvImporter.Load(path),
        ".json" => ProductJsonImporter.Load(path),
        _ => new ImportResult<ProductDto>(
            [],
            [$"Непідтримуваний формат файлу: {Path.GetExtension(path)}"])
    };

Console.WriteLine(
    $"Завантажено записів: {productResult.Items.Count}");

foreach (ProductDto product in productResult.Items.Take(5))
{
    Console.WriteLine(
        $" {product.Id,-6} {product.Name,-20} {product.Price,10:F2}");
}

PrintStatistics(
    productResult.Items.Count + productResult.Errors.Count,
    productResult.Items.Count,
    productResult.Errors.Count);

PrintErrors(productResult.Errors);

return 0;


static void PrintStatistics(
    int total,
    int accepted,
    int skipped)
{
    double errorPercentage =
        total == 0
            ? 0
            : skipped * 100.0 / total;

    Console.WriteLine(
        $"Усього: {total} | Прийнято: {accepted} | " +
        $"Пропущено: {skipped} | Помилки: {errorPercentage:F1}%");
}

static void PrintErrors(IReadOnlyList<string> errors)
{
    if (errors.Count == 0)
        return;

    Console.WriteLine(
        $"Пропущено рядків: {errors.Count}");

    foreach (string error in errors)
        Console.WriteLine($" ! {error}");
}
