using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
using MarketApp.Application.Common.Exceptions;
using MarketApp.Application.DTOs.Sales;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Sales;
using MarketApp.Domain.Enums;

namespace MarketApp.Application.Services;

public sealed class InventoryOperationsService(IUnitOfWork work) : IInventoryOperationsService
{
    private static RequestException Invalid(string message) => new(400, message);
    private static RequestException Missing(string message) => new(404, message);
    private static ConflictException Conflict(string message) => new(message);
    private static void Validate(object value)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), errors, true))
            throw Invalid(string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
    private static void Scale(decimal value, int digits)
    {
        if (decimal.Round(value, digits) != value) throw Invalid($"Use at most {digits} decimal places.");
    }
    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static void CheckReplay(string existing, string requested)
    {
        if (existing != requested) throw Conflict("This client identifier was already used with different data or a different user.");
    }
    public static ReturnDto ToDto(SalesReturn value, IReadOnlyDictionary<Guid, SalesInvoiceLine> originals)
    {
        var lines = value.Lines.Select(l => new ReturnLineDto(l.SalesInvoiceLineId, l.Quantity, l.Restock,
            l.Quantity * originals[l.SalesInvoiceLineId].SellingUnitPrice)).ToList();
        return new(value.Id, value.Number, value.ClientReturnId, value.SalesInvoiceId, value.CreatedByUserId, value.CreatedAtUtc, value.Reason, lines.Sum(l => l.Refund), lines);
    }
    public static AdjustmentDto ToDto(StockAdjustment a) => new(a.Id, a.ClientAdjustmentId, a.ProductId, a.InventoryLocationId,
        a.QuantityChange, a.Reason, a.CreatedByUserId, a.CreatedAtUtc);

    public async Task<ReturnDto> ReturnAsync(Guid branchId, Guid saleId, Guid actorId, CreateReturnDto request, CancellationToken ct = default)
    {
        Validate(request);
        if (request.ClientReturnId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason)) throw Invalid("Return identifier and reason are required.");
        foreach (var line in request.Lines) { if (line is null) throw Invalid("A return line cannot be null."); Validate(line); Scale(line.Quantity, 3); }
        if (request.Lines.Select(l => l.SalesInvoiceLineId).Distinct().Count() != request.Lines.Count) throw Invalid("Duplicate return lines.");
        var hash = Hash(new { branchId, saleId, actorId, request });
        await using var transaction = await work.BeginSerializableAsync(ct);
        var invoice = await work.SalesInvoices.FindAsync(i => i.Id == saleId && i.BranchId == branchId,
            includes: [i => i.Lines], trackChanges: true, cancellationToken: ct) ?? throw Missing("Sale not found.");
        var originals = invoice.Lines.ToDictionary(l => l.Id);
        var returns = work.SalesReturns;
        var previous = await returns.FindAsync(r => r.BranchId == branchId && r.ClientReturnId == request.ClientReturnId,
            includes: [r => r.Lines], cancellationToken: ct);
        if (previous is not null) { CheckReplay(previous.RequestHash, hash); return ToDto(previous, originals); }
        if (request.Lines.Any(l => !l.Restock)) throw Invalid("Damaged goods are not accepted. Every accepted return must restore stock.");
        if (!await work.Branches.AnyAsync(b => b.Id == branchId && b.IsActive, ct)) throw Conflict("Branch is inactive.");
        if (!await work.InventoryLocations.AnyAsync(l => l.Id == invoice.InventoryLocationId && l.BranchId == branchId && l.IsActive, ct)) throw Conflict("Original stock location is inactive.");
        var value = new SalesReturn { ClientReturnId = request.ClientReturnId, SalesInvoiceId = saleId,
            BranchId = branchId, CreatedByUserId = actorId, Reason = request.Reason.Trim(), RequestHash = hash };
        value.Number = $"SR-{value.Id:N}";
        foreach (var line in request.Lines.OrderBy(l => l.SalesInvoiceLineId))
        {
            if (!originals.TryGetValue(line.SalesInvoiceLineId, out var original)) throw Invalid("Line does not belong to this sale.");
            if (line.Quantity > original.Quantity - original.ReturnedQuantity) throw Conflict("Return quantity exceeds the remaining sold quantity.");
            original.ReturnedQuantity += line.Quantity;
            var item = new SalesReturnLine { SalesReturnId = value.Id, SalesInvoiceLineId = original.Id, Quantity = line.Quantity, Restock = line.Restock };
            value.Lines.Add(item);
            if (line.Restock)
            {
                await ChangeStock(original.ProductId, invoice.InventoryLocationId, line.Quantity, ct, original.PurchaseUnitCostSnapshot, true);
                await work.StockMovements.AddAsync(new() { ProductId = original.ProductId, InventoryLocationId = invoice.InventoryLocationId,
                    MovementType = StockMovementType.SalesReturn, QuantityChange = line.Quantity, SalesReturnLineId = item.Id }, ct);
            }
        }
        await returns.AddAsync(value, ct); await work.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return ToDto(value, originals);
    }
    private async Task ChangeStock(Guid productId, Guid locationId, decimal delta, CancellationToken ct, decimal? receiptCost = null, bool isReturn = false)
    {
        var balance = await work.StockBalances.FindAsync(b => b.ProductId == productId && b.InventoryLocationId == locationId,
            trackChanges: true, cancellationToken: ct);
        var quantity = (balance?.Quantity ?? 0) + delta;
        if (quantity < 0 || quantity > 999999999999999.999m) throw Conflict("Stock adjustment would exceed the permitted stock range.");
        if (balance is null)
        {
            balance = new() { ProductId = productId, InventoryLocationId = locationId };
            await work.StockBalances.AddAsync(balance, ct);
        }
        if (delta > 0) await StockCostAccounting.ReceiveAsync(work, balance, delta,
            isReturn ? receiptCost : balance.AveragePurchaseUnitCost, isReturn ? "Sales return" : "Stock adjustment", ct);
        else balance.Quantity = quantity;
    }
    public async Task<AdjustmentDto> AdjustAsync(Guid actorId, CreateAdjustmentDto request, CancellationToken ct = default)
    {
        Validate(request); Scale(request.QuantityChange, 3);
        if (request.ClientAdjustmentId == Guid.Empty || request.QuantityChange == 0 || string.IsNullOrWhiteSpace(request.Reason))
            throw Invalid("Adjustment identifier, nonzero quantity change and reason are required.");
        var hash = Hash(new { actorId, request });
        await using var transaction = await work.BeginSerializableAsync(ct);
        var repo = work.StockAdjustments;
        var previous = await repo.FindAsync(a => a.ClientAdjustmentId == request.ClientAdjustmentId, cancellationToken: ct);
        if (previous is not null) { CheckReplay(previous.RequestHash, hash); return ToDto(previous); }
        if (!await work.Products.AnyAsync(p => p.Id == request.ProductId, ct)) throw Missing("Product not found.");
        var location = await work.InventoryLocations.FindAsync(l => l.Id == request.InventoryLocationId && l.IsActive, cancellationToken: ct)
            ?? throw Conflict("Stock location is missing or inactive.");
        if (location.BranchId is Guid branch && !await work.Branches.AnyAsync(b => b.Id == branch && b.IsActive, ct)) throw Conflict("Branch is inactive.");
        await ChangeStock(request.ProductId, request.InventoryLocationId, request.QuantityChange, ct);
        var value = new StockAdjustment { ClientAdjustmentId = request.ClientAdjustmentId, ProductId = request.ProductId,
            InventoryLocationId = request.InventoryLocationId, QuantityChange = request.QuantityChange,
            Reason = request.Reason.Trim(), CreatedByUserId = actorId, RequestHash = hash };
        await repo.AddAsync(value, ct);
        await work.StockMovements.AddAsync(new() { ProductId = request.ProductId, InventoryLocationId = request.InventoryLocationId,
            MovementType = StockMovementType.Adjustment, QuantityChange = request.QuantityChange, StockAdjustmentId = value.Id }, ct);
        await work.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return ToDto(value);
    }
}
