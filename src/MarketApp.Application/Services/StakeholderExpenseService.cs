using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
using MarketApp.Application.Common.Exceptions;
using MarketApp.Application.DTOs.Accounting;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Accounting;
namespace MarketApp.Application.Services;

public sealed class StakeholderExpenseService(IUnitOfWork work) : IStakeholderExpenseService
{
    private static void Validate(object value)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), errors, true))
            throw new RequestException(400, string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static StakeholderExpenseDto ToDto(StakeholderExpense e) => new(e.Id, e.ClientExpenseId, e.BranchId,
        e.Branch?.Name, e.Category, e.Amount, e.OccurredAtUtc, e.Description, e.PaidTo, e.ReferenceNumber,
        e.CreatedByUserId, e.CreatedAtUtc, e.VoidedByUserId, e.VoidedAtUtc, e.VoidReason);

    public async Task<StakeholderExpenseDto> CreateAsync(Guid actorId, CreateStakeholderExpenseDto request, CancellationToken ct)
    {
        Validate(request);
        if (actorId == Guid.Empty || request.ClientExpenseId == Guid.Empty || request.BranchId == Guid.Empty)
            throw new RequestException(400, "A client expense identifier and authenticated stakeholder are required. Omit BranchId for a general expense.");
        if (decimal.Round(request.Amount, 4) != request.Amount)
            throw new RequestException(400, "Use at most four decimal places for the amount.");
        if (request.Description.Trim().Length < 3)
            throw new RequestException(400, "Provide a meaningful expense description.");
        if (request.OccurredAtUtc.Kind != DateTimeKind.Utc ||
            request.OccurredAtUtc < new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc) ||
            request.OccurredAtUtc > DateTime.UtcNow.AddMinutes(5))
            throw new RequestException(400, "Use a UTC expense date (Z), from year 2000 through now.");
        var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { actorId, request })));
        await using var transaction = await work.BeginSerializableAsync(ct);
        var previous = await work.StakeholderExpenses.FindAsync(e => e.ClientExpenseId == request.ClientExpenseId,
            includes: [e => e.Branch!], cancellationToken: ct);
        if (previous is not null)
        {
            if (previous.RequestHash != hash) throw new ConflictException("This client identifier was already used with different data or a different user.");
            return ToDto(previous);
        }
        // Inactive branches may have historical rent to record. This reference is descriptive only.
        var branch = request.BranchId is Guid branchId
            ? await work.Branches.FindAsync(b => b.Id == branchId, cancellationToken: ct)
                ?? throw new RequestException(404, "Branch not found.")
            : null;
        var expense = new StakeholderExpense {
            ClientExpenseId = request.ClientExpenseId, RequestHash = hash, BranchId = request.BranchId,
            Category = request.Category, Amount = request.Amount, OccurredAtUtc = request.OccurredAtUtc,
            Description = request.Description.Trim(), PaidTo = Optional(request.PaidTo), ReferenceNumber = Optional(request.ReferenceNumber),
            CreatedByUserId = actorId };
        await work.StakeholderExpenses.AddAsync(expense, ct);
        await work.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return ToDto(expense) with { BranchName = branch?.Name };
    }
    public async Task<StakeholderExpenseDto> VoidAsync(Guid expenseId, Guid actorId, VoidStakeholderExpenseDto request, CancellationToken ct)
    {
        Validate(request);
        if (actorId == Guid.Empty || request.Reason.Trim().Length < 3)
            throw new RequestException(400, "An authenticated stakeholder and meaningful void reason are required.");
        await using var transaction = await work.BeginSerializableAsync(ct);
        var expense = await work.StakeholderExpenses.FindAsync(e => e.Id == expenseId, includes: [e => e.Branch!],
            trackChanges: true, cancellationToken: ct) ?? throw new RequestException(404, "Stakeholder expense not found.");
        if (expense.VoidedAtUtc is not null)
        {
            if (expense.VoidedByUserId != actorId || expense.VoidReason != request.Reason.Trim())
                throw new ConflictException("Expense was already voided with different details.");
            return ToDto(expense);
        }
        expense.VoidedAtUtc = DateTime.UtcNow; expense.VoidedByUserId = actorId; expense.VoidReason = request.Reason.Trim();
        await work.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return ToDto(expense);
    }
}
