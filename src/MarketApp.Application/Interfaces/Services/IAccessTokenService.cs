using System.Security.Claims;
using MarketApp.Application.DTOs.Authentication;

namespace MarketApp.Application.Interfaces.Services;

public interface IAccessTokenService
{
    AccessTokenResponseDto CreateToken(ClaimsPrincipal principal);
}