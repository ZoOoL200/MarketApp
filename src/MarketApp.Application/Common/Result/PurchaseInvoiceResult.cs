using MarketApp.Application.DTOs.Purchases;

namespace MarketApp.Application.Common.Results;

public enum PurchaseInvoiceResultStatus
{
    Success,
    NotFound,
    InvalidRequest,
    Conflict
}

public record PurchaseInvoiceResult(
    PurchaseInvoiceResultStatus Status,
    PurchaseInvoiceDto? Invoice = null,
    string? Error = null);