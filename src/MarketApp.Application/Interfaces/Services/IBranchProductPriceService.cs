using MarketApp.Application.Common;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Pricing;

namespace MarketApp.Application.Interfaces.Services;

public interface IBranchProductPriceService
{
    Task<BranchProductPriceDto?> GetManagementPriceAsync(
        Guid branchId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<SellerProductPriceDto?> GetSellerPriceAsync(
        Guid branchId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<BranchProductPriceHistoryDto>?> GetHistoryAsync(
        Guid branchId,
        Guid productId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<BranchPriceResult> SetBaselineAsync(
        Guid branchId,
        Guid productId,
        UpdateBranchPriceDto request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<BranchPriceResult> SetMinimumSellingPriceAsync(
        Guid branchId,
        Guid productId,
        UpdateBranchPriceDto request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}