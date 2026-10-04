namespace MarketApp.Application.DTOs.Authentication;

public record AccessTokenResponseDto(
    string TokenType,
    string AccessToken,
    int ExpiresIn);