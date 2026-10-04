using MarketApp.Application.DTOs.Pricing;

namespace MarketApp.Application.Common.Results;

public enum BranchPriceResultStatus
{
    Success,
    InvalidRequest,
    Conflict
}

public record BranchPriceResult(
    BranchPriceResultStatus Status,
    BranchProductPriceDto? Price = null,
    string? Error = null);