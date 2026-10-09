using MarketApp.Application.Common;
using MarketApp.Application.DTOs.Sales;
namespace MarketApp.Application.Interfaces.Services;
public interface IAccountingQueries
{
    Task<PagedResult<StockCostDto>> CostsAsync(Guid? locationId, PageQuery page, CancellationToken ct);
    Task<PagedResult<ExpenseDto>> ExpensesAsync(Guid branchId, PageQuery page, CancellationToken ct);
    Task<IReadOnlyList<SaleAccountingLineDto>> SaleCostsAsync(Guid branchId, Guid saleId, CancellationToken ct);
    Task<StakeholderProfitDto> StakeholderProfitAsync(Guid? branchId, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}
