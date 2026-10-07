using MarketApp.Application.Interfaces.Services;

namespace MarketApp.Infrastructure.Storage;

public class LocalProductPhotoStorage : IProductPhotoStorage
{
    private readonly string _root;

    public LocalProductPhotoStorage(PhotoStorageOptions options)
    {
        _root = string.IsNullOrWhiteSpace(options.LocalRoot)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MarketApp", "ProductPhotos")
            : Path.GetFullPath(options.LocalRoot);
        Directory.CreateDirectory(_root);
    }

    private string GetPath(string storageKey)
    {
        ProductPhotoStorageIO.ValidateKey(storageKey);
        return Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar));
    }

    public async Task PutAsync(string storageKey, byte[] bytes, string contentType,
        CancellationToken cancellationToken = default)
    {
        var path = GetPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write,
            FileShare.None, bufferSize: 81920, useAsync: true);
        await file.WriteAsync(bytes, cancellationToken);
    }

    public async Task<byte[]?> ReadAsync(string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var file = new FileStream(GetPath(storageKey), FileMode.Open,
                FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
            return await ProductPhotoStorageIO.ReadBoundedAsync(file, cancellationToken);
        }
        catch (Exception exception) when (
            exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try { File.Delete(GetPath(storageKey)); }
        catch (DirectoryNotFoundException) { /* Already absent. */ }
        return Task.CompletedTask;
    }
}
