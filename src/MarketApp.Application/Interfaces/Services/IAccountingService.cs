using MarketApp.Application.DTOs.Sales;
namespace MarketApp.Application.Interfaces.Services;
public interface IAccountingService
{
    Task<StockCostDto> InitializeCostAsync(Guid actorId, InitializeStockCostDto request, CancellationToken ct);
    Task RecordHistoricalCostAsync(Guid branchId, Guid saleId, Guid lineId, Guid actorId, RecordHistoricalCostDto request, CancellationToken ct);
    Task<ExpenseDto> CreateExpenseAsync(Guid branchId, Guid actorId, CreateExpenseDto request, CancellationToken ct);
    Task<ExpenseDto> VoidExpenseAsync(Guid branchId, Guid expenseId, Guid actorId, VoidExpenseDto request, CancellationToken ct);
}
