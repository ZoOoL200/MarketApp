using System.ComponentModel.DataAnnotations;
namespace MarketApp.Application.DTOs.Sales;

public sealed class SyncSaleDto
{
    public DateTime SoldAtUtc { get; set; }
    [Required] public CreateSaleDto Sale { get; set; } = new();
}
public sealed class ReconcileSaleDto
{
    public Guid OriginalSellerUserId { get; set; }
    [Required] public SyncSaleDto Submission { get; set; } = new();
}
public sealed class CreateReturnDto
{
    public Guid ClientReturnId { get; set; }
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; } = "";
    [Required, MinLength(1), MaxLength(100)] public List<CreateReturnLineDto> Lines { get; set; } = [];
}
public sealed class CreateReturnLineDto
{
    public Guid SalesInvoiceLineId { get; set; }
    [Range(typeof(decimal), "0.001", "1000000")] public decimal Quantity { get; set; }
    public bool Restock { get; set; } = true;
}
public sealed class CreateAdjustmentDto
{
    public Guid ClientAdjustmentId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public Guid ProductId { get; set; }
    [Range(typeof(decimal), "-1000000", "1000000")] public decimal QuantityChange { get; set; }
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; } = "";
}
public record ReturnLineDto(Guid SalesInvoiceLineId, decimal Quantity, bool Restock, decimal Refund);
public record ReturnDto(Guid Id, string Number, Guid ClientReturnId, Guid SalesInvoiceId, Guid CreatedByUserId,
    DateTime CreatedAtUtc, string Reason, decimal Refund, IReadOnlyList<ReturnLineDto> Lines);
public record AdjustmentDto(Guid Id, Guid ClientAdjustmentId, Guid ProductId, Guid InventoryLocationId,
    decimal QuantityChange, string Reason, Guid CreatedByUserId, DateTime CreatedAtUtc);
public record BranchReportDto(Guid BranchId, DateTime FromUtc, DateTime ToUtc, int SaleCount,
    decimal SalesRevenue, decimal Refunds, decimal NetRevenue, decimal NetBaselineValue, decimal GrossProfit)
{
    public decimal ExpensesTotal { get; init; }
    public decimal NetProfit { get; init; }
}
