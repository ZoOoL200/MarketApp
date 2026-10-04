using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MarketApp.Application.DTOs.Authentication;
using MarketApp.Application.Interfaces.Services;
using Microsoft.IdentityModel.Tokens;

namespace MarketApp.Infrastructure.Identity;

public class JwtAccessTokenService : IAccessTokenService
{
    private readonly JwtSettings _settings;
    private readonly SigningCredentials _signingCredentials;

    public JwtAccessTokenService(JwtSettings settings)
    {
        _settings = settings;

        _signingCredentials = new SigningCredentials(
            settings.CreateSigningKey(),
            SecurityAlgorithms.HmacSha256);
    }

    public AccessTokenResponseDto CreateToken(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (principal.Identity?.IsAuthenticated != true
            || string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException(
                "An authenticated user is required to create an access token.");
        }

        var now = DateTimeOffset.UtcNow;

        // Preserve Identity's user ID, username, roles, and security stamp.
        var claims = principal.Claims.ToList();

        claims.Add(new Claim(
            JwtRegisteredClaimNames.Sub,
            userId));

        claims.Add(new Claim(
            JwtRegisteredClaimNames.Jti,
            Guid.NewGuid().ToString("N")));

        claims.Add(new Claim(
            JwtRegisteredClaimNames.Iat,
            now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            ClaimValueTypes.Integer64));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(_settings.AccessTokenMinutes).UtcDateTime,
            signingCredentials: _signingCredentials);

        var serializedToken = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new AccessTokenResponseDto(
            TokenType: "Bearer",
            AccessToken: serializedToken,
            ExpiresIn: _settings.AccessTokenMinutes * 60);
    }
}