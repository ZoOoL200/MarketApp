using System.Security.Claims;
using MarketApp.Application.DTOs.Authentication;

namespace MarketApp.Application.Interfaces.Services;

public interface IAuthSessionService
{
    Task<AuthResponseDto?> CreateAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<AuthResponseDto?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}