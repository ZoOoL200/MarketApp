using MarketApp.Application.DTOs.Products;

namespace MarketApp.Application.Interfaces.Services;

public interface IProductPhotoService
{
    Task<IReadOnlyList<ProductPhotoDto>?> GetListAsync(Guid productId,
        CancellationToken cancellationToken = default);

    Task<ProductPhotoContent?> GetContentAsync(Guid productId, Guid photoId,
        CancellationToken cancellationToken = default);

    Task<PhotoUploadResult> UploadAsync(Guid productId, Stream input, int sortOrder,
        CancellationToken cancellationToken = default);

    Task<bool> SetSortOrderAsync(Guid productId, Guid photoId, int sortOrder,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid productId, Guid photoId,
        CancellationToken cancellationToken = default);
}
