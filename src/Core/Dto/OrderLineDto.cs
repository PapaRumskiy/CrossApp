using System.Text.Json.Serialization;

namespace Core.Dto;

public sealed record OrderLineDto(
    string ProductId,
    string Name,
    [property: JsonRequired] decimal Price,
    int Quantity);
