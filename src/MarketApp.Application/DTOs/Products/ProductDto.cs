namespace MarketApp.Application.DTOs.Products;

public record ProductDto(
    Guid Id,
    string Name,
    string Sku,
    bool IsActive,
    Guid CategoryId,
    string CategoryName);