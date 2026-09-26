using MarketApp.Application.Common;
using MarketApp.Application.Common.Results;
using MarketApp.Application.DTOs.Suppliers;

namespace MarketApp.Application.Interfaces.Services;

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> GetPageAsync(
        SupplierQueryDto query,
        CancellationToken cancellationToken = default);

    Task<SupplierDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SupplierSaveResult> CreateAsync(
        SaveSupplierDto request,
        CancellationToken cancellationToken = default);

    Task<SupplierSaveResult> UpdateAsync(
        Guid id,
        SaveSupplierDto request,
        CancellationToken cancellationToken = default);
}