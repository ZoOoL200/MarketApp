using MarketApp.Application.Common;
using MarketApp.Application.Common.Exceptions;
using MarketApp.Application.DTOs.Products;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Services;
using MarketApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketApp.Infrastructure.Services;

public sealed class SalesQueries(AppDbContext db) : ISalesQueries
{
    private static IQueryable<T> Page<T>(IQueryable<T> query, PageQuery page)
    {
        if (page.PageNumber < 1 || page.PageNumber > 1000000 || page.PageSize < 1 || page.PageSize > 100)
            throw new RequestException(400, "PageNumber must be 1–1000000 and PageSize 1–100.");
        return query.Skip((page.PageNumber - 1) * page.PageSize).Take(page.PageSize);
    }
    public async Task<PagedResult<CatalogProductDto>> CatalogAsync(Guid branchId, PageQuery page, CancellationToken ct)
    {
        var term = page.Search?.Trim(); var code = term?.ToUpperInvariant();
        var query = db.Products.AsNoTracking().Where(p => p.IsActive && (string.IsNullOrEmpty(term) ||
            p.Sku == code || p.Name.Contains(term)));
        var count = await query.CountAsync(ct);
        var products = await Page(query.OrderBy(p => p.Name).ThenBy(p => p.Id), page).Include(p => p.Photos).ToListAsync(ct);
        var ids = products.Select(p => p.Id).ToArray();
        var prices = await db.BranchProductPrices.AsNoTracking().Where(p => p.BranchId == branchId && ids.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId, ct);
        return new(products.Select(p => new CatalogProductDto(p.Id, p.Name, p.Sku,
            prices.GetValueOrDefault(p.Id)?.MinimumSellingPrice, prices.GetValueOrDefault(p.Id)?.Revision ?? 0,
            p.Photos.Where(x => x.IsReady && x.DeletedAtUtc == null).OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
                .Select(ProductPhotoDto.FromEntity).ToList())).ToList(), count, page.PageNumber, page.PageSize);
    }
    public async Task<PagedResult<CatalogLocationDto>> LocationsAsync(Guid branchId, PageQuery page, CancellationToken ct)
    {
        var q = db.InventoryLocations.AsNoTracking().Where(l => l.BranchId == branchId && l.IsActive);
        var count = await q.CountAsync(ct);
        var items = await Page(q.OrderBy(l => l.Name).ThenBy(l => l.Id), page)
            .Select(l => new CatalogLocationDto(l.Id, l.Name, l.Code)).ToListAsync(ct);
        return new(items, count, page.PageNumber, page.PageSize);
    }
    public async Task<PagedResult<CatalogStockDto>> StockAsync(Guid branchId, Guid? locationId, PageQuery page, CancellationToken ct)
    {
        var query = db.StockBalances.AsNoTracking().Where(b => b.InventoryLocation.BranchId == branchId &&
            b.InventoryLocation.IsActive && (locationId == null || b.InventoryLocationId == locationId));
        var count = await query.CountAsync(ct);
        var items = await Page(query.OrderBy(x => x.ProductId).ThenBy(x => x.InventoryLocationId), page)
            .Select(x => new CatalogStockDto(x.ProductId, x.InventoryLocationId, x.Quantity)).ToListAsync(ct);
        return new(items, count, page.PageNumber, page.PageSize);
    }
    public async Task<PagedResult<SaleDto>> SalesAsync(Guid branchId, Guid? sellerId, PageQuery page, CancellationToken ct)
    {
        var q = db.SalesInvoices.AsNoTracking().Where(s => s.BranchId == branchId && (sellerId == null || s.SoldByUserId == sellerId));
        var count = await q.CountAsync(ct);
        var items = await Page(q.OrderByDescending(s => s.SoldAtUtc).ThenBy(s => s.Id), page).Include(s => s.Lines).ToListAsync(ct);
        return new(items.Select(SalesService.ToDto).ToList(), count, page.PageNumber, page.PageSize);
    }
    public async Task<SaleDto?> SaleAsync(Guid branchId, Guid saleId, Guid? sellerId, CancellationToken ct)
    {
        var sale = await db.SalesInvoices.AsNoTracking().Include(s => s.Lines).SingleOrDefaultAsync(s =>
            s.Id == saleId && s.BranchId == branchId && (sellerId == null || s.SoldByUserId == sellerId), ct);
        return sale is null ? null : SalesService.ToDto(sale);
    }
    public Task<bool> IsAssignedUserAsync(Guid branchId, Guid userId, string role, CancellationToken ct)
        => (from assignment in db.UserBranchAssignments
            join userRole in db.UserRoles on assignment.UserId equals userRole.UserId
            join r in db.Roles on userRole.RoleId equals r.Id
            where assignment.BranchId == branchId && assignment.UserId == userId && assignment.User.IsActive && r.Name == role
            select assignment.UserId).AnyAsync(ct);

}
