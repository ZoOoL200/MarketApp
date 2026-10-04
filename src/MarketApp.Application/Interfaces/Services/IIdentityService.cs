using System.Security.Claims;
using MarketApp.Application.DTOs.Authentication;

namespace MarketApp.Application.Interfaces.Services;

public interface IIdentityService
{
    Task<ClaimsPrincipal?> AuthenticateAsync(LoginRequestDto request);

    Task<ClaimsPrincipal?> ValidateSessionAsync(ClaimsPrincipal principal);
}