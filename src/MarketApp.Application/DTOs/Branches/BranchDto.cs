namespace MarketApp.Application.DTOs.Branches;

public record BranchDto(
    Guid Id,
    string Name,
    string Code,
    string? Address,
    string? Phone,
    bool IsActive);