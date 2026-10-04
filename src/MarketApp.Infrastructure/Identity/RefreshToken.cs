namespace MarketApp.Infrastructure.Identity;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AuthSessionId { get; set; }
    public AuthSession AuthSession { get; set; } = null!;

    // SHA-256 hash represented as 64 uppercase hexadecimal characters.
    // The original refresh token is never stored here.
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    public uint Version { get; set; }
}