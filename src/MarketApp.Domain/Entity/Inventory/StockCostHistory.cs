namespace MarketApp.Domain.Entity.Inventory;
public class StockCostHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public decimal? AveragePurchaseUnitCost { get; set; }
    public decimal QuantityAtChange { get; set; }
    public DateTime EffectiveAtUtc { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = "";
    public Guid? RecordedByUserId { get; set; }
    public Guid? ClientChangeId { get; set; }
    public string? RequestHash { get; set; }
}
