using MarketApp.Application.Common.Security;
using MarketApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MarketApp.Infrastructure.Identity;

public class IdentityInitializer
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IConfiguration _configuration;

    public IdentityInitializer(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task<string> InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        string[] roleNames =
        [
            AppRoles.Stakeholder,
            AppRoles.BranchManager,
            AppRoles.Seller
        ];

        foreach (var roleName in roleNames)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var role = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName
            };

            EnsureSuccess(
                await _roleManager.CreateAsync(role),
                $"Creating role '{roleName}'");
        }

        var stakeholders =
            await _userManager.GetUsersInRoleAsync(AppRoles.Stakeholder);

        if (stakeholders.Count > 0)
        {
            await transaction.CommitAsync(cancellationToken);

            return "Roles are ready. A stakeholder account already exists; "
                + "no account or password was changed.";
        }

        var userName = GetRequiredSetting("UserName").Trim();
        var email = GetRequiredSetting("Email").Trim();
        var fullName = GetRequiredSetting("FullName").Trim();

        // Preserve the password exactly as entered.
        var password = GetRequiredSetting("Password");

        if (userName.Length > 256)
        {
            throw new InvalidOperationException(
                "The stakeholder username cannot exceed 256 characters.");
        }

        if (email.Length > 256)
        {
            throw new InvalidOperationException(
                "The stakeholder email cannot exceed 256 characters.");
        }

        if (fullName.Length > 150)
        {
            throw new InvalidOperationException(
                "The stakeholder full name cannot exceed 150 characters.");
        }

        var existingUser = await _userManager.FindByNameAsync(userName);
        var existingEmail = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null || existingEmail is not null)
        {
            throw new InvalidOperationException(
                "The configured username or email belongs to an existing "
                + "account. Setup will not promote an existing account.");
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            FullName = fullName,
            IsActive = true,
            EmailConfirmed = false,
            LockoutEnabled = true
        };

        EnsureSuccess(
            await _userManager.CreateAsync(user, password),
            "Creating the stakeholder account");

        EnsureSuccess(
            await _userManager.AddToRoleAsync(user, AppRoles.Stakeholder),
            "Assigning the stakeholder role");

        await transaction.CommitAsync(cancellationToken);

        return "Identity setup completed. Roles and the first "
            + "stakeholder account were created.";
    }

    public async Task<string> InitializeSystemAdminAsync(
    CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        // Create the role if it does not exist.
        // Ensure both roles exist, including on a new database.
        string[] requiredRoles =
        [
            AppRoles.SystemAdmin,
            AppRoles.Stakeholder,
            AppRoles.BranchManager,
            AppRoles.Seller
        ];

        foreach (var roleName in requiredRoles)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var role = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName
            };

            EnsureSuccess(
                await _roleManager.CreateAsync(role),
                $"Creating role '{roleName}'");
        }

        // Initialization creates only the first administrator.
        var administrators =
            await _userManager.GetUsersInRoleAsync(AppRoles.SystemAdmin);

        if (administrators.Count > 0)
        {
            await transaction.CommitAsync(cancellationToken);

            return "A SystemAdmin account already exists. "
                + "No account or password was changed.";
        }

        string ReadSetting(string name)
        {
            var value = _configuration[$"BootstrapSystemAdmin:{name}"];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Missing configuration: BootstrapSystemAdmin:{name}.");
            }

            return value;
        }

        var userName = ReadSetting("UserName").Trim();
        var fullName = ReadSetting("FullName").Trim();
        var email = ReadSetting("Email").Trim();

        // Preserve the password exactly as configured.
        var password = ReadSetting("Password");

        if (userName.Length > 256 ||
            email.Length > 256 ||
            fullName.Length > 150)
        {
            throw new InvalidOperationException(
                "Username and email must not exceed 256 characters. "
                + "Full name must not exceed 150 characters.");
        }

        var existingUser = await _userManager.FindByNameAsync(userName);
        var existingEmail = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null || existingEmail is not null)
        {
            throw new InvalidOperationException(
                "The configured username or email already belongs to an account. "
                + "Use a unique username and email for the administrator.");
        }

        var administrator = new ApplicationUser
        {
            UserName = userName,
            FullName = fullName,
            Email = email,
            IsActive = true,
            EmailConfirmed = false,
            LockoutEnabled = true
        };

        EnsureSuccess(
            await _userManager.CreateAsync(administrator, password),
            "Creating SystemAdmin account");

        EnsureSuccess(
            await _userManager.AddToRoleAsync(
                administrator,
                AppRoles.SystemAdmin),
            "Assigning SystemAdmin role");

        await transaction.CommitAsync(cancellationToken);

        return "The first SystemAdmin account was created successfully.";
    }
    private string GetRequiredSetting(string name)
    {
        var value = _configuration[$"BootstrapStakeholder:{name}"];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Missing configuration: BootstrapStakeholder:{name}.");
        }

        return value;
    }

    private static void EnsureSuccess(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => error.Description));

        throw new InvalidOperationException($"{operation} failed: {errors}");
    }
}