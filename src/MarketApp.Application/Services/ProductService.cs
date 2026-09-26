using MarketApp.Application.Common;
using MarketApp.Application.Common.Result;
using MarketApp.Application.DTOs.Products;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Application.Persistence.Contracts;
using MarketApp.Domain.Entity.Main;

namespace MarketApp.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<ProductDto>> GetPageAsync(
        ProductQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var page = await _unitOfWork.Products.GetPageAsync(
            pageNumber: query.PageNumber,
            pageSize: query.PageSize,
            orderBy: products => products
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id),
            predicate: p =>
                (!query.CategoryId.HasValue ||
                    p.CategoryId == query.CategoryId.Value) &&
                (!query.IsActive.HasValue ||
                    p.IsActive == query.IsActive.Value),
            includes: [p => p.Category],
            cancellationToken: cancellationToken);

        var items = page.Items
            .Select(p => ToDto(p, p.Category.Name))
            .ToList();

        return new PagedResult<ProductDto>(
            items,
            page.TotalCount,
            page.PageNumber,
            page.PageSize);
    }

    public async Task<ProductDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.FindAsync(
            predicate: p => p.Id == id,
            includes: [p => p.Category],
            cancellationToken: cancellationToken);

        return product is null
            ? null
            : ToDto(product, product.Category.Name);
    }

    public async Task<ProductSaveResult> CreateAsync(
        SaveProductDto request,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.FindAsync(
            predicate: c => c.Id == request.CategoryId,
            cancellationToken: cancellationToken);

        if (category is null)
        {
            return new(ProductSaveStatus.CategoryNotFound);
        }

        var sku = request.Sku.Trim().ToUpperInvariant();

        var skuExists = await _unitOfWork.Products.AnyAsync(
            predicate: p => p.Sku == sku,
            cancellationToken: cancellationToken);

        if (skuExists)
        {
            return new(ProductSaveStatus.DuplicateSku);
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Sku = sku,
            CategoryId = category.Id,
            IsActive = request.IsActive
        };

        await _unitOfWork.Products.AddAsync(
            product,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new(
            ProductSaveStatus.Success,
            ToDto(product, category.Name));
    }

    public async Task<ProductSaveResult> UpdateAsync(
        Guid id,
        SaveProductDto request,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.FindAsync(
            predicate: p => p.Id == id,
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (product is null)
        {
            return new(ProductSaveStatus.NotFound);
        }

        var category = await _unitOfWork.Categories.FindAsync(
            predicate: c => c.Id == request.CategoryId,
            cancellationToken: cancellationToken);

        if (category is null)
        {
            return new(ProductSaveStatus.CategoryNotFound);
        }

        var sku = request.Sku.Trim().ToUpperInvariant();

        var skuExists = await _unitOfWork.Products.AnyAsync(
            predicate: p => p.Sku == sku && p.Id != id,
            cancellationToken: cancellationToken);

        if (skuExists)
        {
            return new(ProductSaveStatus.DuplicateSku);
        }

        product.Name = request.Name.Trim();
        product.Sku = sku;
        product.CategoryId = category.Id;
        product.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new(
            ProductSaveStatus.Success,
            ToDto(product, category.Name));
    }

    private static ProductDto ToDto(
        Product product,
        string categoryName)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Sku,
            product.IsActive,
            product.CategoryId,
            categoryName);
    }
}