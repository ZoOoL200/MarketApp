using MarketApp.Domain.Entity.Main;
using MarketApp.Domain.Enums;

namespace MarketApp.Domain.Entity.Inventory;

public class StockTransfer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Number { get; set; } = string.Empty;

    public Guid SourceLocationId { get; set; }
    // Navigation property for the source inventory location
    public InventoryLocation SourceLocation { get; set; } = null!;

    public Guid DestinationLocationId { get; set; }
    // Navigation property for the destination inventory location
    public InventoryLocation DestinationLocation { get; set; } = null!;

    public StockTransferStatus Status { get; set; }
        = StockTransferStatus.Draft;

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ShippedAtUtc { get; set; }

    public DateTime? ReceivedAtUtc { get; set; }

    public uint Version { get; set; }

    // Navigation property for the stock transfer lines
    public ICollection<StockTransferLine> Lines { get; set; }
        = new List<StockTransferLine>();
}