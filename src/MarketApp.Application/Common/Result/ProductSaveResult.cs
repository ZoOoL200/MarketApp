using MarketApp.Application.DTOs.Products;

namespace MarketApp.Application.Common.Result;

public enum ProductSaveStatus
{
    Success,
    NotFound,
    CategoryNotFound,
    DuplicateSku
}

public record ProductSaveResult(
    ProductSaveStatus Status,
    ProductDto? Product = null);