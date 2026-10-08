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

public sealed class SalesService(IUnitOfWork work) : ISalesService
{
    private static RequestException Invalid(string message) => new(400, message);
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
    public static SaleDto ToDto(SalesInvoice invoice) => new(invoice.Id, invoice.Number, invoice.ClientSaleId,
        invoice.BranchId, invoice.InventoryLocationId, invoice.SoldByUserId, invoice.SoldAtUtc, invoice.CreatedAtUtc, invoice.Notes,
        invoice.Lines.Sum(l => l.Quantity * l.SellingUnitPrice), invoice.Lines.OrderBy(l => l.LineNumber)
            .Select(l => new SaleLineDto(l.Id, l.ProductId, l.ProductNameSnapshot, l.ProductSkuSnapshot,
                l.Quantity, l.SellingUnitPrice, l.Quantity * l.SellingUnitPrice)).ToList());
    public async Task<PostedSaleDto> SellAsync(Guid branchId, Guid actorId, CreateSaleDto request,
        CancellationToken ct = default)
    {
        Validate(request);
        if (request.ClientSaleId == Guid.Empty || request.InventoryLocationId == Guid.Empty || actorId == Guid.Empty)
            throw Invalid("ClientSaleId, InventoryLocationId and authenticated seller are required.");
        foreach (var line in request.Lines)
        {
            if (line is null) throw Invalid("A sale line cannot be null.");
            Validate(line); Scale(line.Quantity, 3); Scale(line.SellingUnitPrice, 4);
            if (line.ProductId == Guid.Empty) throw Invalid("Every line needs a product.");
        }
        if (request.Lines.Select(l => l.ProductId).Distinct().Count() != request.Lines.Count)
            throw Invalid("Combine repeated products into one line.");
        var hash = Hash(new { branchId, actorId, request });
        await using var transaction = await work.BeginSerializableAsync(ct);
        var invoices = work.SalesInvoices;
        var previous = await invoices.FindAsync(i => i.BranchId == branchId && i.ClientSaleId == request.ClientSaleId,
            includes: [i => i.Lines], cancellationToken: ct);
        if (previous is not null) { CheckReplay(previous.RequestHash, hash); return new(true, ToDto(previous)); }
        var now = DateTime.UtcNow;
        if (!await work.Branches.AnyAsync(b => b.Id == branchId && b.IsActive, ct)) throw Conflict("Branch is missing or inactive.");
        if (!await work.InventoryLocations.AnyAsync(l => l.Id == request.InventoryLocationId && l.BranchId == branchId && l.IsActive, ct))
            throw Conflict("Choose an active inventory location belonging to this branch.");
        var invoice = new SalesInvoice
        {
            ClientSaleId = request.ClientSaleId, BranchId = branchId, InventoryLocationId = request.InventoryLocationId,
            SoldByUserId = actorId, RequestHash = hash, SoldAtUtc = now,
            CreatedAtUtc = now, Notes = request.Notes?.Trim()
        };
        invoice.Number = $"SI-{invoice.Id:N}";
        foreach (var line in request.Lines.OrderBy(l => l.ProductId))
        {
            var product = await work.Products.FindAsync(p => p.Id == line.ProductId && p.IsActive, cancellationToken: ct)
                ?? throw Conflict("A product is missing or inactive.");
            var price = await work.BranchProductPrices.FindAsync(p => p.BranchId == branchId && p.ProductId == line.ProductId,
                cancellationToken: ct) ?? throw Conflict("Product pricing is not configured.");
            if (price.Revision != line.PriceRevision)
                throw Conflict("Pricing changed. Refresh the catalog and review the selling price before submitting a new sale.");
            var baseline = price.BaselineUnitPrice ?? throw Conflict("Baseline pricing is not configured.");
            var minimum = price.MinimumSellingPrice ?? throw Conflict("Minimum selling price is not configured.");
            if (line.SellingUnitPrice < minimum) throw Conflict("Selling price cannot be below the minimum for this revision.");
            var balance = await work.StockBalances.FindAsync(b => b.ProductId == product.Id && b.InventoryLocationId == invoice.InventoryLocationId,
                trackChanges: true, cancellationToken: ct);
            if (balance is null || balance.Quantity < line.Quantity) throw Conflict("Insufficient stock. The sale was not posted.");
            balance.Quantity -= line.Quantity;
            var item = new SalesInvoiceLine
            {
                SalesInvoiceId = invoice.Id, LineNumber = request.Lines.IndexOf(line) + 1, ProductId = product.Id,
                ProductNameSnapshot = product.Name, ProductSkuSnapshot = product.Sku, Quantity = line.Quantity,
                BaselineUnitPriceSnapshot = baseline, MinimumSellingPriceSnapshot = minimum,
                SellingUnitPrice = line.SellingUnitPrice, PriceRevisionSnapshot = line.PriceRevision
            };
            invoice.Lines.Add(item);
            await work.StockMovements.AddAsync(new() { ProductId = product.Id, InventoryLocationId = invoice.InventoryLocationId,
                MovementType = StockMovementType.Sale, QuantityChange = -line.Quantity, SalesInvoiceLineId = item.Id, OccurredAtUtc = now }, ct);
        }
        await invoices.AddAsync(invoice, ct);
        await work.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(false, ToDto(invoice));
    }

}
