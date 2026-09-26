using MarketApp.Application.Common.Result;
using MarketApp.Application.DTOs.Categories;

namespace MarketApp.Application.Interfaces.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateAsync(
        SaveCategoryDto request,
        CancellationToken cancellationToken = default);

    Task<CategoryDto?> UpdateAsync(
        Guid id,
        SaveCategoryDto request,
        CancellationToken cancellationToken = default);

    Task<CategoryDeleteResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}