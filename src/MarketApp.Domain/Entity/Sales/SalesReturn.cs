using MarketApp.Domain.Entity.Main;

namespace MarketApp.Domain.Entity.Sales;

public class SalesReturn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = "";
    public Guid ClientReturnId { get; set; }
    public string RequestHash { get; set; } = "";
    public Guid SalesInvoiceId { get; set; }
    public SalesInvoice SalesInvoice { get; set; } = null!;
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = "";
    public ICollection<SalesReturnLine> Lines { get; set; } = new List<SalesReturnLine>();
}
