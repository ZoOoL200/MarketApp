namespace MarketApp.Application.DTOs.InventoryLocations;

public record InventoryLocationDto(
    Guid Id,
    string Name,
    string Code,
    Guid? BranchId,
    string? BranchName,
    bool IsActive);