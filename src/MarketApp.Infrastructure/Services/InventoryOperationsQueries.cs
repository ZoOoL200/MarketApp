using MarketApp.Application.Common;
using MarketApp.Application.Common.Exceptions;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Services;
using MarketApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketApp.Infrastructure.Services;

public sealed class InventoryOperationsQueries(AppDbContext db) : IInventoryOperationsQueries
{
    private static IQueryable<T> Page<T>(IQueryable<T> query, PageQuery page)
    {
        if (page.PageNumber < 1 || page.PageNumber > 1000000 || page.PageSize < 1 || page.PageSize > 100)
            throw new RequestException(400, "PageNumber must be 1–1000000 and PageSize 1–100.");
        return query.Skip((page.PageNumber - 1) * page.PageSize).Take(page.PageSize);
    }
    public async Task<PagedResult<ReturnDto>> ReturnsAsync(Guid branchId, PageQuery page, CancellationToken ct)
    {
        var q = db.SalesReturns.AsNoTracking().Where(r => r.BranchId == branchId);
        var count = await q.CountAsync(ct);
        var items = await Page(q.OrderByDescending(r => r.CreatedAtUtc).ThenBy(r => r.Id), page)
            .Include(r => r.Lines).ThenInclude(l => l.SalesInvoiceLine).ToListAsync(ct);
        return new(items.Select(r => InventoryOperationsService.ToDto(r, r.Lines.ToDictionary(l => l.SalesInvoiceLineId, l => l.SalesInvoiceLine))).ToList(),
            count, page.PageNumber, page.PageSize);
    }
    public async Task<PagedResult<AdjustmentDto>> AdjustmentsAsync(PageQuery page, CancellationToken ct)
    {
        var q = db.StockAdjustments.AsNoTracking(); var count = await q.CountAsync(ct);
        var items = await Page(q.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id), page).ToListAsync(ct);
        return new(items.Select(InventoryOperationsService.ToDto).ToList(), count, page.PageNumber, page.PageSize);
    }
    public async Task<BranchReportDto> ReportAsync(Guid branchId, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        if (fromUtc.Kind != DateTimeKind.Utc || toUtc.Kind != DateTimeKind.Utc || fromUtc >= toUtc || toUtc - fromUtc > TimeSpan.FromDays(31))
            throw new RequestException(400, "Use UTC dates (Z), fromUtc < toUtc, and a range of at most 31 days. The end is exclusive.");
        await using var snapshot = await db.Database.BeginTransactionAsync(db.Database.IsNpgsql() ? System.Data.IsolationLevel.RepeatableRead : System.Data.IsolationLevel.Serializable, ct);
        var sales = db.SalesInvoices.AsNoTracking().Where(s => s.BranchId == branchId && s.SoldAtUtc >= fromUtc && s.SoldAtUtc < toUtc);
        var count = await sales.CountAsync(ct);
        decimal revenue = 0, refunds = 0, baseline = 0;
        // Stream projections rather than loading complete invoices or Identity records.
        await foreach (var x in db.SalesInvoiceLines.AsNoTracking().Where(l => l.SalesInvoice.BranchId == branchId && l.SalesInvoice.SoldAtUtc >= fromUtc && l.SalesInvoice.SoldAtUtc < toUtc).Select(l => new {
            Revenue = l.Quantity * l.SellingUnitPrice, Baseline = l.Quantity * l.BaselineUnitPriceSnapshot }).AsAsyncEnumerable().WithCancellation(ct))
        {
            revenue += x.Revenue; baseline += x.Baseline;
        }
        await foreach (var x in db.SalesReturnLines.AsNoTracking().Where(l => l.SalesReturn.BranchId == branchId && l.SalesReturn.CreatedAtUtc >= fromUtc && l.SalesReturn.CreatedAtUtc < toUtc).Select(l => new {
            Refund = l.Quantity * l.SalesInvoiceLine.SellingUnitPrice,
            Baseline = l.Restock ? l.Quantity * l.SalesInvoiceLine.BaselineUnitPriceSnapshot : 0 }).AsAsyncEnumerable().WithCancellation(ct))
        {
            refunds += x.Refund; baseline -= x.Baseline;
        }
        decimal expenses = 0;
        await foreach (var amount in db.BranchExpenses.AsNoTracking().Where(e => e.BranchId == branchId &&
            e.VoidedAtUtc == null && e.OccurredAtUtc >= fromUtc && e.OccurredAtUtc < toUtc)
            .Select(e => e.Amount).AsAsyncEnumerable().WithCancellation(ct)) expenses += amount;
        return new(branchId, fromUtc, toUtc, count, revenue, refunds, revenue - refunds, baseline,
            revenue - refunds - baseline) { ExpensesTotal = expenses, NetProfit = revenue - refunds - baseline - expenses };
    }
}
