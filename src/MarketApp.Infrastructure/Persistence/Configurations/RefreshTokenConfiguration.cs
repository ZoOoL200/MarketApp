using MarketApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketApp.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", table =>
        {
            table.HasCheckConstraint(
                "CK_RefreshTokens_Expiry",
                "\"ExpiresAtUtc\" > \"CreatedAtUtc\"");

            table.HasCheckConstraint(
                "CK_RefreshTokens_Consumption",
                "\"ConsumedAtUtc\" IS NULL OR " +
                "\"ConsumedAtUtc\" >= \"CreatedAtUtc\"");

            table.HasCheckConstraint(
                "CK_RefreshTokens_Hash",
                "\"TokenHash\" ~ '^[0-9A-F]{64}$'");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ConsumedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Version)
            .IsRowVersion();

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_RefreshTokens_TokenHash");

        builder.HasIndex(x => new
        {
            x.AuthSessionId,
            x.CreatedAtUtc
        })
            .HasDatabaseName("IX_RefreshTokens_Session_Created");

        builder.HasOne(x => x.AuthSession)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.AuthSessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}