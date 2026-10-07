namespace MarketApp.Infrastructure.Storage;

public class PhotoStorageOptions
{
    public string Provider { get; set; } = "Local";
    public string LocalRoot { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    // Optional development credentials. Use User Secrets, not committed config.
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
}
