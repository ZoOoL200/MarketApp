namespace MarketApp.Domain.Entity.Main;

public class ProductPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    // A stable object key, never a local Windows path or expiring download URL.
    public string StorageKey { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    // Uppercase SHA-256 of the stored bytes, for the future desktop cache.
    public string ContentHash { get; set; } = string.Empty;

    // The first photo in SortOrder/Id order is the display photo.
    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Pending uploads are not visible to clients.
    public bool IsReady { get; set; }

    // Hide immediately; storage deletion is retried by the cleanup worker.
    public DateTime? DeletedAtUtc { get; set; }
}
