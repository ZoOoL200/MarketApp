using MarketApp.Application.DTOs.StockTransfers;

namespace MarketApp.Application.Common.Results;

public enum StockTransferResultStatus
{
    Success,
    NotFound,
    InvalidRequest,
    Conflict
}

public record StockTransferResult(
    StockTransferResultStatus Status,
    StockTransferDto? Transfer = null,
    string? Error = null);