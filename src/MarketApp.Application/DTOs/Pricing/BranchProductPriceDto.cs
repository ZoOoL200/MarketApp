namespace MarketApp.Application.DTOs.Pricing;

public record BranchProductPriceDto(
    Guid Id,
    Guid BranchId,
    Guid ProductId,
    decimal? BaselineUnitPrice,
    decimal? MinimumSellingPrice,
    int Revision,
    DateTime UpdatedAtUtc);