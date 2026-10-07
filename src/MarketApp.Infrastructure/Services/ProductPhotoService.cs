using System.Security.Cryptography;
using MarketApp.Application.Common.Exceptions;
using MarketApp.Application.DTOs.Products;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Domain.Entity.Main;
using MarketApp.Infrastructure.Persistence;
using MarketApp.Infrastructure.Photos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketApp.Infrastructure.Services;

public class ProductPhotoService : IProductPhotoService
{
    private readonly AppDbContext _db;
    private readonly IProductPhotoStorage _storage;
    private readonly ILogger<ProductPhotoService> _logger;

    public ProductPhotoService(AppDbContext db, IProductPhotoStorage storage,
        ILogger<ProductPhotoService> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProductPhotoDto>?> GetListAsync(Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == productId, cancellationToken)) return null;
        var photos = await _db.ProductPhotos.AsNoTracking()
            .Where(p => p.ProductId == productId && p.IsReady && p.DeletedAtUtc == null)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync(cancellationToken);
        return photos.Select(ProductPhotoDto.FromEntity).ToList();
    }

    public async Task<ProductPhotoContent?> GetContentAsync(Guid productId, Guid photoId,
        CancellationToken cancellationToken = default)
    {
        var photo = await _db.ProductPhotos.AsNoTracking().SingleOrDefaultAsync(
            p => p.Id == photoId && p.ProductId == productId && p.IsReady && p.DeletedAtUtc == null,
            cancellationToken);
        if (photo is null) return null;
        var bytes = await _storage.ReadAsync(photo.StorageKey, cancellationToken);
        if (bytes is null) return null;
        if (bytes.LongLength != photo.FileSizeBytes ||
            Convert.ToHexString(SHA256.HashData(bytes)) != photo.ContentHash)
        {
            throw new InvalidDataException("Stored photo does not match its metadata.");
        }
        return new(bytes, photo.ContentType, photo.ContentHash, photo.CreatedAtUtc);
    }

    public async Task<PhotoUploadResult> UploadAsync(Guid productId, Stream input, int sortOrder,
        CancellationToken cancellationToken = default)
    {
        if (sortOrder < 0)
            return new(PhotoUploadStatus.InvalidRequest, Error: "Sort order cannot be negative.");

        var product = await _db.Products.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null) return new(PhotoUploadStatus.NotFound, Error: "Product was not found.");
        if (!product.IsActive)
            return new(PhotoUploadStatus.Conflict, Error: "Activate the product before uploading photos.");

        var processed = await ProductPhotoProcessor.NormalizeAsync(input, cancellationToken);
        if (processed.Bytes is null) return new(PhotoUploadStatus.InvalidRequest, Error: processed.Error);

        var photoId = Guid.NewGuid();
        var photo = new ProductPhoto
        {
            Id = photoId, ProductId = productId,
            StorageKey = $"products/{productId:N}/{photoId:N}.webp",
            ContentType = "image/webp", FileSizeBytes = processed.Bytes.LongLength,
            ContentHash = Convert.ToHexString(SHA256.HashData(processed.Bytes)),
            SortOrder = sortOrder, CreatedAtUtc = DateTime.UtcNow, IsReady = false
        };

        // Save the pending record first, so a stopped process leaves recoverable work.
        _db.ProductPhotos.Add(photo);
        await _db.SaveChangesAsync(cancellationToken);
        try
        {
            using var uploadTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            uploadTimeout.CancelAfter(TimeSpan.FromMinutes(2));
            await _storage.PutAsync(photo.StorageKey, processed.Bytes, photo.ContentType, uploadTimeout.Token);

            // Conditional publication cannot resurrect a photo deleted during upload.
            var updated = await _db.ProductPhotos
                .Where(p => p.Id == photo.Id && !p.IsReady && p.DeletedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsReady, true), cancellationToken);
            if (updated != 1)
                throw new ConflictException("The photo upload was cancelled or expired. Refresh the photo list.");

            photo.IsReady = true;
            return new(PhotoUploadStatus.Success, ProductPhotoDto.FromEntity(photo));
        }
        catch
        {
            try
            {
                using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var now = DateTime.UtcNow;
                // Never remove a published photo if its successful DB response was lost.
                await _db.ProductPhotos.Where(p => p.Id == photo.Id && !p.IsReady && p.DeletedAtUtc == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAtUtc, (DateTime?)now), cleanupTimeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Could not mark pending photo {PhotoId} for cleanup.", photo.Id);
            }
            throw;
        }
    }

    public async Task<bool> SetSortOrderAsync(Guid productId, Guid photoId, int sortOrder,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);
        return await _db.ProductPhotos
            .Where(p => p.Id == photoId && p.ProductId == productId && p.IsReady && p.DeletedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.SortOrder, sortOrder), cancellationToken) == 1;
    }

    public async Task DeleteAsync(Guid productId, Guid photoId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _db.ProductPhotos.Where(p => p.Id == photoId && p.ProductId == productId && p.DeletedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAtUtc, (DateTime?)now), cancellationToken);
        // Idempotent: an absent or already-deleted photo is also a successful delete.
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiredUploadCutoff = now.AddHours(-1);
        await _db.ProductPhotos
            .Where(p => !p.IsReady && p.DeletedAtUtc == null && p.CreatedAtUtc < expiredUploadCutoff)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAtUtc, (DateTime?)now), cancellationToken);

        // Grace period for storage requests completing after cancellation.
        var deletionCutoff = now.AddMinutes(-5);
        var deletedPhotos = await _db.ProductPhotos.AsNoTracking()
            .Where(p => p.DeletedAtUtc != null && p.DeletedAtUtc <= deletionCutoff)
            .OrderBy(p => p.DeletedAtUtc).ThenBy(p => p.Id).Take(25).ToListAsync(cancellationToken);
        foreach (var photo in deletedPhotos)
        {
            try
            {
                await _storage.DeleteAsync(photo.StorageKey, cancellationToken);
                await _db.ProductPhotos.Where(p => p.Id == photo.Id && p.DeletedAtUtc != null)
                    .ExecuteDeleteAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Could not clean product photo {PhotoId}; it will retry.", photo.Id);
            }
        }
    }
}
