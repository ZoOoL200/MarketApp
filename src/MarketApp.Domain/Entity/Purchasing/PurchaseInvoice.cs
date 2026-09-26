using MarketApp.Domain.Entity.Main;
using MarketApp.Domain.Enums;

namespace MarketApp.Domain.Entity.Purchasing;

public class PurchaseInvoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Number { get; set; } = string.Empty;

    public string? SupplierInvoiceNumber { get; set; }

    public Guid SupplierId { get; set; }

    // Navigation property to the Supplier entity
    public Supplier Supplier { get; set; } = null!;

    public Guid InventoryLocationId { get; set; }

    // Navigation property to the InventoryLocation entity
    public InventoryLocation InventoryLocation { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }

    public PurchaseInvoiceStatus Status { get; set; }
        = PurchaseInvoiceStatus.Draft;

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? PostedAtUtc { get; set; }

    public uint Version { get; set; }

    // Navigation property to the collection of PurchaseInvoiceLine entities
    public ICollection<PurchaseInvoiceLine> Lines { get; set; }
        = new List<PurchaseInvoiceLine>();
}