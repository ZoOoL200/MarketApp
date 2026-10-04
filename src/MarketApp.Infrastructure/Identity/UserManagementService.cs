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