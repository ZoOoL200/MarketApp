using MarketApp.Domain.Entity.Main;

namespace MarketApp.Domain.Entity.Purchasing;

public class PurchaseInvoiceLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PurchaseInvoiceId { get; set; }

    // Navigation property to the PurchaseInvoice entity
    public PurchaseInvoice PurchaseInvoice { get; set; } = null!;

    public int LineNumber { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }
}