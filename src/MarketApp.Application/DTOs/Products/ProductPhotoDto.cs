using MarketApp.Domain.Entity.Main;

namespace MarketApp.Application.DTOs.Products;

public record ProductPhotoDto(
    Guid Id,
    string StorageKey,
    string ContentType,
    long FileSizeBytes,
    string ContentHash,
    int SortOrder,
    DateTime CreatedAtUtc)
{
    public string DownloadPath { get; init; } = string.Empty;

    public static ProductPhotoDto FromEntity(ProductPhoto photo) => new(
        photo.Id, photo.StorageKey, photo.ContentType, photo.FileSizeBytes,
        photo.ContentHash, photo.SortOrder, photo.CreatedAtUtc)
    {
        DownloadPath = $"/api/products/{photo.ProductId}/photos/{photo.Id}/content"
    };
}
