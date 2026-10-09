using MarketApp.Application.Common.Security;
using System.ComponentModel.DataAnnotations;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Users;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MarketApp.Infrastructure.Identity;

public class UserManagementService : IUserManagementService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UserManagementService(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<UserManagementResult> CreateAsync(
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                validationResults,
                validateAllProperties: true))
        {
            return new UserManagementResult(
                UserManagementResultStatus.InvalidRequest,
                Error: string.Join(
                    " ",
                    validationResults.Select(
                        x => x.ErrorMessage ?? "Invalid request.")));
        }

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();
        var branchIds = request.BranchIds.ToArray();

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            return new UserManagementResult(
                UserManagementResultStatus.Conflict,
                Error: "The requested role has not been initialized.");
        }

        if (await _userManager.FindByNameAsync(userName) is not null)
        {
            return new UserManagementResult(
                UserManagementResultStatus.Conflict,
                Error: "This username is already in use.");
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return new UserManagementResult(
                UserManagementResultStatus.Conflict,
                Error: "This email address is already in use.");
        }

        var activeBranchCount = await _context.Branches
            .AsNoTracking()
            .CountAsync(
                branch => branchIds.Contains(branch.Id)
                    && branch.IsActive,
                cancellationToken);

        if (activeBranchCount != branchIds.Length)
        {
            return new UserManagementResult(
                UserManagementResultStatus.InvalidRequest,
                Error: "Every assigned branch must exist and be active.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var user = new ApplicationUser
            {
                UserName = userName,
                FullName = fullName,
                Email = email,
                IsActive = true,
                EmailConfirmed = false,
                LockoutEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            // Do not trim or otherwise modify the password.
            var createResult = await _userManager.CreateAsync(
                user,
                request.Password);

            if (!createResult.Succeeded)
            {
                return FromIdentityFailure(createResult);
            }

            var roleResult = await _userManager.AddToRoleAsync(
                user,
                request.Role);

            if (!roleResult.Succeeded)
            {
                return FromIdentityFailure(roleResult);
            }

            var assignments = branchIds.Select(branchId =>
                new ApplicationUserBranch
                {
                    UserId = user.Id,
                    BranchId = branchId
                });

            _context.UserBranchAssignments.AddRange(assignments);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var dto = new UserDetailsDto(
                user.Id,
                user.UserName!,
                user.FullName,
                user.Email!,
                user.IsActive,
                user.EmailConfirmed,
                user.CreatedAtUtc,
                new[] { request.Role },
                branchIds);

            return new UserManagementResult(
                UserManagementResultStatus.Success,
                User: dto);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "UserNameIndex" or "EmailIndex"
            })
        {
            return new UserManagementResult(
                UserManagementResultStatus.Conflict,
                Error: "The username or email address is already in use.");
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ForeignKeyViolation
            })
        {
            return new UserManagementResult(
                UserManagementResultStatus.Conflict,
                Error: "A related branch or role changed. "
                    + "Refresh the data and try again.");
        }
    }

    public async Task<UserDetailsDto?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        var branchIds = await _context.UserBranchAssignments
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.BranchId)
            .Select(x => x.BranchId)
            .ToArrayAsync(cancellationToken);

        return new UserDetailsDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.FullName,
            user.Email ?? string.Empty,
            user.IsActive,
            user.EmailConfirmed,
            user.CreatedAtUtc,
            roles.ToArray(),
            branchIds);
    }

    public async Task<MarketApp.Application.Common.PagedResult<UserDetailsDto>> GetPageAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageNumber > 1000000 || pageSize < 1 || pageSize > 100)
            throw new MarketApp.Application.Common.Exceptions.RequestException(400, "Invalid page parameters.");
        var count = await _context.Users.CountAsync(ct);
        var ids = await _context.Users.AsNoTracking().OrderBy(u => u.UserName).ThenBy(u => u.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(u => u.Id).ToListAsync(ct);
        var items = new List<UserDetailsDto>();
        foreach (var id in ids) if (await GetByIdAsync(id, ct) is { } dto) items.Add(dto);
        return new(items, count, pageNumber, pageSize);
    }

    public async Task<UserDetailsDto> UpdateAccessAsync(Guid userId, Guid actorId, UpdateUserAccessDto request, CancellationToken ct = default)
    {
        if (request.BranchIds is null || request.BranchIds.Count > 100 || request.BranchIds.Contains(Guid.Empty))
            throw new MarketApp.Application.Common.Exceptions.RequestException(400, "Invalid branch assignments.");
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new MarketApp.Application.Common.Exceptions.RequestException(404, "User not found.");
        if (user.Id == actorId || await _userManager.IsInRoleAsync(user, AppRoles.Stakeholder))
            throw new MarketApp.Application.Common.Exceptions.RequestException(400, "This endpoint manages staff accounts only; it cannot alter stakeholder access.");
        var ids = request.BranchIds.Distinct().ToArray();
        if (request.IsActive && ids.Length == 0)
            throw new MarketApp.Application.Common.Exceptions.RequestException(400, "Active staff need at least one branch.");
        if (await _context.Branches.CountAsync(b => ids.Contains(b.Id) && b.IsActive, ct) != ids.Length)
            throw new MarketApp.Application.Common.Exceptions.RequestException(400, "Every assigned branch must exist and be active.");
        user.IsActive = request.IsActive;
        var old = await _context.UserBranchAssignments.Where(a => a.UserId == userId).ToListAsync(ct);
        _context.UserBranchAssignments.RemoveRange(old.Where(a => !ids.Contains(a.BranchId)));
        _context.UserBranchAssignments.AddRange(ids.Except(old.Select(a => a.BranchId))
            .Select(id => new ApplicationUserBranch { UserId = userId, BranchId = id }));
        // Invalidate access and refresh sessions together with the access change.
        user.SecurityStamp = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        await _context.AuthSessions.Where(a => a.UserId == userId && a.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.RevokedAtUtc, now), ct);
        await _context.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return (await GetByIdAsync(userId, ct))!;
    }

    private static UserManagementResult FromIdentityFailure(
        IdentityResult result)
    {
        var isConflict = result.Errors.Any(error =>
            error.Code is "DuplicateUserName"
                or "DuplicateEmail"
                or "ConcurrencyFailure");

        return new UserManagementResult(
            isConflict
                ? UserManagementResultStatus.Conflict
                : UserManagementResultStatus.InvalidRequest,
            Error: string.Join(
                " ",
                result.Errors.Select(error => error.Description)));
    }
}