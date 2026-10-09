using MarketApp.Application.Common;
using MarketApp.Application.Common.Exceptions;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Services;
using MarketApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace MarketApp.Infrastructure.Services;
public sealed class AccountingQueries(AppDbContext db) : IAccountingQueries
{
    private static IQueryable<T> Page<T>(IQueryable<T> query, PageQuery page)
    {
        if (page.PageNumber < 1 || page.PageNumber > 1000000 || page.PageSize < 1 || page.PageSize > 100)
            throw new RequestException(400, "PageNumber must be 1–1000000 and PageSize 1–100.");
        return query.Skip((page.PageNumber - 1) * page.PageSize).Take(page.PageSize);
    }
    public async Task<PagedResult<StockCostDto>> CostsAsync(Guid? locationId, PageQuery page, CancellationToken ct)
    {
        var q = db.StockBalances.AsNoTracking().Where(b => locationId == null || b.InventoryLocationId == locationId);
        var count = await q.CountAsync(ct);
        var items = await Page(q.OrderBy(b => b.InventoryLocationId).ThenBy(b => b.ProductId), page)
            .Select(b => new StockCostDto(b.ProductId, b.InventoryLocationId, b.Quantity, b.AveragePurchaseUnitCost)).ToListAsync(ct);
        return new(items, count, page.PageNumber, page.PageSize);
    }
    public async Task<PagedResult<ExpenseDto>> ExpensesAsync(Guid branchId, PageQuery page, CancellationToken ct)
    {
        var q = db.BranchExpenses.AsNoTracking().Where(e => e.BranchId == branchId);
        var count = await q.CountAsync(ct);
        var items = await Page(q.OrderByDescending(e => e.OccurredAtUtc).ThenBy(e => e.Id), page).ToListAsync(ct);
        return new(items.Select(AccountingService.ToDto).ToList(), count, page.PageNumber, page.PageSize);
    }
    public async Task<IReadOnlyList<SaleAccountingLineDto>> SaleCostsAsync(Guid branchId, Guid saleId, CancellationToken ct)
    {
        await using var snapshot = await db.Database.BeginTransactionAsync(db.Database.IsNpgsql() ? System.Data.IsolationLevel.RepeatableRead : System.Data.IsolationLevel.Serializable, ct);
        var sale = await db.SalesInvoices.AsNoTracking().Include(s => s.Lines).SingleOrDefaultAsync(s => s.Id == saleId && s.BranchId == branchId, ct)
            ?? throw new RequestException(404, "Sale not found.");
        // Preserve accounting for legacy non-restocked returns; new returns always restock.
        var returns = await db.SalesReturnLines.AsNoTracking().Where(l => l.SalesReturn.SalesInvoiceId == saleId && l.Restock)
            .Select(l => new { l.SalesInvoiceLineId, l.Quantity }).ToListAsync(ct);
        return sale.Lines.OrderBy(l => l.LineNumber).Select(l => {
            var netStock = l.Quantity - returns.Where(x => x.SalesInvoiceLineId == l.Id).Sum(x => x.Quantity);
            return new SaleAccountingLineDto(l.Id, l.ProductId, l.Quantity, l.ReturnedQuantity, l.PurchaseUnitCostSnapshot,
                l.BaselineUnitPriceSnapshot, l.SellingUnitPrice, l.PurchaseUnitCostSnapshot is decimal cost ? netStock * (l.BaselineUnitPriceSnapshot - cost) : null,
                (l.Quantity - l.ReturnedQuantity) * l.SellingUnitPrice - netStock * l.BaselineUnitPriceSnapshot,
                l.PurchaseCostRecordedByUserId, l.PurchaseCostRecordedAtUtc, l.PurchaseCostReason);
        }).ToList();
    }
    private sealed class Totals
    {
        public decimal Baseline, ReturnedBaseline, Cost;
        public int Missing;
    }
    public async Task<StakeholderProfitDto> StakeholderProfitAsync(Guid? branchId, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        if (fromUtc.Kind != DateTimeKind.Utc || toUtc.Kind != DateTimeKind.Utc || fromUtc >= toUtc || toUtc - fromUtc > TimeSpan.FromDays(31))
            throw new RequestException(400, "Use UTC dates (Z), fromUtc < toUtc, and a range of at most 31 days. The end is exclusive.");
        await using var snapshot = await db.Database.BeginTransactionAsync(db.Database.IsNpgsql() ? System.Data.IsolationLevel.RepeatableRead : System.Data.IsolationLevel.Serializable, ct);
        var branches = await db.Branches.AsNoTracking().Where(b => branchId == null || b.Id == branchId).OrderBy(b => b.Name).ToListAsync(ct);
        if (branchId is not null && branches.Count == 0) throw new RequestException(404, "Branch not found.");
        var totals = branches.ToDictionary(b => b.Id, b => new Totals());
        await foreach (var l in db.SalesInvoiceLines.AsNoTracking().Where(l => (branchId == null || l.SalesInvoice.BranchId == branchId) &&
            l.SalesInvoice.SoldAtUtc >= fromUtc && l.SalesInvoice.SoldAtUtc < toUtc)
            .Select(l => new { l.SalesInvoice.BranchId, l.Quantity, l.BaselineUnitPriceSnapshot, l.PurchaseUnitCostSnapshot })
            .AsAsyncEnumerable().WithCancellation(ct))
        {
            var t = totals[l.BranchId]; t.Baseline += l.Quantity * l.BaselineUnitPriceSnapshot;
            if (l.PurchaseUnitCostSnapshot is decimal cost) t.Cost += l.Quantity * cost; else t.Missing++;
        }
        await foreach (var l in db.SalesReturnLines.AsNoTracking().Where(l => l.Restock && (branchId == null || l.SalesReturn.BranchId == branchId) &&
            l.SalesReturn.CreatedAtUtc >= fromUtc && l.SalesReturn.CreatedAtUtc < toUtc)
            .Select(l => new { l.SalesReturn.BranchId, l.Quantity, l.SalesInvoiceLine.BaselineUnitPriceSnapshot, l.SalesInvoiceLine.PurchaseUnitCostSnapshot })
            .AsAsyncEnumerable().WithCancellation(ct))
        {
            var t = totals[l.BranchId]; t.ReturnedBaseline += l.Quantity * l.BaselineUnitPriceSnapshot;
            if (l.PurchaseUnitCostSnapshot is decimal cost) t.Cost -= l.Quantity * cost; else t.Missing++;
        }
        var rows = branches.Select(b => { var t = totals[b.Id]; return new StakeholderBranchProfitDto(b.Id, b.Name,
            t.Baseline, t.ReturnedBaseline, t.Baseline - t.ReturnedBaseline, t.Missing == 0 ? t.Cost : null,
            t.Missing == 0 ? t.Baseline - t.ReturnedBaseline - t.Cost : null, t.Missing); }).ToList();
        var missing = rows.Sum(x => x.MissingCostEntries);
        return new(fromUtc, toUtc, branchId, rows.Sum(x => x.NetBaselineValue), missing == 0 ? rows.Sum(x => x.NetPurchaseCost) : null,
            missing == 0 ? rows.Sum(x => x.GrossProfit) : null, missing, rows);
    }
}
