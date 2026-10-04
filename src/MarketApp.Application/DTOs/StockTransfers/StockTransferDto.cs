namespace MarketApp.Application.DTOs.StockTransfers;

public record StockTransferLineDto(
    Guid Id,
    int LineNumber,
    Guid ProductId,
    decimal Quantity,
    decimal BaselineUnitPrice,
    decimal BaselineAmount);

public record StockTransferDto(
    Guid Id,
    string Number,
    Guid SourceLocationId,
    Guid DestinationLocationId,
    string Status,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? ShippedAtUtc,
    DateTime? ReceivedAtUtc,
    string? ReturnReason,
    DateTime? ReturnRequestedAtUtc,
    DateTime? ReturnedAtUtc,
    decimal TotalBaselineAmount,
    IReadOnlyList<StockTransferLineDto> Lines);