namespace MarketApp.Application.DTOs.Purchases;

public record PurchaseInvoiceLineDto(
    Guid Id,
    int LineNumber,
    Guid ProductId,
    decimal Quantity,
    decimal UnitCost,
    decimal LineAmount);

public record PurchaseInvoiceDto(
    Guid Id,
    string Number,
    string? SupplierInvoiceNumber,
    Guid SupplierId,
    Guid InventoryLocationId,
    DateOnly InvoiceDate,
    string Status,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? PostedAtUtc,
    decimal TotalAmount,
    IReadOnlyList<PurchaseInvoiceLineDto> Lines);