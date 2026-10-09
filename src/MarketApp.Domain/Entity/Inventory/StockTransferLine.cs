using MarketApp.Domain.Entity.Main;

namespace MarketApp.Domain.Entity.Inventory;

public class StockTransferLine
{
    public decimal? PurchaseUnitCostSnapshot { get; set; }

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StockTransferId { get; set; }
    // Navigation property for the associated stock transfer
    public StockTransfer StockTransfer { get; set; } = null!;

    public int LineNumber { get; set; }

    public Guid ProductId { get; set; }
    // Navigation property for the associated product
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal BaselineUnitPrice { get; set; }

    // Captured from the destination branch when the transfer is created.
    // Null identifies a legacy transfer whose pricing must not be overwritten.
    public int? BaselineRevisionAtCreation { get; set; }
}
