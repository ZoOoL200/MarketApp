using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Enums;

namespace MarketApp.Domain.Entity.Pricing;

public class BranchProductPriceHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BranchProductPriceId { get; set; }

    public BranchProductPrice BranchProductPrice { get; set; } = null!;

    public int Revision { get; set; }

    public decimal? OldBaselineUnitPrice { get; set; }

    public decimal? NewBaselineUnitPrice { get; set; }

    public decimal? OldMinimumSellingPrice { get; set; }

    public decimal? NewMinimumSellingPrice { get; set; }

    public BranchPriceChangeType ChangeType { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? ChangedByUserId { get; set; }

    public Guid? StockTransferLineId { get; set; }

    public StockTransferLine? StockTransferLine { get; set; }
}