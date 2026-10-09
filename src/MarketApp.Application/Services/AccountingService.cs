using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
using MarketApp.Application.Common.Exceptions;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Entity.Sales;
namespace MarketApp.Application.Services;
public sealed class AccountingService(IUnitOfWork work, ISalesQueries sales) : IAccountingService
{
    private static void Validate(object request)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            throw new RequestException(400, string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
    private static void Scale(decimal value, int digits)
    {
        if (decimal.Round(value, digits) != value) throw new RequestException(400, $"Use at most {digits} decimal places.");
    }
    private static void Reason(string value)
    {
        if (value.Trim().Length < 3) throw new RequestException(400, "Provide a meaningful reason or description.");
    }
    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static void Replay(string? actual, string expected)
    {
        if (actual != expected) throw new ConflictException("This identifier was already used with different data or a different user.");
    }
    public static ExpenseDto ToDto(BranchExpense e) => new(e.Id, e.BranchId, e.ClientExpenseId, e.Category, e.EmployeeUserId,
        e.Amount, e.OccurredAtUtc, e.Description, e.CreatedByUserId, e.CreatedAtUtc, e.VoidedByUserId, e.VoidedAtUtc, e.VoidReason);
    public async Task<StockCostDto> InitializeCostAsync(Guid actorId, InitializeStockCostDto request, CancellationToken ct)
    {
        Validate(request); Scale(request.PurchaseUnitCost!.Value, 6); Scale(request.ExpectedQuantity, 3); Reason(request.Reason);
        if (request.ClientChangeId == Guid.Empty || request.ProductId == Guid.Empty || request.InventoryLocationId == Guid.Empty)
            throw new RequestException(400, "ClientChangeId, ProductId and InventoryLocationId are required.");
        var hash = Hash(new { actorId, request });
        await using var transaction = await work.BeginSerializableAsync(ct);
        var previous = await work.StockCostHistories.FindAsync(h => h.ClientChangeId == request.ClientChangeId, cancellationToken: ct);
        if (previous is not null)
        {
            Replay(previous.RequestHash, hash);
            return new(previous.ProductId, previous.InventoryLocationId, previous.QuantityAtChange, previous.AveragePurchaseUnitCost);
        }
        var balance = await work.StockBalances.FindAsync(b => b.ProductId == request.ProductId && b.InventoryLocationId == request.InventoryLocationId,
            trackChanges: true, cancellationToken: ct) ?? throw new RequestException(404, "Stock balance not found.");
        if (balance.AveragePurchaseUnitCost is not null) throw new ConflictException("Purchase cost is already known; it cannot be overwritten by initialization.");
        if (balance.Quantity != request.ExpectedQuantity) throw new ConflictException("Stock quantity changed. Verify the current stock before initializing its cost.");
        balance.AveragePurchaseUnitCost = request.PurchaseUnitCost;
        await work.StockCostHistories.AddAsync(new StockCostHistory { ProductId = balance.ProductId, InventoryLocationId = balance.InventoryLocationId,
            AveragePurchaseUnitCost = balance.AveragePurchaseUnitCost, QuantityAtChange = balance.Quantity, Reason = request.Reason.Trim(),
            RecordedByUserId = actorId, ClientChangeId = request.ClientChangeId, RequestHash = hash }, ct);
        await work.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return new(balance.ProductId, balance.InventoryLocationId, balance.Quantity, balance.AveragePurchaseUnitCost);
    }
    public async Task RecordHistoricalCostAsync(Guid branchId, Guid saleId, Guid lineId, Guid actorId, RecordHistoricalCostDto request, CancellationToken ct)
    {
        Validate(request); Scale(request.PurchaseUnitCost!.Value, 6); Reason(request.Reason);
        var hash = Hash(new { branchId, saleId, lineId, actorId, request });
        await using var transaction = await work.BeginSerializableAsync(ct);
        var invoice = await work.SalesInvoices.FindAsync(s => s.Id == saleId && s.BranchId == branchId,
            includes: [s => s.Lines], trackChanges: true, cancellationToken: ct) ?? throw new RequestException(404, "Sale not found.");
        var line = invoice.Lines.SingleOrDefault(l => l.Id == lineId) ?? throw new RequestException(404, "Sale line not found.");
        if (line.PurchaseCostRecordHash == hash) return;
        if (line.PurchaseUnitCostSnapshot is not null) throw new ConflictException("This sale already has a fixed purchase cost; it cannot be repriced.");
        line.PurchaseUnitCostSnapshot = request.PurchaseUnitCost;
        line.PurchaseCostRecordedByUserId = actorId; line.PurchaseCostRecordedAtUtc = DateTime.UtcNow;
        line.PurchaseCostRecordHash = hash; line.PurchaseCostReason = request.Reason.Trim();
        await work.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    public async Task<ExpenseDto> CreateExpenseAsync(Guid branchId, Guid actorId, CreateExpenseDto request, CancellationToken ct)
    {
        Validate(request); Scale(request.Amount, 4); Reason(request.Description);
        if (request.ClientExpenseId == Guid.Empty || request.OccurredAtUtc.Kind != DateTimeKind.Utc ||
            request.OccurredAtUtc < new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc) || request.OccurredAtUtc > DateTime.UtcNow.AddMinutes(5))
            throw new RequestException(400, "Provide ClientExpenseId and a UTC expense date (Z), from year 2000 through now.");
        var hash = Hash(new { branchId, actorId, request });
        await using var transaction = await work.BeginSerializableAsync(ct);
        var previous = await work.BranchExpenses.FindAsync(e => e.BranchId == branchId && e.ClientExpenseId == request.ClientExpenseId, cancellationToken: ct);
        if (previous is not null) { Replay(previous.RequestHash, hash); return ToDto(previous); }
        if (request.Category == "Rent") throw new RequestException(400, "Record rent in /api/stakeholder-expenses. Rent does not belong to branch profit expenses.");
        if (!await work.Branches.AnyAsync(b => b.Id == branchId && b.IsActive, ct)) throw new ConflictException("Branch is missing or inactive.");
        if (request.EmployeeUserId is Guid employee && !await sales.IsAssignedUserAsync(branchId, employee, AppRoles.Seller, ct))
            throw new RequestException(400, "Employee must be an active seller assigned to this branch. For a historical or grouped expense, omit EmployeeUserId and describe it.");
        var expense = new BranchExpense { BranchId = branchId, ClientExpenseId = request.ClientExpenseId, RequestHash = hash,
            Category = request.Category, EmployeeUserId = request.EmployeeUserId, Amount = request.Amount,
            OccurredAtUtc = request.OccurredAtUtc, Description = request.Description.Trim(), CreatedByUserId = actorId };
        await work.BranchExpenses.AddAsync(expense, ct); await work.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return ToDto(expense);
    }
    public async Task<ExpenseDto> VoidExpenseAsync(Guid branchId, Guid expenseId, Guid actorId, VoidExpenseDto request, CancellationToken ct)
    {
        Validate(request); Reason(request.Reason);
        await using var transaction = await work.BeginSerializableAsync(ct);
        var expense = await work.BranchExpenses.FindAsync(e => e.Id == expenseId && e.BranchId == branchId,
            trackChanges: true, cancellationToken: ct) ?? throw new RequestException(404, "Expense not found.");
        if (expense.VoidedAtUtc is not null)
        {
            if (expense.VoidedByUserId != actorId || expense.VoidReason != request.Reason.Trim()) throw new ConflictException("Expense was already voided with different details.");
            return ToDto(expense);
        }
        expense.VoidedByUserId = actorId; expense.VoidedAtUtc = DateTime.UtcNow; expense.VoidReason = request.Reason.Trim();
        await work.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return ToDto(expense);
    }
}
