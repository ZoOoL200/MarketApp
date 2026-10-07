using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Infrastructure.Services;
using MarketApp.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketApp.Infrastructure.Extension;

public static class ProductPhotoRegistration
{
    public static IServiceCollection AddProductPhotos(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("PhotoStorage").Get<PhotoStorageOptions>() ?? new();
        services.AddSingleton(options);
        if (string.Equals(options.Provider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IProductPhotoStorage, LocalProductPhotoStorage>();
        }
        else if (string.Equals(options.Provider, "S3", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.BucketName) || string.IsNullOrWhiteSpace(options.Region))
                throw new InvalidOperationException("PhotoStorage:BucketName and Region are required for S3.");
            var hasAccessKey = !string.IsNullOrWhiteSpace(options.AccessKeyId);
            var hasSecretKey = !string.IsNullOrWhiteSpace(options.SecretAccessKey);
            if (hasAccessKey != hasSecretKey)
                throw new InvalidOperationException("Provide both S3 access credentials or neither.");
            services.AddSingleton<IAmazonS3>(_ =>
            {
                var region = RegionEndpoint.GetBySystemName(options.Region);
                return hasAccessKey
                    ? new AmazonS3Client(new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey), region)
                    : new AmazonS3Client(region);
            });
            services.AddSingleton<IProductPhotoStorage, S3ProductPhotoStorage>();
        }
        else throw new InvalidOperationException("PhotoStorage:Provider must be Local or S3.");

        services.AddScoped<ProductPhotoService>();
        services.AddScoped<IProductPhotoService>(provider => provider.GetRequiredService<ProductPhotoService>());
        services.AddHostedService<ProductPhotoCleanupWorker>();
        return services;
    }
}
