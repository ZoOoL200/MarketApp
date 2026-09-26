using MarketApp.Application.Common.Result;
using MarketApp.Application.DTOs.Categories;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Main;

namespace MarketApp.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(
            orderBy: query => query
                .OrderBy(c => c.Name)
                .ThenBy(c => c.Id),
            cancellationToken: cancellationToken);

        return categories
            .Select(c => new CategoryDto(c.Id, c.Name))
            .ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.FindAsync(
            predicate: c => c.Id == id,
            cancellationToken: cancellationToken);

        return category is null
            ? null
            : new CategoryDto(category.Id, category.Name);
    }

    /// <summary>
    /// Creates a new category based on the provided SaveCategoryDto. Returns the created CategoryDto.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<CategoryDto> CreateAsync(
        SaveCategoryDto request,
        CancellationToken cancellationToken = default)
    {
        var category = new Category
        {
            Name = request.Name.Trim()
        };

        await _unitOfWork.Categories.AddAsync(
            category,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name);
    }

    /// <summary>
    /// Updates an existing category by its ID. Returns the updated CategoryDto if successful, or null if the category was not found.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<CategoryDto?> UpdateAsync(
        Guid id,
        SaveCategoryDto request,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.FindAsync(
            predicate: c => c.Id == id,
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (category is null)
        {
            return null;
        }

        category.Name = request.Name.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name);
    }


    /// <summary>
    /// Deletes a category by its ID. Returns a CategoryDeleteResult indicating the outcome of the operation.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<CategoryDeleteResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.FindAsync(
            predicate: c => c.Id == id,
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (category is null)
        {
            return CategoryDeleteResult.NotFound;
        }

        var hasProducts = await _unitOfWork.Products.AnyAsync(
            predicate: p => p.CategoryId == id,
            cancellationToken: cancellationToken);

        if (hasProducts)
        {
            return CategoryDeleteResult.HasProducts;
        }

        _unitOfWork.Categories.Remove(category);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CategoryDeleteResult.Deleted;
    }
}