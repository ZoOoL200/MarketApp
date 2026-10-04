using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Authentication;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace MarketApp.Infrastructure.Identity;

public class AuthSessionService : IAuthSessionService
{
    private readonly AppDbContext _context;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAccessTokenService _accessTokenService;

    public AuthSessionService(
        AppDbContext context,
        SignInManager<ApplicationUser> signInManager,
        IAccessTokenService accessTokenService)
    {
        _context = context;
        _signInManager = signInManager;
        _accessTokenService = accessTokenService;
    }

    public async Task<AuthResponseDto?> CreateAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var user =
            await _signInManager.ValidateSecurityStampAsync(principal);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var securityStamp = user.SecurityStamp;

        if (string.IsNullOrWhiteSpace(securityStamp))
        {
            return null;
        }

        var now = DateTime.UtcNow;

        var session = new AuthSession
        {
            UserId = user.Id,
            SecurityStamp = securityStamp,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(7)
        };

        var rawRefreshToken = GenerateRefreshToken();

        var token = CreateRefreshToken(
            session,
            rawRefreshToken,
            now);

        var sessionPrincipal =
            await CreatePrincipalAsync(user, session.Id);

        var response = CreateResponse(
            sessionPrincipal,
            rawRefreshToken,
            session.ExpiresAtUtc);

        _context.AuthSessions.Add(session);
        _context.RefreshTokens.Add(token);

        // One SaveChanges persists both records atomically.
        await _context.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<AuthResponseDto?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)
            || refreshToken.Length > 256)
        {
            return null;
        }

        var tokenHash = HashToken(refreshToken);

        var sessionId = await _context.RefreshTokens
            .AsNoTracking()
            .Where(x => x.TokenHash == tokenHash)
            .Select(x => (Guid?)x.AuthSessionId)
            .SingleOrDefaultAsync(cancellationToken);

        if (sessionId is null)
        {
            return null;
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var session = await LockSessionAsync(
            sessionId.Value,
            cancellationToken);

        var now = DateTime.UtcNow;

        if (session is null
            || session.RevokedAtUtc is not null
            || session.ExpiresAtUtc <= now
            || session.CreatedAtUtc > now)
        {
            return null;
        }

        // Read again after acquiring the session lock.
        var storedToken = await _context.RefreshTokens
            .AsTracking()
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash
                    && x.AuthSessionId == session.Id,
                cancellationToken);

        if (storedToken is null)
        {
            return null;
        }

        if (storedToken.ConsumedAtUtc is not null)
        {
            // Reuse of a consumed token revokes the whole session.
            session.RevokedAtUtc = now;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return null;
        }

        if (storedToken.ExpiresAtUtc <= now
            || storedToken.CreatedAtUtc > now)
        {
            return null;
        }

        var user = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == session.UserId,
                cancellationToken);

        if (user is null
            || !user.IsActive
            || !string.Equals(
                user.SecurityStamp,
                session.SecurityStamp,
                StringComparison.Ordinal))
        {
            session.RevokedAtUtc = now;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return null;
        }

        var principal = await CreatePrincipalAsync(user, session.Id);
        var newRawRefreshToken = GenerateRefreshToken();

        var replacement = CreateRefreshToken(
            session,
            newRawRefreshToken,
            now);

        var response = CreateResponse(
            principal,
            newRawRefreshToken,
            session.ExpiresAtUtc);

        storedToken.ConsumedAtUtc = now;
        session.LastRefreshedAtUtc = now;

        _context.RefreshTokens.Add(replacement);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return response;
    }

    public async Task LogoutAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(
                principal.FindFirstValue(AppClaimTypes.SessionId),
                out var sessionId)
            || !Guid.TryParse(
                principal.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
        {
            return;
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var session = await LockSessionAsync(
            sessionId,
            cancellationToken);

        if (session is not null
            && session.UserId == userId
            && session.RevokedAtUtc is null)
        {
            var now = DateTime.UtcNow;

            session.RevokedAtUtc = now < session.CreatedAtUtc
                ? session.CreatedAtUtc
                : now;

            await _context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private Task<AuthSession?> LockSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        // Include xmin because Version is mapped to it.
        // The interpolated value is passed as a SQL parameter.
        return _context.AuthSessions
            .FromSqlInterpolated($"""
                SELECT s.*, s.xmin
                FROM "AuthSessions" AS s
                WHERE s."Id" = {sessionId}
                FOR UPDATE
                """)
            .AsTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(
        ApplicationUser user,
        Guid sessionId)
    {
        var principal =
            await _signInManager.CreateUserPrincipalAsync(user);

        var identity = (ClaimsIdentity)principal.Identity!;

        foreach (var claim in identity
            .FindAll(AppClaimTypes.SessionId)
            .ToArray())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(
            AppClaimTypes.SessionId,
            sessionId.ToString()));

        return principal;
    }

    private AuthResponseDto CreateResponse(
        ClaimsPrincipal principal,
        string refreshToken,
        DateTime sessionExpiresAtUtc)
    {
        var accessToken = _accessTokenService.CreateToken(principal);

        return new AuthResponseDto(
            accessToken.TokenType,
            accessToken.AccessToken,
            accessToken.ExpiresIn,
            refreshToken,
            sessionExpiresAtUtc);
    }

    private static RefreshToken CreateRefreshToken(
        AuthSession session,
        string rawToken,
        DateTime now)
    {
        return new RefreshToken
        {
            AuthSessionId = session.Id,
            TokenHash = HashToken(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = session.ExpiresAtUtc
        };
    }

    private static string GenerateRefreshToken()
    {
        return WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(32));
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);

        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}