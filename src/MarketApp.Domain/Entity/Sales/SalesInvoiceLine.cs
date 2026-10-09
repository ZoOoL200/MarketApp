using MarketApp.Domain.Entity.Main;

namespace MarketApp.Domain.Entity.Sales;

public class SalesInvoiceLine
{
    public decimal? PurchaseUnitCostSnapshot { get; set; }
    public Guid? PurchaseCostRecordedByUserId { get; set; }
    public DateTime? PurchaseCostRecordedAtUtc { get; set; }
    public string? PurchaseCostRecordHash { get; set; }
    public string? PurchaseCostReason { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public uint Version { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesInvoiceId { get; set; }
    public SalesInvoice SalesInvoice { get; set; } = null!;
    public int LineNumber { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ProductNameSnapshot { get; set; } = "";
    public string ProductSkuSnapshot { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal BaselineUnitPriceSnapshot { get; set; }
    public decimal MinimumSellingPriceSnapshot { get; set; }
    public decimal SellingUnitPrice { get; set; }
    public int PriceRevisionSnapshot { get; set; }
}
