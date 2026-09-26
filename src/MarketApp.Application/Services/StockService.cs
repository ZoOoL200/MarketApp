using MarketApp.Application.Common;
using MarketApp.Application.DTOs.Stock;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;

namespace MarketApp.Application.Services;

public class StockService : IStockService
{
    private readonly IUnitOfWork _unitOfWork;

    public StockService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<StockBalanceDto>> GetBalancesAsync(
        StockQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var page = await _unitOfWork.StockBalances.GetPageAsync(
            pageNumber: query.PageNumber,
            pageSize: query.PageSize,
            orderBy: balances => balances
                .OrderBy(s => s.Product.Name)
                .ThenBy(s => s.InventoryLocation.Code)
                .ThenBy(s => s.Id),
            predicate: s =>
                (!query.ProductId.HasValue ||
                    s.ProductId == query.ProductId.Value) &&
                (!query.InventoryLocationId.HasValue ||
                    s.InventoryLocationId ==
                        query.InventoryLocationId.Value),
            includes:
            [
                s => s.Product,
                s => s.InventoryLocation
            ],
            trackChanges: false,
            cancellationToken: cancellationToken);

        var items = page.Items
            .Select(s => new StockBalanceDto(
                s.Id,
                s.ProductId,
                s.Product.Name,
                s.Product.Sku,
                s.InventoryLocationId,
                s.InventoryLocation.Name,
                s.InventoryLocation.Code,
                s.Quantity))
            .ToList();

        return new PagedResult<StockBalanceDto>(
            items,
            page.TotalCount,
            page.PageNumber,
            page.PageSize);
    }

    public async Task<PagedResult<StockMovementDto>> GetMovementsAsync(
    StockQueryDto query,
    CancellationToken cancellationToken = default)
    {
        var page = await _unitOfWork.StockMovements.GetPageAsync(
            pageNumber: query.PageNumber,
            pageSize: query.PageSize,
            orderBy: movements => movements
                .OrderByDescending(m => m.OccurredAtUtc)
                .ThenByDescending(m => m.Id),
            predicate: m =>
                (!query.ProductId.HasValue ||
                    m.ProductId == query.ProductId.Value) &&
                (!query.InventoryLocationId.HasValue ||
                    m.InventoryLocationId ==
                        query.InventoryLocationId.Value),
            includes:
            [
                m => m.Product,
            m => m.InventoryLocation,
            m => m.PurchaseInvoiceLine!,
            m => m.StockTransferLine!
            ],
            trackChanges: false,
            cancellationToken: cancellationToken);

        var items = page.Items
            .Select(m => new StockMovementDto(
                m.Id,
                m.ProductId,
                m.Product.Name,
                m.Product.Sku,
                m.InventoryLocationId,
                m.InventoryLocation.Name,
                m.MovementType.ToString(),
                m.QuantityChange,
                m.OccurredAtUtc,
                m.PurchaseInvoiceLine?.PurchaseInvoiceId,
                m.PurchaseInvoiceLineId,
                m.StockTransferLine?.StockTransferId,
                m.StockTransferLineId))
            .ToList();

        return new PagedResult<StockMovementDto>(
            items,
            page.TotalCount,
            page.PageNumber,
            page.PageSize);
    }
}