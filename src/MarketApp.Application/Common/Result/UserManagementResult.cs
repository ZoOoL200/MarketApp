using MarketApp.Application.DTOs.Users;

namespace MarketApp.Application.Common.Results;

public enum UserManagementResultStatus
{
    Success,
    InvalidRequest,
    Conflict
}

public record UserManagementResult(
    UserManagementResultStatus Status,
    UserDetailsDto? User = null,
    string? Error = null);