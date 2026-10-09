using System.ComponentModel.DataAnnotations;
namespace MarketApp.Application.DTOs.Sales;
public sealed class InitializeStockCostDto
{
    public Guid ClientChangeId { get; set; }
    public Guid ProductId { get; set; }
    public Guid InventoryLocationId { get; set; }
    [Range(typeof(decimal), "0", "999999999999999.999")] public decimal ExpectedQuantity { get; set; }
    [Required, Range(typeof(decimal), "0", "1000000000")] public decimal? PurchaseUnitCost { get; set; }
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; } = "";
}
public sealed class RecordHistoricalCostDto
{
    [Required, Range(typeof(decimal), "0", "1000000000")] public decimal? PurchaseUnitCost { get; set; }
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; } = "";
}
public sealed class CreateExpenseDto
{
    public Guid ClientExpenseId { get; set; }
    [Required, RegularExpression("^(Salary|Other)$")] public string Category { get; set; } = "Other";
    public Guid? EmployeeUserId { get; set; }
    [Range(typeof(decimal), "0.0001", "1000000000000")] public decimal Amount { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    [Required, StringLength(1000, MinimumLength = 3)] public string Description { get; set; } = "";
}
public sealed class VoidExpenseDto
{
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; } = "";
}
public record StockCostDto(Guid ProductId, Guid InventoryLocationId, decimal Quantity, decimal? AveragePurchaseUnitCost);
public record ExpenseDto(Guid Id, Guid BranchId, Guid ClientExpenseId, string Category, Guid? EmployeeUserId,
    decimal Amount, DateTime OccurredAtUtc, string Description, Guid CreatedByUserId, DateTime CreatedAtUtc,
    Guid? VoidedByUserId, DateTime? VoidedAtUtc, string? VoidReason);
public record SaleAccountingLineDto(Guid Id, Guid ProductId, decimal Quantity, decimal ReturnedQuantity,
    decimal? PurchaseUnitCost, decimal BaselineUnitPrice, decimal SellingUnitPrice, decimal? StakeholderProfit,
    decimal BranchGrossProfit, Guid? CostRecordedByUserId, DateTime? CostRecordedAtUtc, string? CostReason);
public record StakeholderBranchProfitDto(Guid BranchId, string BranchName, decimal BaselineSalesValue,
    decimal ReturnedBaselineValue, decimal NetBaselineValue, decimal? NetPurchaseCost, decimal? GrossProfit, int MissingCostEntries);
public record StakeholderProfitDto(DateTime FromUtc, DateTime ToUtc, Guid? BranchId, decimal NetBaselineValue,
    decimal? NetPurchaseCost, decimal? GrossProfit, int MissingCostEntries, IReadOnlyList<StakeholderBranchProfitDto> Branches);
