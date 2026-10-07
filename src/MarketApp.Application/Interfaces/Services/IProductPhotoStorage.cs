namespace MarketApp.Application.Interfaces.Services;

public interface IProductPhotoStorage
{
    Task PutAsync(string storageKey, byte[] bytes, string contentType,
        CancellationToken cancellationToken = default);

    Task<byte[]?> ReadAsync(string storageKey,
        CancellationToken cancellationToken = default);

    // Must succeed if the stored file is already absent.
    Task DeleteAsync(string storageKey,
        CancellationToken cancellationToken = default);
}
