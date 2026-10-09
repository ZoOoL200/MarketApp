using MarketApp.Application.Common;
using MarketApp.Application.DTOs.Sales;
namespace MarketApp.Application.Interfaces.Services;
public interface ISalesQueries
{
    Task<PagedResult<CatalogProductDto>> CatalogAsync(Guid branchId, PageQuery page, CancellationToken ct);
    Task<PagedResult<CatalogLocationDto>> LocationsAsync(Guid branchId, PageQuery page, CancellationToken ct);
    Task<PagedResult<CatalogStockDto>> StockAsync(Guid branchId, Guid? locationId, PageQuery page, CancellationToken ct);
    Task<PagedResult<SaleDto>> SalesAsync(Guid branchId, Guid? sellerId, PageQuery page, CancellationToken ct);
    Task<SaleDto?> SaleAsync(Guid branchId, Guid saleId, Guid? sellerId, CancellationToken ct);
    Task<bool> IsAssignedUserAsync(Guid branchId, Guid userId, string role, CancellationToken ct);
}
