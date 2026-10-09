namespace MarketApp.Api.Models;
// Describes the existing anonymous-object responses for OpenAPI consumers.
public record MessageResponse(string Message);
public record CurrentUserResponse(string? UserId, string? UserName, string? SessionId, IReadOnlyList<string> Roles);
public record HealthResponse(string Status);
