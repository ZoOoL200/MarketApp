namespace MarketApp.Application.DTOs.Stock;

public record StockMovementDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string Sku,
    Guid InventoryLocationId,
    string InventoryLocationName,
    string MovementType,
    decimal QuantityChange,
    DateTime OccurredAtUtc,
    Guid? PurchaseInvoiceId,
    Guid? PurchaseInvoiceLineId,
    Guid? StockTransferId,
    Guid? StockTransferLineId)
{
    public Guid? SalesInvoiceLineId { get; init; }
    public Guid? SalesReturnLineId { get; init; }
    public Guid? StockAdjustmentId { get; init; }
}