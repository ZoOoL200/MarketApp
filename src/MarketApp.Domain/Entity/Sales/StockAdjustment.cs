using MarketApp.Domain.Entity.Main;

namespace MarketApp.Domain.Entity.Sales;

public class StockAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientAdjustmentId { get; set; }
    public string RequestHash { get; set; } = "";
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid InventoryLocationId { get; set; }
    public InventoryLocation InventoryLocation { get; set; } = null!;
    public decimal QuantityChange { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = "";
}
