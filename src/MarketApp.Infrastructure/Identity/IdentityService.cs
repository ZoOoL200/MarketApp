using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Authentication;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketApp.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _context;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    public async Task<ClaimsPrincipal?> AuthenticateAsync(
        LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName)
            || string.IsNullOrEmpty(request.Password))
        {
            return null;
        }

        var user = await _userManager.FindByNameAsync(
            request.UserName.Trim());

        if (user is null || !user.IsActive)
        {
            return null;
        }

        // Two-factor accounts need a separate login flow.
        if (await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return null;
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return null;
        }

        return await _signInManager.CreateUserPrincipalAsync(user);
    }

    public async Task<ClaimsPrincipal?> ValidateSessionAsync(
    ClaimsPrincipal principal)
    {
        if (!Guid.TryParse(
                principal.FindFirstValue(AppClaimTypes.SessionId),
                out var sessionId))
        {
            return null;
        }

        var user =
            await _signInManager.ValidateSecurityStampAsync(principal);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var session = await _context.AuthSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == sessionId && x.UserId == user.Id);

        var now = DateTime.UtcNow;

        if (session is null
            || session.RevokedAtUtc is not null
            || session.ExpiresAtUtc <= now
            || session.CreatedAtUtc > now
            || !string.Equals(
                session.SecurityStamp,
                user.SecurityStamp,
                StringComparison.Ordinal))
        {
            return null;
        }

        var updatedPrincipal =
            await _signInManager.CreateUserPrincipalAsync(user);

        var identity = (ClaimsIdentity)updatedPrincipal.Identity!;

        foreach (var claim in identity
            .FindAll(AppClaimTypes.SessionId)
            .ToArray())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(
            AppClaimTypes.SessionId,
            session.Id.ToString()));

        return updatedPrincipal;
    }
}