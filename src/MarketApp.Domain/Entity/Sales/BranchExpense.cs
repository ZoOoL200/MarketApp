using MarketApp.Domain.Entity.Main;
namespace MarketApp.Domain.Entity.Sales;
public class BranchExpense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public Guid ClientExpenseId { get; set; }
    public string RequestHash { get; set; } = "";
    public string Category { get; set; } = "Other";
    public Guid? EmployeeUserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string Description { get; set; } = "";
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? VoidedByUserId { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    public string? VoidReason { get; set; }
    public uint Version { get; set; }
}
