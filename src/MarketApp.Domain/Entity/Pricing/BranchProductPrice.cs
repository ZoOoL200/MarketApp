using MarketApp.Domain.Entity.Main;

namespace MarketApp.Domain.Entity.Pricing;

public class BranchProductPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BranchId { get; set; }

    public Branch Branch { get; set; } = null!;

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public decimal? BaselineUnitPrice { get; set; }

    public decimal? MinimumSellingPrice { get; set; }

    public int Revision { get; set; } = 1;

    // The general revision at the last baseline change; zero means unset.
    // Minimum-selling-price changes do not change this value.
    public int BaselineRevision { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public uint Version { get; set; }
}
