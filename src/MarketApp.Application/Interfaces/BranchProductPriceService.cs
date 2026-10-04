using System.ComponentModel.DataAnnotations;
using MarketApp.Application.Common;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Pricing;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Pricing;
using MarketApp.Domain.Enums;

namespace MarketApp.Application.Services;

public class BranchProductPriceService : IBranchProductPriceService
{
    private readonly IUnitOfWork _unitOfWork;

    public BranchProductPriceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BranchProductPriceDto?> GetManagementPriceAsync(
        Guid branchId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var price = await _unitOfWork.BranchProductPrices.FindAsync(
            predicate: p =>
                p.BranchId == branchId &&
                p.ProductId == productId,
            cancellationToken: cancellationToken);

        return price is null ? null : ToDto(price);
    }

    public async Task<SellerProductPriceDto?> GetSellerPriceAsync(
        Guid branchId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var price = await _unitOfWork.BranchProductPrices.FindAsync(
            predicate: p =>
                p.BranchId == branchId &&
                p.ProductId == productId &&
                p.Branch.IsActive &&
                p.Product.IsActive,
            cancellationToken: cancellationToken);

        if (price is null ||
            !price.BaselineUnitPrice.HasValue ||
            !price.MinimumSellingPrice.HasValue)
        {
            return null;
        }

        return new SellerProductPriceDto(
            price.BranchId,
            price.ProductId,
            price.MinimumSellingPrice.Value,
            price.Revision,
            price.UpdatedAtUtc);
    }

    public async Task<PagedResult<BranchProductPriceHistoryDto>?>
        GetHistoryAsync(
            Guid branchId,
            Guid productId,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        var price = await _unitOfWork.BranchProductPrices.FindAsync(
            predicate: p =>
                p.BranchId == branchId &&
                p.ProductId == productId,
            cancellationToken: cancellationToken);

        if (price is null)
        {
            return null;
        }

        var page = await _unitOfWork.BranchProductPriceHistories
            .GetPageAsync(
                pageNumber: pageNumber,
                pageSize: pageSize,
                orderBy: history => history
                    .OrderByDescending(h => h.Revision)
                    .ThenByDescending(h => h.Id),
                predicate: h => h.BranchProductPriceId == price.Id,
                cancellationToken: cancellationToken);

        var items = page.Items
            .Select(h => new BranchProductPriceHistoryDto(
                h.Id,
                h.Revision,
                h.OldBaselineUnitPrice,
                h.NewBaselineUnitPrice,
                h.OldMinimumSellingPrice,
                h.NewMinimumSellingPrice,
                h.ChangeType.ToString(),
                h.Reason,
                h.ChangedAtUtc,
                h.ChangedByUserId,
                h.StockTransferLineId))
            .ToList();

        return new PagedResult<BranchProductPriceHistoryDto>(
            items,
            page.TotalCount,
            page.PageNumber,
            page.PageSize);
    }

    public Task<BranchPriceResult> SetBaselineAsync(
        Guid branchId,
        Guid productId,
        UpdateBranchPriceDto request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        return UpdatePriceAsync(
            branchId,
            productId,
            request,
            actorUserId,
            updateBaseline: true,
            cancellationToken: cancellationToken);
    }

    public Task<BranchPriceResult> SetMinimumSellingPriceAsync(
        Guid branchId,
        Guid productId,
        UpdateBranchPriceDto request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        return UpdatePriceAsync(
            branchId,
            productId,
            request,
            actorUserId,
            updateBaseline: false,
            cancellationToken: cancellationToken);
    }

    private async Task<BranchPriceResult> UpdatePriceAsync(
        Guid branchId,
        Guid productId,
        UpdateBranchPriceDto request,
        Guid actorUserId,
        bool updateBaseline,
        CancellationToken cancellationToken)
    {
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                validationResults,
                validateAllProperties: true))
        {
            return Failure(
                BranchPriceResultStatus.InvalidRequest,
                validationResults[0].ErrorMessage ??
                "Invalid price update.");
        }

        if (branchId == Guid.Empty ||
            productId == Guid.Empty ||
            actorUserId == Guid.Empty)
        {
            return Failure(
                BranchPriceResultStatus.InvalidRequest,
                "Branch, product, and authenticated user IDs are required.");
        }

        var branch = await _unitOfWork.Branches.FindAsync(
            predicate: b => b.Id == branchId,
            cancellationToken: cancellationToken);

        var product = await _unitOfWork.Products.FindAsync(
            predicate: p => p.Id == productId,
            cancellationToken: cancellationToken);

        if (branch is null || product is null)
        {
            return Failure(
                BranchPriceResultStatus.InvalidRequest,
                "Branch or product does not exist.");
        }

        if (!branch.IsActive || !product.IsActive)
        {
            return Failure(
                BranchPriceResultStatus.Conflict,
                "Branch and product must be active to change pricing.");
        }

        var price = await _unitOfWork.BranchProductPrices.FindAsync(
            predicate: p =>
                p.BranchId == branchId &&
                p.ProductId == productId,
            trackChanges: true,
            cancellationToken: cancellationToken);

        var isNew = price is null;
        var currentRevision = price?.Revision ?? 0;

        if (request.ExpectedRevision != currentRevision)
        {
            return Failure(
                BranchPriceResultStatus.Conflict,
                "Pricing has changed. Refresh the current price " +
                "and submit the update again.");
        }

        var currentValue = updateBaseline
            ? price?.BaselineUnitPrice
            : price?.MinimumSellingPrice;

        // An unchanged value does not create another price revision.
        if (!isNew && currentValue == request.Price)
        {
            return new(
                BranchPriceResultStatus.Success,
                ToDto(price!));
        }

        if (currentRevision == int.MaxValue)
        {
            return Failure(
                BranchPriceResultStatus.Conflict,
                "The maximum supported price revision has been reached.");
        }

        price ??= new BranchProductPrice
        {
            BranchId = branchId,
            ProductId = productId
        };

        var oldBaseline = price.BaselineUnitPrice;
        var oldMinimum = price.MinimumSellingPrice;

        if (updateBaseline)
        {
            price.BaselineUnitPrice = request.Price;
        }
        else
        {
            price.MinimumSellingPrice = request.Price;
        }

        var now = DateTime.UtcNow;

        if (!isNew && now < price.UpdatedAtUtc)
        {
            now = price.UpdatedAtUtc;
        }

        price.Revision = currentRevision + 1;
        price.UpdatedAtUtc = now;

        var history = new BranchProductPriceHistory
        {
            BranchProductPriceId = price.Id,
            BranchProductPrice = price,
            Revision = price.Revision,
            OldBaselineUnitPrice = oldBaseline,
            NewBaselineUnitPrice = price.BaselineUnitPrice,
            OldMinimumSellingPrice = oldMinimum,
            NewMinimumSellingPrice = price.MinimumSellingPrice,
            ChangeType = updateBaseline
                ? BranchPriceChangeType.ManualBaseline
                : BranchPriceChangeType.ManualMinimumSellingPrice,
            Reason = request.Reason.Trim(),
            ChangedAtUtc = now,
            ChangedByUserId = actorUserId
        };

        if (isNew)
        {
            await _unitOfWork.BranchProductPrices.AddAsync(
                price,
                cancellationToken);
        }

        await _unitOfWork.BranchProductPriceHistories.AddAsync(
            history,
            cancellationToken);

        // The current price and its history are committed together.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new(
            BranchPriceResultStatus.Success,
            ToDto(price));
    }

    private static BranchProductPriceDto ToDto(
        BranchProductPrice price)
    {
        return new BranchProductPriceDto(
            price.Id,
            price.BranchId,
            price.ProductId,
            price.BaselineUnitPrice,
            price.MinimumSellingPrice,
            price.Revision,
            price.UpdatedAtUtc);
    }

    private static BranchPriceResult Failure(
        BranchPriceResultStatus status,
        string error)
    {
        return new(status, Error: error);
    }
}