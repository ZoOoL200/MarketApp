namespace MarketApp.Application.DTOs.Pricing;

public record SellerProductPriceDto(
    Guid BranchId,
    Guid ProductId,
    decimal MinimumSellingPrice,
    int PriceRevision,
    DateTime UpdatedAtUtc);