using MarketApp.Application.Common;
using MarketApp.Application.Common.Result;
using MarketApp.Application.DTOs.Products;

namespace MarketApp.Application.Interfaces.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPageAsync(
        ProductQueryDto query,
        CancellationToken cancellationToken = default);

    Task<ProductDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ProductSaveResult> CreateAsync(
        SaveProductDto request,
        CancellationToken cancellationToken = default);

    Task<ProductSaveResult> UpdateAsync(
        Guid id,
        SaveProductDto request,
        CancellationToken cancellationToken = default);
}