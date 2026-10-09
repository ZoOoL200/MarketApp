using MarketApp.Domain.Entity.Main;

namespace MarketApp.Domain.Entity.Sales;

public class ManagerProfitShare
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientChangeId { get; set; }
    public string RequestHash { get; set; } = "";
    public string Reason { get; set; } = "";
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public Guid ManagerUserId { get; set; }
    public decimal Percent { get; set; }
    public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
}
