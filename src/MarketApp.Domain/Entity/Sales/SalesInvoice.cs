using MarketApp.Domain.Entity.Main;

namespace MarketApp.Domain.Entity.Sales;

public class SalesInvoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = "";
    public Guid ClientSaleId { get; set; }
    public string RequestHash { get; set; } = "";
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public Guid InventoryLocationId { get; set; }
    public InventoryLocation InventoryLocation { get; set; } = null!;
    public Guid SoldByUserId { get; set; }
    public DateTime SoldAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();
}
