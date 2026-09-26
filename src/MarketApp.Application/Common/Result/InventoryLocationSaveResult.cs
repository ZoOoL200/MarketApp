using MarketApp.Application.DTOs.InventoryLocations;

namespace MarketApp.Application.Common.Results;

public enum InventoryLocationSaveStatus
{
    Success,
    NotFound,
    DuplicateCode,
    BranchNotFound,
    BranchInactive
}

public record InventoryLocationSaveResult(
    InventoryLocationSaveStatus Status,
    InventoryLocationDto? Location = null);