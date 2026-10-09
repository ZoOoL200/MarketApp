using MarketApp.Application.Common;
using MarketApp.Application.DTOs.Sales;
namespace MarketApp.Application.Interfaces.Services;
public interface IInventoryOperationsQueries
{
    Task<PagedResult<ReturnDto>> ReturnsAsync(Guid branchId, PageQuery page, CancellationToken ct);
    Task<PagedResult<AdjustmentDto>> AdjustmentsAsync(PageQuery page, CancellationToken ct);
    Task<BranchReportDto> ReportAsync(Guid branchId, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}
