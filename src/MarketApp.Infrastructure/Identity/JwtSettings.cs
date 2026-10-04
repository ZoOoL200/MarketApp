using Microsoft.IdentityModel.Tokens;

namespace MarketApp.Infrastructure.Identity;

public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKeyBase64 { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 30;

    public SymmetricSecurityKey CreateSigningKey()
    {
        if (string.IsNullOrWhiteSpace(Issuer)
            || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException(
                "JWT issuer and audience must be configured.");
        }

        if (AccessTokenMinutes is < 1 or > 60)
        {
            throw new InvalidOperationException(
                "JWT access-token lifetime must be between 1 and 60 minutes.");
        }

        byte[] keyBytes;

        try
        {
            keyBytes = Convert.FromBase64String(SigningKeyBase64);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKeyBase64 must contain a valid Base64 key.");
        }

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "The JWT signing key must contain at least 32 random bytes.");
        }

        return new SymmetricSecurityKey(keyBytes);
    }
}