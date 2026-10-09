using MarketApp.Domain.Entity.Main;

namespace MarketApp.Domain.Entity.Sales;

public class SalesReturnLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesReturnId { get; set; }
    public SalesReturn SalesReturn { get; set; } = null!;
    public Guid SalesInvoiceLineId { get; set; }
    public SalesInvoiceLine SalesInvoiceLine { get; set; } = null!;
    public decimal Quantity { get; set; }
    public bool Restock { get; set; }
}
