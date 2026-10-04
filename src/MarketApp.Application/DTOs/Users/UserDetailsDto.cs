namespace MarketApp.Application.DTOs.Users;

public record UserDetailsDto(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    bool IsActive,
    bool EmailConfirmed,
    DateTime CreatedAtUtc,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> BranchIds);