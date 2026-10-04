namespace MarketApp.Application.DTOs.Pricing;

public record BranchProductPriceHistoryDto(
    Guid Id,
    int Revision,
    decimal? OldBaselineUnitPrice,
    decimal? NewBaselineUnitPrice,
    decimal? OldMinimumSellingPrice,
    decimal? NewMinimumSellingPrice,
    string ChangeType,
    string Reason,
    DateTime ChangedAtUtc,
    Guid? ChangedByUserId,
    Guid? StockTransferLineId);