using MarketApp.Domain.Entity.Main;
using MarketApp.Domain.Entity.Purchasing;
using MarketApp.Domain.Enums;

namespace MarketApp.Domain.Entity.Inventory;

public class StockMovement
{
    public Guid? SalesInvoiceLineId { get; set; }
    public MarketApp.Domain.Entity.Sales.SalesInvoiceLine? SalesInvoiceLine { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public Guid InventoryLocationId { get; set; }

    public InventoryLocation InventoryLocation { get; set; } = null!;

    public StockMovementType MovementType { get; set; }

    public decimal QuantityChange { get; set; }

    public Guid? PurchaseInvoiceLineId { get; set; }

    public PurchaseInvoiceLine? PurchaseInvoiceLine { get; set; }

    public Guid? StockTransferLineId { get; set; }

    public StockTransferLine? StockTransferLine { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}