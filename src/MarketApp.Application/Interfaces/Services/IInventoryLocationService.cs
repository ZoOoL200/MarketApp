using MarketApp.Application.Common;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.InventoryLocations;

namespace MarketApp.Application.Interfaces.Services;

public interface IInventoryLocationService
{
    Task<PagedResult<InventoryLocationDto>> GetPageAsync(
        InventoryLocationQueryDto query,
        CancellationToken cancellationToken = default);

    Task<InventoryLocationDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<InventoryLocationSaveResult> CreateAsync(
        CreateInventoryLocationDto request,
        CancellationToken cancellationToken = default);

    Task<InventoryLocationSaveResult> UpdateAsync(
        Guid id,
        SaveInventoryLocationDto request,
        CancellationToken cancellationToken = default);
}