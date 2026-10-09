using MarketApp.Domain.Entity.Main;

namespace MarketApp.Domain.Entity.Inventory;

public class StockBalance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }

    // navigational property for the Product entity
    public Product Product { get; set; } = null!;

    public Guid InventoryLocationId { get; set; }

    // navigational property for the InventoryLocation entity
    public InventoryLocation InventoryLocation { get; set; } = null!;

    public decimal? AveragePurchaseUnitCost { get; set; }

    public decimal Quantity { get; set; }

    public uint Version { get; set; }
}