using System.ComponentModel.DataAnnotations;
using MarketApp.Application.Common.Exceptions;
using MarketApp.Application.DTOs.Accounting;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Services;
using MarketApp.Domain.Entity.Accounting;
using MarketApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace MarketApp.Infrastructure.Services;

public sealed class StakeholderExpenseQueries(AppDbContext db) : IStakeholderExpenseQueries
{
    public async Task<StakeholderExpenseDto> GetAsync(Guid expenseId, CancellationToken ct)
    {
        var expense = await db.StakeholderExpenses.AsNoTracking().Include(e => e.Branch)
            .SingleOrDefaultAsync(e => e.Id == expenseId, ct) ?? throw new RequestException(404, "Stakeholder expense not found.");
        return StakeholderExpenseService.ToDto(expense);
    }
    public async Task<StakeholderExpensePageDto> ListAsync(StakeholderExpenseQuery query, CancellationToken ct)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(query, new ValidationContext(query), errors, true))
            throw new RequestException(400, string.Join(" ", errors.Select(e => e.ErrorMessage)));
        if (query.FromUtc is DateTime from && from.Kind != DateTimeKind.Utc ||
            query.ToUtc is DateTime to && to.Kind != DateTimeKind.Utc ||
            query.FromUtc.HasValue && query.ToUtc.HasValue && query.FromUtc >= query.ToUtc)
            throw new RequestException(400, "Use UTC dates (Z), with FromUtc before ToUtc; the end date is exclusive.");
        if (query.BranchId == Guid.Empty) throw new RequestException(400, "BranchId must be a valid identifier or omitted.");
        await using var snapshot = await db.Database.BeginTransactionAsync(db.Database.IsNpgsql()
            ? System.Data.IsolationLevel.RepeatableRead : System.Data.IsolationLevel.Serializable, ct);
        if (query.BranchId is Guid branch && !await db.Branches.AnyAsync(b => b.Id == branch, ct))
            throw new RequestException(404, "Branch not found.");
        IQueryable<StakeholderExpense> q = db.StakeholderExpenses.AsNoTracking();
        if (query.BranchId.HasValue) q = q.Where(e => e.BranchId == query.BranchId);
        if (!string.IsNullOrEmpty(query.Category)) q = q.Where(e => e.Category == query.Category);
        if (query.FromUtc.HasValue) q = q.Where(e => e.OccurredAtUtc >= query.FromUtc);
        if (query.ToUtc.HasValue) q = q.Where(e => e.OccurredAtUtc < query.ToUtc);
        if (!query.IncludeVoided) q = q.Where(e => e.VoidedAtUtc == null);
        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search)) q = q.Where(e => e.Description.Contains(search) ||
            (e.PaidTo != null && e.PaidTo.Contains(search)) || (e.ReferenceNumber != null && e.ReferenceNumber.Contains(search)));
        var count = await q.CountAsync(ct);
        decimal activeAmount = 0;
        // Total covers the whole filtered register, not only the displayed page. Voids never contribute.
        await foreach (var amount in q.Where(e => e.VoidedAtUtc == null).Select(e => e.Amount)
            .AsAsyncEnumerable().WithCancellation(ct)) activeAmount += amount;
        var items = await q.OrderByDescending(e => e.OccurredAtUtc).ThenBy(e => e.Id)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).Include(e => e.Branch).ToListAsync(ct);
        return new(items.Select(StakeholderExpenseService.ToDto).ToList(), count, query.PageNumber, query.PageSize, activeAmount);
    }
}
