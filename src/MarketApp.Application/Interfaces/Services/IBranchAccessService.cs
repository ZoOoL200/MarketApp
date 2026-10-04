using System.Security.Claims;

namespace MarketApp.Application.Interfaces.Services;

public interface IBranchAccessService
{
    Task<bool> CanAccessAsync(
        ClaimsPrincipal user,
        Guid branchId,
        CancellationToken cancellationToken = default);
}