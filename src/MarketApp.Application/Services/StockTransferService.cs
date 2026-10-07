using System.ComponentModel.DataAnnotations;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.StockTransfers;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Inventory;
using MarketApp.Domain.Entity.Pricing;
using MarketApp.Domain.Enums;

namespace MarketApp.Application.Services;

public class StockTransferService : IStockTransferService
{
    private const decimal MaximumStockQuantity =
        999999999999999.999m;

    private readonly IUnitOfWork _unitOfWork;

    public StockTransferService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<StockTransferResult> CreateAsync(
        CreateStockTransferDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request);

        if (validationError is not null)
        {
            return Failure(
                StockTransferResultStatus.InvalidRequest,
                validationError);
        }

        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();

        var transfer = new StockTransfer
        {
            Id = id,
            Number = $"TR-{id:N}",
            SourceLocationId = request.SourceLocationId,
            DestinationLocationId = request.DestinationLocationId,
            Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? null
                : request.Notes.Trim(),
            Status = StockTransferStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        for (var index = 0; index < request.Lines.Count; index++)
        {
            var line = request.Lines[index];

            transfer.Lines.Add(new StockTransferLine
            {
                StockTransferId = transfer.Id,
                LineNumber = index + 1,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                BaselineUnitPrice = line.BaselineUnitPrice
            });
        }

        var referenceError = await ValidateDispatchReferencesAsync(
            transfer,
            cancellationToken);

        if (referenceError is not null)
        {
            return referenceError;
        }

        var destination = await _unitOfWork.InventoryLocations.FindAsync(
            predicate: l => l.Id == transfer.DestinationLocationId,
            cancellationToken: cancellationToken);

        var branchId = destination!.BranchId!.Value;
        var productIds = transfer.Lines.Select(l => l.ProductId).Distinct().ToArray();
        var prices = await _unitOfWork.BranchProductPrices.GetAllAsync(
            predicate: p => p.BranchId == branchId && productIds.Contains(p.ProductId),
            cancellationToken: cancellationToken);
        var baselineRevisions = prices.ToDictionary(p => p.ProductId, p => p.BaselineRevision);

        foreach (var line in transfer.Lines)
        {
            line.BaselineRevisionAtCreation = baselineRevisions.GetValueOrDefault(line.ProductId);
        }

        await _unitOfWork.StockTransfers.AddAsync(
            transfer,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Success(transfer);
    }

    public async Task<StockTransferDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _unitOfWork.StockTransfers.FindAsync(
            predicate: t => t.Id == id,
            includes: [t => t.Lines],
            cancellationToken: cancellationToken);

        return transfer is null ? null : ToDto(transfer);
    }

    public Task<StockTransferResult> ShipAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return ExecuteStockActionAsync(
            id,
            receiving: false,
            actorUserId: null,
            cancellationToken: cancellationToken);
    }

    public Task<StockTransferResult> ReceiveAsync(
        Guid id,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
        {
            return Task.FromResult(Failure(
                StockTransferResultStatus.InvalidRequest,
                "An authenticated user ID is required to receive a transfer."));
        }

        return ExecuteStockActionAsync(
            id,
            receiving: true,
            actorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    private async Task<StockTransferResult> ExecuteStockActionAsync(
        Guid id,
        bool receiving,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var transfer = await _unitOfWork.StockTransfers.FindAsync(
            predicate: t => t.Id == id,
            includes: [t => t.Lines],
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (transfer is null)
        {
            return Failure(
                StockTransferResultStatus.NotFound,
                "Stock transfer was not found.");
        }

        // Repeated successful actions do not change stock again.
        if (receiving &&
            transfer.Status == StockTransferStatus.Received)
        {
            return Success(transfer);
        }

        if (!receiving &&
            (transfer.Status == StockTransferStatus.InTransit ||
             transfer.Status == StockTransferStatus.Received))
        {
            return Success(transfer);
        }

        var expectedStatus = receiving
            ? StockTransferStatus.InTransit
            : StockTransferStatus.Draft;

        if (transfer.Status != expectedStatus)
        {
            return Failure(
                StockTransferResultStatus.Conflict,
                receiving
                    ? "Only an in-transit transfer can be received."
                    : "Only a draft transfer can be shipped.");
        }

        if (transfer.Lines.Count == 0)
        {
            return Failure(
                StockTransferResultStatus.InvalidRequest,
                "The transfer must contain at least one line.");
        }

        // Validate activity before goods leave the source.
        if (!receiving)
        {
            var referenceError = await ValidateDispatchReferencesAsync(
                transfer,
                cancellationToken);

            if (referenceError is not null)
            {
                return referenceError;
            }
        }

        var locationId = receiving
            ? transfer.DestinationLocationId
            : transfer.SourceLocationId;

        // First validate every balance without changing quantities.
        var changes = new List<(
            StockBalance Balance,
            decimal Quantity,
            bool IsNew)>();

        foreach (var group in transfer.Lines.GroupBy(l => l.ProductId))
        {
            var productId = group.Key;
            var quantity = group.Sum(l => l.Quantity);

            var balance = await _unitOfWork.StockBalances.FindAsync(
                predicate: s =>
                    s.ProductId == productId &&
                    s.InventoryLocationId == locationId,
                trackChanges: true,
                cancellationToken: cancellationToken);

            if (!receiving &&
                (balance is null || balance.Quantity < quantity))
            {
                return Failure(
                    StockTransferResultStatus.Conflict,
                    $"Insufficient source stock for product '{productId}'.");
            }

            var isNew = balance is null;

            balance ??= new StockBalance
            {
                ProductId = productId,
                InventoryLocationId = locationId,
                Quantity = 0
            };

            if (receiving &&
                quantity > MaximumStockQuantity - balance.Quantity)
            {
                return Failure(
                    StockTransferResultStatus.Conflict,
                    $"Receiving product '{productId}' would exceed " +
                    "the supported stock quantity.");
            }

            changes.Add((balance, quantity, isNew));
        }

        // Prepare every price change before mutating any tracked entity.
        var priceChanges = new List<ReceiptPriceChange>();
        if (receiving)
        {
            var prepared = await PrepareReceiptPricesAsync(transfer, cancellationToken);
            if (prepared.Error is not null)
            {
                return prepared.Error;
            }

            priceChanges = prepared.Changes;
        }

        foreach (var change in changes)
        {
            change.Balance.Quantity += receiving
                ? change.Quantity
                : -change.Quantity;

            if (change.IsNew)
            {
                await _unitOfWork.StockBalances.AddAsync(
                    change.Balance,
                    cancellationToken);
            }
        }

        var now = DateTime.UtcNow;

        // Preserve timestamp ordering if the system clock moved backwards.
        if (receiving &&
            transfer.ShippedAtUtc is DateTime shippedAt &&
            now < shippedAt)
        {
            now = shippedAt;
        }

        foreach (var change in priceChanges)
        {
            if (now < change.Price.UpdatedAtUtc)
            {
                now = change.Price.UpdatedAtUtc;
            }
        }

        foreach (var change in priceChanges)
        {
            var price = change.Price;
            var oldBaseline = price.BaselineUnitPrice;
            price.BaselineUnitPrice = change.Line.BaselineUnitPrice;
            price.Revision++;
            price.BaselineRevision = price.Revision;
            price.UpdatedAtUtc = now;

            if (change.IsNew)
            {
                await _unitOfWork.BranchProductPrices.AddAsync(price, cancellationToken);
            }

            await _unitOfWork.BranchProductPriceHistories.AddAsync(
                new BranchProductPriceHistory
                {
                    BranchProductPriceId = price.Id,
                    BranchProductPrice = price,
                    Revision = price.Revision,
                    OldBaselineUnitPrice = oldBaseline,
                    NewBaselineUnitPrice = price.BaselineUnitPrice,
                    OldMinimumSellingPrice = price.MinimumSellingPrice,
                    NewMinimumSellingPrice = price.MinimumSellingPrice,
                    ChangeType = BranchPriceChangeType.TransferReceipt,
                    Reason = $"Received transfer {transfer.Number}.",
                    ChangedAtUtc = now,
                    ChangedByUserId = actorUserId,
                    StockTransferLineId = change.Line.Id
                },
                cancellationToken);
        }

        foreach (var line in transfer.Lines)
        {
            await _unitOfWork.StockMovements.AddAsync(
                new StockMovement
                {
                    ProductId = line.ProductId,
                    InventoryLocationId = locationId,
                    MovementType = receiving
                        ? StockMovementType.TransferIn
                        : StockMovementType.TransferOut,
                    QuantityChange = receiving
                        ? line.Quantity
                        : -line.Quantity,
                    StockTransferLineId = line.Id,
                    OccurredAtUtc = now
                },
                cancellationToken);
        }

        transfer.UpdatedAtUtc = now;

        if (receiving)
        {
            transfer.Status = StockTransferStatus.Received;
            transfer.ReceivedAtUtc = now;
        }
        else
        {
            transfer.Status = StockTransferStatus.InTransit;
            transfer.ShippedAtUtc = now;
        }

        // Status, movements, balances, pricing, and history share one transaction.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Success(transfer);
    }

    private sealed record ReceiptPriceChange(
        BranchProductPrice Price,
        StockTransferLine Line,
        bool IsNew);

    private async Task<(List<ReceiptPriceChange> Changes, StockTransferResult? Error)>
        PrepareReceiptPricesAsync(StockTransfer transfer, CancellationToken cancellationToken)
    {
        var changes = new List<ReceiptPriceChange>();
        var destination = await _unitOfWork.InventoryLocations.FindAsync(
            predicate: l => l.Id == transfer.DestinationLocationId,
            cancellationToken: cancellationToken);

        if (destination?.BranchId is not Guid branchId)
        {
            return (changes, Failure(StockTransferResultStatus.Conflict,
                "The destination must belong to a branch to receive this transfer."));
        }

        var productIds = transfer.Lines.Select(l => l.ProductId).Distinct().ToArray();
        var prices = await _unitOfWork.BranchProductPrices.GetAllAsync(
            predicate: p => p.BranchId == branchId && productIds.Contains(p.ProductId),
            trackChanges: true,
            cancellationToken: cancellationToken);
        var pricesByProduct = prices.ToDictionary(p => p.ProductId);

        foreach (var group in transfer.Lines.GroupBy(l => l.ProductId))
        {
            // No trustworthy creation snapshot exists for pre-migration transfers.
            // Receive their stock normally and leave current pricing untouched.
            if (group.Any(l => !l.BaselineRevisionAtCreation.HasValue))
            {
                continue;
            }

            var line = group.OrderBy(l => l.LineNumber).First();
            if (group.Any(l => l.BaselineUnitPrice != line.BaselineUnitPrice ||
                               l.BaselineRevisionAtCreation != line.BaselineRevisionAtCreation))
            {
                return (changes, Failure(StockTransferResultStatus.Conflict,
                    $"Transfer lines for product '{group.Key}' have inconsistent pricing."));
            }

            pricesByProduct.TryGetValue(group.Key, out var price);

            // Even changing a baseline and later changing it back makes this stale.
            if ((price?.BaselineRevision ?? 0) != line.BaselineRevisionAtCreation)
            {
                continue;
            }

            // No value change means no additional revision or history entry.
            if (price?.BaselineUnitPrice == line.BaselineUnitPrice)
            {
                continue;
            }

            if (price?.Revision == int.MaxValue)
            {
                return (changes, Failure(StockTransferResultStatus.Conflict,
                    $"The maximum price revision has been reached for product '{group.Key}'."));
            }

            var isNew = price is null;
            price ??= new BranchProductPrice
            {
                BranchId = branchId,
                ProductId = group.Key,
                Revision = 0
            };

            changes.Add(new ReceiptPriceChange(price, line, isNew));
        }

        return (changes, null);
    }

    public async Task<StockTransferResult> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _unitOfWork.StockTransfers.FindAsync(
            predicate: t => t.Id == id,
            includes: [t => t.Lines],
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (transfer is null)
        {
            return Failure(
                StockTransferResultStatus.NotFound,
                "Stock transfer was not found.");
        }

        if (transfer.Status == StockTransferStatus.Cancelled)
        {
            return Success(transfer);
        }

        if (transfer.Status != StockTransferStatus.Draft)
        {
            return Failure(
                StockTransferResultStatus.Conflict,
                "Only a draft transfer can be cancelled.");
        }

        transfer.Status = StockTransferStatus.Cancelled;
        transfer.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Success(transfer);
    }

    private async Task<StockTransferResult?>
        ValidateDispatchReferencesAsync(
            StockTransfer transfer,
            CancellationToken cancellationToken)
    {
        var source = await _unitOfWork.InventoryLocations.FindAsync(
            predicate: l => l.Id == transfer.SourceLocationId,
            cancellationToken: cancellationToken);

        var destination = await _unitOfWork.InventoryLocations.FindAsync(
            predicate: l => l.Id == transfer.DestinationLocationId,
            includes: [l => l.Branch!],
            cancellationToken: cancellationToken);

        if (source is null || destination is null)
        {
            return Failure(
                StockTransferResultStatus.InvalidRequest,
                "Source or destination location does not exist.");
        }

        if (source.BranchId.HasValue)
        {
            return Failure(
                StockTransferResultStatus.InvalidRequest,
                "This workflow requires a central source location.");
        }

        if (!destination.BranchId.HasValue)
        {
            return Failure(
                StockTransferResultStatus.InvalidRequest,
                "This workflow requires a branch destination location.");
        }

        if (!source.IsActive ||
            !destination.IsActive ||
            destination.Branch?.IsActive != true)
        {
            return Failure(
                StockTransferResultStatus.Conflict,
                "Source, destination, and destination branch " +
                "must be active before dispatch.");
        }

        foreach (var productId in transfer.Lines
                     .Select(l => l.ProductId)
                     .Distinct())
        {
            var product = await _unitOfWork.Products.FindAsync(
                predicate: p => p.Id == productId,
                cancellationToken: cancellationToken);

            if (product is null)
            {
                return Failure(
                    StockTransferResultStatus.InvalidRequest,
                    $"Product '{productId}' does not exist.");
            }

            if (!product.IsActive)
            {
                return Failure(
                    StockTransferResultStatus.Conflict,
                    $"Product '{productId}' is inactive.");
            }
        }

        return null;
    }

    private static string? ValidateRequest(CreateStockTransferDto request)
    {
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                results,
                validateAllProperties: true))
        {
            return results[0].ErrorMessage ?? "Invalid transfer.";
        }

        foreach (var line in request.Lines)
        {
            results.Clear();

            if (!Validator.TryValidateObject(
                    line,
                    new ValidationContext(line),
                    results,
                    validateAllProperties: true))
            {
                return results[0].ErrorMessage ?? "Invalid transfer line.";
            }
        }

        if (request.Lines.GroupBy(l => l.ProductId)
            .Any(group => group.Select(l => l.BaselineUnitPrice).Distinct().Count() > 1))
        {
            return "All lines for the same product must use the same baseline price.";
        }

        return null;
    }

    private static StockTransferDto ToDto(StockTransfer transfer)
    {
        var lines = transfer.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new StockTransferLineDto(
                l.Id,
                l.LineNumber,
                l.ProductId,
                l.Quantity,
                l.BaselineUnitPrice,
                l.Quantity * l.BaselineUnitPrice))
            .ToList();

        return new StockTransferDto(
            transfer.Id,
            transfer.Number,
            transfer.SourceLocationId,
            transfer.DestinationLocationId,
            transfer.Status.ToString(),
            transfer.Notes,
            transfer.CreatedAtUtc,
            transfer.ShippedAtUtc,
            transfer.ReceivedAtUtc,
            transfer.ReturnReason,
            transfer.ReturnRequestedAtUtc,
            transfer.ReturnedAtUtc,
            lines.Sum(l => l.BaselineAmount),
            lines);
    }

    public async Task<StockTransferResult> RequestReturnAsync(
    Guid id,
    RequestStockTransferReturnDto request,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) ||
            request.Reason.Length > 500)
        {
            return Failure(
                StockTransferResultStatus.InvalidRequest,
                "A return reason between 1 and 500 characters is required.");
        }

        var transfer = await _unitOfWork.StockTransfers.FindAsync(
            predicate: t => t.Id == id,
            includes: [t => t.Lines],
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (transfer is null)
        {
            return Failure(
                StockTransferResultStatus.NotFound,
                "Stock transfer was not found.");
        }

        // Repeated requests preserve the original reason and timestamp.
        if (transfer.Status == StockTransferStatus.ReturnRequested ||
            transfer.Status == StockTransferStatus.Returned)
        {
            return Success(transfer);
        }

        if (transfer.Status != StockTransferStatus.InTransit)
        {
            return Failure(
                StockTransferResultStatus.Conflict,
                "Only an in-transit transfer can be requested for return.");
        }

        var now = DateTime.UtcNow;

        if (transfer.ShippedAtUtc is DateTime shippedAt && now < shippedAt)
        {
            now = shippedAt;
        }

        transfer.Status = StockTransferStatus.ReturnRequested;
        transfer.ReturnReason = request.Reason.Trim();
        transfer.ReturnRequestedAtUtc = now;
        transfer.UpdatedAtUtc = now;

        // The goods are still in transit: do not change stock here.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Success(transfer);
    }

    public async Task<StockTransferResult> ConfirmReturnAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _unitOfWork.StockTransfers.FindAsync(
            predicate: t => t.Id == id,
            includes: [t => t.Lines],
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (transfer is null)
        {
            return Failure(
                StockTransferResultStatus.NotFound,
                "Stock transfer was not found.");
        }

        if (transfer.Status == StockTransferStatus.Returned)
        {
            return Success(transfer);
        }

        if (transfer.Status != StockTransferStatus.ReturnRequested)
        {
            return Failure(
                StockTransferResultStatus.Conflict,
                "A return must be requested before it can be confirmed.");
        }

        if (transfer.Lines.Count == 0)
        {
            return Failure(
                StockTransferResultStatus.InvalidRequest,
                "The transfer must contain at least one line.");
        }

        // Prepare all balance changes before changing quantities.
        var changes = new List<(
            StockBalance Balance,
            decimal Quantity,
            bool IsNew)>();

        foreach (var group in transfer.Lines.GroupBy(l => l.ProductId))
        {
            var productId = group.Key;
            var quantity = group.Sum(l => l.Quantity);

            var balance = await _unitOfWork.StockBalances.FindAsync(
                predicate: s =>
                    s.ProductId == productId &&
                    s.InventoryLocationId == transfer.SourceLocationId,
                trackChanges: true,
                cancellationToken: cancellationToken);

            var isNew = balance is null;

            balance ??= new StockBalance
            {
                ProductId = productId,
                InventoryLocationId = transfer.SourceLocationId,
                Quantity = 0
            };

            if (quantity > MaximumStockQuantity - balance.Quantity)
            {
                return Failure(
                    StockTransferResultStatus.Conflict,
                    $"Returning product '{productId}' would exceed " +
                    "the supported stock quantity.");
            }

            changes.Add((balance, quantity, isNew));
        }

        foreach (var change in changes)
        {
            change.Balance.Quantity += change.Quantity;

            if (change.IsNew)
            {
                await _unitOfWork.StockBalances.AddAsync(
                    change.Balance,
                    cancellationToken);
            }
        }

        var now = DateTime.UtcNow;

        if (transfer.ReturnRequestedAtUtc is DateTime requestedAt &&
            now < requestedAt)
        {
            now = requestedAt;
        }

        foreach (var line in transfer.Lines)
        {
            await _unitOfWork.StockMovements.AddAsync(
                new StockMovement
                {
                    ProductId = line.ProductId,
                    InventoryLocationId = transfer.SourceLocationId,
                    MovementType = StockMovementType.TransferReturn,
                    QuantityChange = line.Quantity,
                    StockTransferLineId = line.Id,
                    OccurredAtUtc = now
                },
                cancellationToken);
        }

        transfer.Status = StockTransferStatus.Returned;
        transfer.ReturnedAtUtc = now;
        transfer.UpdatedAtUtc = now;

        // Restore balances, write movements, and update status together.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Success(transfer);
    }

    private static StockTransferResult Success(StockTransfer transfer)
    {
        return new(
            StockTransferResultStatus.Success,
            ToDto(transfer));
    }

    private static StockTransferResult Failure(
        StockTransferResultStatus status,
        string error)
    {
        return new(status, Error: error);
    }
}
