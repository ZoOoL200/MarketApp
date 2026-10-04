namespace MarketApp.Application.DTOs.Authentication;

public record AuthResponseDto(
    string TokenType,
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    DateTime SessionExpiresAtUtc);