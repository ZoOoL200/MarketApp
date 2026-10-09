using System.ComponentModel.DataAnnotations;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Purchases;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Entity.Purchasing;
using MarketApp.Domain.Enums;

namespace MarketApp.Application.Services;

public class PurchaseInvoiceService : IPurchaseInvoiceService
{
    private const decimal MaximumStockQuantity =
        999999999999999.999m;

    private readonly IUnitOfWork _unitOfWork;

    public PurchaseInvoiceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MarketApp.Application.Common.PagedResult<PurchaseInvoiceDto>> GetPageAsync(MarketApp.Application.DTOs.Sales.PageQuery query, CancellationToken ct = default)
    {
        var page = await _unitOfWork.PurchaseInvoices.GetPageAsync(query.PageNumber, query.PageSize,
            q => q.OrderByDescending(i => i.CreatedAtUtc).ThenBy(i => i.Id), includes: [i => i.Lines], cancellationToken: ct);
        return new(page.Items.Select(ToDto).ToList(), page.TotalCount, page.PageNumber, page.PageSize);
    }

    public async Task<PurchaseInvoiceResult> CreateAsync(
        CreatePurchaseInvoiceDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request);

        if (validationError is not null)
        {
            return Failure(
                PurchaseInvoiceResultStatus.InvalidRequest,
                validationError);
        }

        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();

        var invoice = new PurchaseInvoice
        {
            Id = id,
            Number = $"PI-{id:N}",
            SupplierId = request.SupplierId,
            InventoryLocationId = request.InventoryLocationId,
            InvoiceDate = request.InvoiceDate,
            SupplierInvoiceNumber =
                NormalizeOptional(request.SupplierInvoiceNumber),
            Notes = NormalizeOptional(request.Notes),
            Status = PurchaseInvoiceStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        for (var index = 0; index < request.Lines.Count; index++)
        {
            var line = request.Lines[index];

            invoice.Lines.Add(new PurchaseInvoiceLine
            {
                PurchaseInvoiceId = invoice.Id,
                LineNumber = index + 1,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost
            });
        }

        var referenceError = await ValidateReferencesAsync(
            invoice,
            cancellationToken);

        if (referenceError is not null)
        {
            return referenceError;
        }

        await _unitOfWork.PurchaseInvoices.AddAsync(
            invoice,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new(
            PurchaseInvoiceResultStatus.Success,
            ToDto(invoice));
    }

    public async Task<PurchaseInvoiceDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _unitOfWork.PurchaseInvoices.FindAsync(
            predicate: i => i.Id == id,
            includes: [i => i.Lines],
            cancellationToken: cancellationToken);

        return invoice is null ? null : ToDto(invoice);
    }

    public async Task<PurchaseInvoiceResult> PostAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _unitOfWork.PurchaseInvoices.FindAsync(
            predicate: i => i.Id == id,
            includes: [i => i.Lines],
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (invoice is null)
        {
            return Failure(
                PurchaseInvoiceResultStatus.NotFound,
                "Purchase invoice was not found.");
        }

        // A repeated request must not receive the same goods again.
        if (invoice.Status == PurchaseInvoiceStatus.Posted)
        {
            return new(
                PurchaseInvoiceResultStatus.Success,
                ToDto(invoice));
        }

        if (invoice.Status != PurchaseInvoiceStatus.Draft)
        {
            return Failure(
                PurchaseInvoiceResultStatus.Conflict,
                "Only draft purchase invoices can be posted.");
        }

        if (invoice.Lines.Count == 0)
        {
            return Failure(
                PurchaseInvoiceResultStatus.InvalidRequest,
                "The invoice must contain at least one line.");
        }

        // References may have been deactivated since draft creation.
        var referenceError = await ValidateReferencesAsync(
            invoice,
            cancellationToken);

        if (referenceError is not null)
        {
            return referenceError;
        }

        // Group repeated products so each balance is updated once.
        foreach (var group in invoice.Lines.GroupBy(l => l.ProductId))
        {
            var productId = group.Key;
            var receivedQuantity = group.Sum(l => l.Quantity);

            var balance = await _unitOfWork.StockBalances.FindAsync(
                predicate: s =>
                    s.ProductId == productId &&
                    s.InventoryLocationId == invoice.InventoryLocationId,
                trackChanges: true,
                cancellationToken: cancellationToken);

            var currentQuantity = balance?.Quantity ?? 0m;

            if (receivedQuantity > MaximumStockQuantity - currentQuantity)
            {
                return Failure(
                    PurchaseInvoiceResultStatus.Conflict,
                    $"Receiving product '{productId}' would exceed " +
                    "the supported stock quantity.");
            }

            if (balance is null)
            {
                balance = new StockBalance
                {
                    ProductId = productId,
                    InventoryLocationId = invoice.InventoryLocationId,
                    Quantity = 0
                };

                await _unitOfWork.StockBalances.AddAsync(
                    balance,
                    cancellationToken);
            }
            await StockCostAccounting.ReceiveAsync(_unitOfWork, balance, receivedQuantity,
                group.Sum(l => l.Quantity * l.UnitCost) / receivedQuantity,
                $"Purchase receipt {invoice.Number}", cancellationToken);
        }

        var now = DateTime.UtcNow;

        foreach (var line in invoice.Lines)
        {
            var movement = new StockMovement
            {
                ProductId = line.ProductId,
                InventoryLocationId = invoice.InventoryLocationId,
                MovementType = StockMovementType.PurchaseReceipt,
                QuantityChange = line.Quantity,
                PurchaseInvoiceLineId = line.Id,
                OccurredAtUtc = now
            };

            await _unitOfWork.StockMovements.AddAsync(
                movement,
                cancellationToken);
        }

        invoice.Status = PurchaseInvoiceStatus.Posted;
        invoice.PostedAtUtc = now;
        invoice.UpdatedAtUtc = now;

        // Save the invoice, balances, and movements together.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new(
            PurchaseInvoiceResultStatus.Success,
            ToDto(invoice));
    }

    private async Task<PurchaseInvoiceResult?> ValidateReferencesAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken)
    {
        var supplier = await _unitOfWork.Suppliers.FindAsync(
            predicate: s => s.Id == invoice.SupplierId,
            cancellationToken: cancellationToken);

        if (supplier is null)
        {
            return Failure(
                PurchaseInvoiceResultStatus.InvalidRequest,
                "Supplier does not exist.");
        }

        if (!supplier.IsActive)
        {
            return Failure(
                PurchaseInvoiceResultStatus.Conflict,
                "Supplier is inactive.");
        }

        var location = await _unitOfWork.InventoryLocations.FindAsync(
            predicate: l => l.Id == invoice.InventoryLocationId,
            includes: [l => l.Branch!],
            cancellationToken: cancellationToken);

        if (location is null)
        {
            return Failure(
                PurchaseInvoiceResultStatus.InvalidRequest,
                "Inventory location does not exist.");
        }

        if (!location.IsActive)
        {
            return Failure(
                PurchaseInvoiceResultStatus.Conflict,
                "Inventory location is inactive.");
        }

        if (location.BranchId.HasValue &&
            location.Branch?.IsActive != true)
        {
            return Failure(
                PurchaseInvoiceResultStatus.Conflict,
                "The location's branch is inactive.");
        }

        foreach (var productId in invoice.Lines
                     .Select(l => l.ProductId)
                     .Distinct())
        {
            var product = await _unitOfWork.Products.FindAsync(
                predicate: p => p.Id == productId,
                cancellationToken: cancellationToken);

            if (product is null)
            {
                return Failure(
                    PurchaseInvoiceResultStatus.InvalidRequest,
                    $"Product '{productId}' does not exist.");
            }

            if (!product.IsActive)
            {
                return Failure(
                    PurchaseInvoiceResultStatus.Conflict,
                    $"Product '{productId}' is inactive.");
            }
        }

        return null;
    }

    private static string? ValidateRequest(
        CreatePurchaseInvoiceDto request)
    {
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                results,
                validateAllProperties: true))
        {
            return results[0].ErrorMessage ?? "Invalid invoice.";
        }

        // TryValidateObject does not recursively validate child objects.
        foreach (var line in request.Lines)
        {
            results.Clear();

            if (!Validator.TryValidateObject(
                    line,
                    new ValidationContext(line),
                    results,
                    validateAllProperties: true))
            {
                return results[0].ErrorMessage ?? "Invalid invoice line.";
            }
        }

        return null;
    }

    private static PurchaseInvoiceDto ToDto(PurchaseInvoice invoice)
    {
        var lines = invoice.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new PurchaseInvoiceLineDto(
                l.Id,
                l.LineNumber,
                l.ProductId,
                l.Quantity,
                l.UnitCost,
                l.Quantity * l.UnitCost))
            .ToList();

        return new PurchaseInvoiceDto(
            invoice.Id,
            invoice.Number,
            invoice.SupplierInvoiceNumber,
            invoice.SupplierId,
            invoice.InventoryLocationId,
            invoice.InvoiceDate,
            invoice.Status.ToString(),
            invoice.Notes,
            invoice.CreatedAtUtc,
            invoice.PostedAtUtc,
            lines.Sum(l => l.LineAmount),
            lines);
    }

    private static PurchaseInvoiceResult Failure(
        PurchaseInvoiceResultStatus status,
        string error)
    {
        return new(status, Error: error);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}