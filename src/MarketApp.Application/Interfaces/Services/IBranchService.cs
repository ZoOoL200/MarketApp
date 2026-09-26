using MarketApp.Application.Common;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Branches;

namespace MarketApp.Application.Interfaces.Services;

public interface IBranchService
{
    Task<PagedResult<BranchDto>> GetPageAsync(
        BranchQueryDto query,
        CancellationToken cancellationToken = default);

    Task<BranchDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BranchSaveResult> CreateAsync(
        SaveBranchDto request,
        CancellationToken cancellationToken = default);

    Task<BranchSaveResult> UpdateAsync(
        Guid id,
        SaveBranchDto request,
        CancellationToken cancellationToken = default);
}