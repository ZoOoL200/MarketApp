using MarketApp.Application.Common;
using MarketApp.Application.DTOs.Stock;

namespace MarketApp.Application.Interfaces.Services;

public interface IStockService
{
    Task<PagedResult<StockBalanceDto>> GetBalancesAsync(
        StockQueryDto query,
        CancellationToken cancellationToken = default);

    Task<PagedResult<StockMovementDto>> GetMovementsAsync(
        StockQueryDto query,
        CancellationToken cancellationToken = default);
}