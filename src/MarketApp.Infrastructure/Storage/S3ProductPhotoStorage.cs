using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using MarketApp.Application.Interfaces.Services;

namespace MarketApp.Infrastructure.Storage;

public class S3ProductPhotoStorage : IProductPhotoStorage
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;

    public S3ProductPhotoStorage(IAmazonS3 client, PhotoStorageOptions options)
    {
        _client = client;
        _bucketName = options.BucketName;
    }

    public async Task PutAsync(string storageKey, byte[] bytes, string contentType,
        CancellationToken cancellationToken = default)
    {
        ProductPhotoStorageIO.ValidateKey(storageKey);
        using var input = new MemoryStream(bytes, writable: false);
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName, Key = storageKey, InputStream = input,
            ContentType = contentType, AutoCloseStream = false
        }, cancellationToken);
    }

    public async Task<byte[]?> ReadAsync(string storageKey,
        CancellationToken cancellationToken = default)
    {
        ProductPhotoStorageIO.ValidateKey(storageKey);
        try
        {
            using var response = await _client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucketName, Key = storageKey
            }, cancellationToken);
            return await ProductPhotoStorageIO.ReadBoundedAsync(
                response.ResponseStream, cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string storageKey,
        CancellationToken cancellationToken = default)
    {
        ProductPhotoStorageIO.ValidateKey(storageKey);
        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName, Key = storageKey
        }, cancellationToken);
    }
}
