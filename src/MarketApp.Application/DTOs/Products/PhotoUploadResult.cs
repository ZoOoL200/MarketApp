namespace MarketApp.Application.DTOs.Products;

public enum PhotoUploadStatus
{
    Success,
    InvalidRequest,
    NotFound,
    Conflict
}

public record PhotoUploadResult(
    PhotoUploadStatus Status,
    ProductPhotoDto? Photo = null,
    string? Error = null);

public record ProductPhotoContent(
    byte[] Bytes,
    string ContentType,
    string ContentHash,
    DateTime CreatedAtUtc);
