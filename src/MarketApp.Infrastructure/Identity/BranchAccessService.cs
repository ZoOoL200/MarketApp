using System.Security.Claims;
using MarketApp.Application.Common.Security;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketApp.Infrastructure.Identity;

public class BranchAccessService : IBranchAccessService
{
    private readonly AppDbContext _context;

    public BranchAccessService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanAccessAsync(
        ClaimsPrincipal user,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (user.Identity?.IsAuthenticated != true
            || branchId == Guid.Empty
            || !Guid.TryParse(
                user.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId)
            || userId == Guid.Empty)
        {
            return false;
        }

        if (user.IsInRole(AppRoles.Stakeholder))
        {
            // Stakeholders can also inspect inactive branches.
            return await _context.Branches
                .AsNoTracking()
                .AnyAsync(
                    branch => branch.Id == branchId,
                    cancellationToken);
        }

        if (!user.IsInRole(AppRoles.BranchManager)
            && !user.IsInRole(AppRoles.Seller))
        {
            return false;
        }

        return await _context.UserBranchAssignments
            .AsNoTracking()
            .AnyAsync(
                assignment =>
                    assignment.UserId == userId
                    && assignment.BranchId == branchId
                    && assignment.User.IsActive
                    && assignment.Branch.IsActive,
                cancellationToken);
    }
}