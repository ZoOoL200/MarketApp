namespace MarketApp.Infrastructure.Identity;

public class AuthSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    // Captured when the session is created.
    // Compared with the user's current stamp during validation.
    public string SecurityStamp { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? LastRefreshedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public uint Version { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; }
        = new List<RefreshToken>();
}